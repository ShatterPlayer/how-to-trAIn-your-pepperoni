import argparse
import json
from pathlib import Path

import torch
from PIL import Image
from torch import nn
from torch.utils.data import DataLoader
from torchvision import datasets

from pizza_ingredient_classifier.config import (
    ARTIFACTS_DIR,
    DEFAULT_CONFIDENCE_THRESHOLD,
    DEFAULT_DATA_DIR,
    DEFAULT_MANUAL_DATA_DIR,
    INVALID_LABEL,
    ROTATION_INVARIANT_CLASS_NAMES,
)
from pizza_ingredient_classifier.data import build_transforms
from pizza_ingredient_classifier.metrics import evaluate_loader
from pizza_ingredient_classifier.model import IngredientCNN
from pizza_ingredient_classifier.preprocess import preprocess_tensor
from pizza_ingredient_classifier.utils import resolve_device, write_json


TEST_LABEL_ALIASES = {
    "pieczarka": "pieczarki",
}
ROTATION_TEST_ANGLES = (0, 45, 90, 135, 180, 225, 270, 315)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Evaluate a trained ingredient classifier.")
    parser.add_argument("--data-dir", type=Path, default=DEFAULT_DATA_DIR)
    parser.add_argument("--artifacts-dir", type=Path, default=ARTIFACTS_DIR)
    parser.add_argument("--manual-test-dir", type=Path, default=DEFAULT_MANUAL_DATA_DIR)
    parser.add_argument("--threshold", type=float, default=DEFAULT_CONFIDENCE_THRESHOLD)
    parser.add_argument("--batch-size", type=int, default=64)
    parser.add_argument("--num-workers", type=int, default=2)
    parser.add_argument("--target-val-accuracy", type=float, default=0.95)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--cpu", action="store_true")
    return parser.parse_args()


def load_labels(path: Path) -> list[str]:
    return json.loads(path.read_text(encoding="utf-8"))


def load_model(artifacts_dir: Path, labels: list[str], device: torch.device) -> IngredientCNN:
    weights_path = artifacts_dir / "best_model.pt"
    if not weights_path.exists():
        raise FileNotFoundError(f"model weights not found: {weights_path}")
    model = IngredientCNN(num_classes=len(labels)).to(device)
    model.load_state_dict(torch.load(weights_path, map_location=device))
    model.eval()
    return model


def expected_label_for_test(path: Path) -> str | None:
    if not path.stem.startswith("test_"):
        return None
    raw_label = path.stem.removeprefix("test_")
    return TEST_LABEL_ALIASES.get(raw_label, raw_label)


@torch.no_grad()
def predict_pil_image(
    model: IngredientCNN,
    image: Image.Image,
    labels: list[str],
    threshold: float,
    device: torch.device,
) -> dict:
    tensor = preprocess_tensor(image).to(device)
    logits = model(tensor)
    probs = torch.softmax(logits, dim=1)[0]
    confidence, index = torch.max(probs, dim=0)
    confidence_value = confidence.item()
    raw_label = labels[index.item()]
    predicted_label = raw_label if confidence_value >= threshold else INVALID_LABEL
    return {
        "label": predicted_label,
        "raw_label": raw_label,
        "confidence": confidence_value,
        "raw_probabilities": {label: probs[i].item() for i, label in enumerate(labels)},
    }


def predict_image(
    model: IngredientCNN,
    image_path: Path,
    labels: list[str],
    threshold: float,
    device: torch.device,
) -> dict:
    with Image.open(image_path) as image:
        prediction = predict_pil_image(model, image, labels, threshold, device)
    prediction["file"] = str(image_path)
    return prediction


def evaluate_manual_tests(
    model: IngredientCNN,
    manual_test_dir: Path,
    labels: list[str],
    threshold: float,
    device: torch.device,
) -> dict:
    predictions = []
    for image_path in sorted(manual_test_dir.glob("test_*.png")):
        expected = expected_label_for_test(image_path)
        if expected not in labels:
            continue
        prediction = predict_image(model, image_path, labels, threshold, device)
        prediction["expected_label"] = expected
        prediction["correct"] = prediction["label"] == expected
        predictions.append(prediction)

    correct = sum(1 for prediction in predictions if prediction["correct"])
    total = len(predictions)
    return {
        "accuracy": correct / total if total else 0.0,
        "correct": correct,
        "total": total,
        "all_correct": bool(predictions) and correct == total,
        "predictions": predictions,
    }


def evaluate_rotated_manual_tests(
    model: IngredientCNN,
    manual_test_dir: Path,
    labels: list[str],
    threshold: float,
    device: torch.device,
) -> dict:
    predictions = []
    for image_path in sorted(manual_test_dir.glob("test_*.png")):
        expected = expected_label_for_test(image_path)
        if expected not in ROTATION_INVARIANT_CLASS_NAMES:
            continue
        with Image.open(image_path) as image:
            base_image = image.convert("L")
            for angle in ROTATION_TEST_ANGLES:
                rotated = base_image.rotate(
                    angle,
                    resample=Image.Resampling.BILINEAR,
                    expand=True,
                    fillcolor=255,
                )
                prediction = predict_pil_image(model, rotated, labels, threshold, device)
                prediction["file"] = str(image_path)
                prediction["angle"] = angle
                prediction["expected_label"] = expected
                prediction["correct"] = prediction["label"] == expected
                predictions.append(prediction)

    correct = sum(1 for prediction in predictions if prediction["correct"])
    total = len(predictions)
    return {
        "accuracy": correct / total if total else 0.0,
        "correct": correct,
        "total": total,
        "all_correct": bool(predictions) and correct == total,
        "predictions": predictions,
    }


def main() -> None:
    args = parse_args()
    device = resolve_device(prefer_cuda=not args.cpu)
    labels = load_labels(args.artifacts_dir / "labels.json")
    model = load_model(args.artifacts_dir, labels, device)
    criterion = nn.CrossEntropyLoss()

    val_dataset = datasets.ImageFolder(args.data_dir / "val", transform=build_transforms(train=False))
    val_loader = DataLoader(
        val_dataset,
        batch_size=args.batch_size,
        shuffle=False,
        num_workers=args.num_workers,
        pin_memory=device.type == "cuda",
    )
    val_metrics = evaluate_loader(model, val_loader, criterion, device, labels)
    manual_metrics = evaluate_manual_tests(
        model=model,
        manual_test_dir=args.manual_test_dir,
        labels=labels,
        threshold=args.threshold,
        device=device,
    )
    rotated_manual_metrics = evaluate_rotated_manual_tests(
        model=model,
        manual_test_dir=args.manual_test_dir,
        labels=labels,
        threshold=args.threshold,
        device=device,
    )
    passed = (
        val_metrics["accuracy"] >= args.target_val_accuracy
        and manual_metrics["all_correct"]
        and rotated_manual_metrics["all_correct"]
    )
    report = {
        "passed": passed,
        "target_val_accuracy": args.target_val_accuracy,
        "threshold": args.threshold,
        "validation": val_metrics,
        "manual_tests": manual_metrics,
        "rotated_manual_tests": rotated_manual_metrics,
    }

    output_path = args.output or args.artifacts_dir / "evaluation.json"
    write_json(output_path, report)
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
