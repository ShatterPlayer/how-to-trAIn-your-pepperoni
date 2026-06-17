from pathlib import Path

import torch
from PIL import Image, ImageOps
from torchvision import transforms

from pizza_ingredient_classifier.config import IMAGE_SIZE


INK_THRESHOLD = 245
MIN_MARGIN_PX = 4


def _ink_bbox(image: Image.Image, threshold: int = INK_THRESHOLD) -> tuple[int, int, int, int] | None:
    mask = image.point(lambda p: 255 if p < threshold else 0)
    return mask.getbbox()


def _expand_bbox(
    bbox: tuple[int, int, int, int],
    image_size: tuple[int, int],
    margin_ratio: float,
) -> tuple[int, int, int, int]:
    left, top, right, bottom = bbox
    width = right - left
    height = bottom - top
    margin = max(MIN_MARGIN_PX, round(max(width, height) * margin_ratio))
    image_width, image_height = image_size
    return (
        max(0, left - margin),
        max(0, top - margin),
        min(image_width, right + margin),
        min(image_height, bottom + margin),
    )


def crop_to_ink(image: Image.Image, margin_ratio: float = 0.18) -> Image.Image:
    grayscale = ImageOps.exif_transpose(image).convert("L")
    bbox = _ink_bbox(grayscale)
    if bbox is None:
        return grayscale
    return grayscale.crop(_expand_bbox(bbox, grayscale.size, margin_ratio))


def square_pad(image: Image.Image, fill: int = 255) -> Image.Image:
    width, height = image.size
    side = max(width, height)
    left = (side - width) // 2
    top = (side - height) // 2
    padded = Image.new("L", (side, side), color=fill)
    padded.paste(image, (left, top))
    return padded


def preprocess_pil_image(
    image: Image.Image,
    *,
    image_size: int = IMAGE_SIZE,
    center_ink: bool = True,
) -> Image.Image:
    grayscale = ImageOps.exif_transpose(image).convert("L")
    if center_ink:
        grayscale = crop_to_ink(grayscale)
    squared = square_pad(grayscale)
    return squared.resize((image_size, image_size), Image.Resampling.LANCZOS)


def load_preprocessed_image(path: Path, *, image_size: int = IMAGE_SIZE, center_ink: bool = True) -> Image.Image:
    with Image.open(path) as image:
        return preprocess_pil_image(image, image_size=image_size, center_ink=center_ink)


def preprocess_tensor(image: Image.Image) -> torch.Tensor:
    transform = transforms.Compose(
        [
            transforms.Lambda(preprocess_pil_image),
            transforms.ToTensor(),
        ]
    )
    return transform(image).unsqueeze(0)
