import argparse
from pathlib import Path

import torch
from torch import nn

from pizza_ingredient_classifier.config import ARTIFACTS_DIR, CLASS_NAMES, DEFAULT_DATA_DIR, IMAGE_SIZE
from pizza_ingredient_classifier.data import build_dataloaders
from pizza_ingredient_classifier.metrics import evaluate_loader
from pizza_ingredient_classifier.model import IngredientCNN
from pizza_ingredient_classifier.utils import ensure_dir, resolve_device, write_json


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Train ingredient classifier and export ONNX.")
    parser.add_argument("--data-dir", type=Path, default=DEFAULT_DATA_DIR)
    parser.add_argument("--artifacts-dir", type=Path, default=ARTIFACTS_DIR)
    parser.add_argument("--epochs", type=int, default=20)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--num-workers", type=int, default=2)
    parser.add_argument("--cpu", action="store_true")
    return parser.parse_args()


def run_epoch(model, loader, criterion, optimizer, device):
    model.train()
    total_loss = 0.0
    total_correct = 0
    total_samples = 0

    for images, labels in loader:
        images = images.to(device)
        labels = labels.to(device)

        optimizer.zero_grad()
        logits = model(images)
        loss = criterion(logits, labels)
        loss.backward()
        optimizer.step()

        total_loss += loss.item() * images.size(0)
        preds = logits.argmax(dim=1)
        total_correct += (preds == labels).sum().item()
        total_samples += images.size(0)

    return total_loss / total_samples, total_correct / total_samples


def export_onnx(model: IngredientCNN, artifacts_dir: Path) -> Path:
    export_path = artifacts_dir / "pizza_ingredient_classifier.onnx"
    dummy = torch.randn(1, 1, IMAGE_SIZE, IMAGE_SIZE)
    model.eval().cpu()
    torch.onnx.export(
        model,
        dummy,
        export_path,
        input_names=["input"],
        output_names=["logits"],
        dynamic_axes={"input": {0: "batch"}, "logits": {0: "batch"}},
        opset_version=17,
        external_data=False,
    )
    return export_path


def main() -> None:
    args = parse_args()
    device = resolve_device(prefer_cuda=not args.cpu)

    ensure_dir(args.artifacts_dir)
    train_loader, val_loader, detected_classes = build_dataloaders(
        data_dir=args.data_dir,
        batch_size=args.batch_size,
        num_workers=args.num_workers,
    )

    missing = set(CLASS_NAMES) - set(detected_classes)
    extra = set(detected_classes) - set(CLASS_NAMES)
    if missing or extra:
        raise ValueError(
            f"Dataset classes must match expected classes. Missing={sorted(missing)}, Extra={sorted(extra)}"
        )

    model = IngredientCNN(num_classes=len(detected_classes)).to(device)
    criterion = nn.CrossEntropyLoss()
    optimizer = torch.optim.Adam(model.parameters(), lr=args.lr)
    write_json(args.artifacts_dir / "labels.json", detected_classes)

    best_acc = -1.0
    best_metrics = {}
    best_model_path = args.artifacts_dir / "best_model.pt"
    history = []

    for epoch in range(1, args.epochs + 1):
        train_loss, train_acc = run_epoch(model, train_loader, criterion, optimizer, device)
        val_metrics = evaluate_loader(model, val_loader, criterion, device, detected_classes)
        val_loss = val_metrics["loss"]
        val_acc = val_metrics["accuracy"]

        print(
            f"epoch={epoch:02d} train_loss={train_loss:.4f} train_acc={train_acc:.4f} "
            f"val_loss={val_loss:.4f} val_acc={val_acc:.4f}"
        )

        epoch_metrics = {
            "epoch": epoch,
            "train_loss": train_loss,
            "train_accuracy": train_acc,
            "val_loss": val_loss,
            "val_accuracy": val_acc,
        }
        history.append(epoch_metrics)

        if val_acc > best_acc:
            best_acc = val_acc
            best_metrics = {
                **epoch_metrics,
                "image_size": IMAGE_SIZE,
                "classes": detected_classes,
                "val_per_class": val_metrics["per_class"],
                "val_confusion_matrix": val_metrics["confusion_matrix"],
            }
            torch.save(model.state_dict(), best_model_path)

    if not best_model_path.exists():
        raise RuntimeError("Training finished without saving a model checkpoint.")

    model.load_state_dict(torch.load(best_model_path, map_location="cpu"))
    best_val_metrics = evaluate_loader(model, val_loader, criterion, torch.device("cpu"), detected_classes)
    best_metrics["val_loss"] = best_val_metrics["loss"]
    best_metrics["val_accuracy"] = best_val_metrics["accuracy"]
    best_metrics["val_per_class"] = best_val_metrics["per_class"]
    best_metrics["val_confusion_matrix"] = best_val_metrics["confusion_matrix"]
    best_metrics["history"] = history
    write_json(args.artifacts_dir / "metrics.json", best_metrics)

    onnx_path = export_onnx(model, args.artifacts_dir)

    print(f"saved pytorch weights: {best_model_path}")
    print(f"saved onnx model: {onnx_path}")


if __name__ == "__main__":
    main()
