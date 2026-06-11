import argparse
import json
import sys
from pathlib import Path

import torch
from PIL import Image
from torchvision import transforms

from pizza_ingredient_classifier.config import (
    ARTIFACTS_DIR,
    CLASS_NAMES,
    DEFAULT_CONFIDENCE_THRESHOLD,
    IMAGE_SIZE,
    INVALID_LABEL,
)
from pizza_ingredient_classifier.model import IngredientCNN


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run local inference on a sketch image.")
    parser.add_argument("--image", type=Path, required=True)
    parser.add_argument("--artifacts-dir", type=Path, default=ARTIFACTS_DIR)
    parser.add_argument("--threshold", type=float, default=DEFAULT_CONFIDENCE_THRESHOLD)
    return parser.parse_args()


def load_labels(path: Path) -> list[str]:
    if not path.exists():
        print(
            f"warning: {path} not found, falling back to built-in class labels {CLASS_NAMES}",
            file=sys.stderr,
        )
        return CLASS_NAMES
    return json.loads(path.read_text(encoding="utf-8"))


def preprocess(image_path: Path) -> torch.Tensor:
    transform = transforms.Compose(
        [
            transforms.Grayscale(num_output_channels=1),
            transforms.Resize((IMAGE_SIZE, IMAGE_SIZE)),
            transforms.ToTensor(),
        ]
    )
    image = Image.open(image_path)
    return transform(image).unsqueeze(0)


def main() -> None:
    args = parse_args()
    if not args.image.exists():
        raise SystemExit(f"image not found: {args.image}")

    weights_path = args.artifacts_dir / "best_model.pt"
    if not weights_path.exists():
        raise SystemExit(
            f"model weights not found: {weights_path}. Run training first with 'uv run -m pizza_ingredient_classifier.train'."
        )

    labels = load_labels(args.artifacts_dir / "labels.json")
    model = IngredientCNN(num_classes=len(labels))
    model.load_state_dict(torch.load(weights_path, map_location="cpu"))
    model.eval()

    tensor = preprocess(args.image)
    with torch.no_grad():
        logits = model(tensor)
        probs = torch.softmax(logits, dim=1)[0]

    confidence, index = torch.max(probs, dim=0)
    predicted_label = labels[index.item()] if confidence.item() >= args.threshold else INVALID_LABEL

    print(json.dumps({
        "label": predicted_label,
        "confidence": round(confidence.item(), 4),
        "raw_probabilities": {label: round(probs[i].item(), 4) for i, label in enumerate(labels)},
        "threshold": args.threshold,
    }, indent=2))


if __name__ == "__main__":
    main()
