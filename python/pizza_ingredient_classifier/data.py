from pathlib import Path

from torch.utils.data import DataLoader
from torchvision import datasets, transforms

from pizza_ingredient_classifier.preprocess import preprocess_pil_image


def build_transforms(train: bool) -> transforms.Compose:
    ops = [
        transforms.Lambda(preprocess_pil_image),
    ]
    if train:
        ops.extend(
            [
                transforms.RandomRotation(18),
                transforms.RandomAffine(degrees=0, translate=(0.08, 0.08), scale=(0.9, 1.1)),
            ]
        )
    ops.extend(
        [
            transforms.ToTensor(),
        ]
    )
    return transforms.Compose(ops)


def build_dataloaders(data_dir: Path, batch_size: int, num_workers: int) -> tuple[DataLoader, DataLoader, list[str]]:
    train_dir = data_dir / "train"
    val_dir = data_dir / "val"

    train_dataset = datasets.ImageFolder(train_dir, transform=build_transforms(train=True))
    val_dataset = datasets.ImageFolder(val_dir, transform=build_transforms(train=False))

    if train_dataset.classes != val_dataset.classes:
        raise ValueError("Train and validation class folders do not match.")

    train_loader = DataLoader(
        train_dataset,
        batch_size=batch_size,
        shuffle=True,
        num_workers=num_workers,
        pin_memory=True,
    )
    val_loader = DataLoader(
        val_dataset,
        batch_size=batch_size,
        shuffle=False,
        num_workers=num_workers,
        pin_memory=True,
    )

    return train_loader, val_loader, train_dataset.classes
