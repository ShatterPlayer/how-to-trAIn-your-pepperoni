from collections.abc import Sequence

import torch
from torch import nn
from torch.utils.data import DataLoader


def summarize_classification(
    targets: Sequence[int],
    predictions: Sequence[int],
    class_names: Sequence[str],
) -> dict:
    num_classes = len(class_names)
    matrix = [[0 for _ in range(num_classes)] for _ in range(num_classes)]
    for target, prediction in zip(targets, predictions, strict=True):
        matrix[target][prediction] += 1

    total = len(targets)
    correct = sum(matrix[index][index] for index in range(num_classes))
    per_class = {}
    for index, class_name in enumerate(class_names):
        true_positive = matrix[index][index]
        support = sum(matrix[index])
        predicted = sum(row[index] for row in matrix)
        precision = true_positive / predicted if predicted else 0.0
        recall = true_positive / support if support else 0.0
        f1 = 2 * precision * recall / (precision + recall) if precision + recall else 0.0
        per_class[class_name] = {
            "precision": precision,
            "recall": recall,
            "f1": f1,
            "support": support,
        }

    return {
        "accuracy": correct / total if total else 0.0,
        "per_class": per_class,
        "confusion_matrix": {
            "labels": list(class_names),
            "matrix": matrix,
        },
    }


@torch.no_grad()
def evaluate_loader(
    model: nn.Module,
    loader: DataLoader,
    criterion: nn.Module,
    device: torch.device,
    class_names: Sequence[str],
) -> dict:
    model.eval()
    total_loss = 0.0
    total_samples = 0
    targets: list[int] = []
    predictions: list[int] = []

    for images, labels in loader:
        images = images.to(device)
        labels = labels.to(device)
        logits = model(images)
        loss = criterion(logits, labels)

        total_loss += loss.item() * images.size(0)
        total_samples += images.size(0)
        targets.extend(labels.cpu().tolist())
        predictions.extend(logits.argmax(dim=1).cpu().tolist())

    summary = summarize_classification(targets, predictions, class_names)
    summary["loss"] = total_loss / total_samples if total_samples else 0.0
    return summary
