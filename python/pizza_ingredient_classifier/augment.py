import argparse
import random
import shutil
from pathlib import Path

from PIL import Image, ImageChops, ImageEnhance, ImageFilter, ImageOps

from pizza_ingredient_classifier.config import (
    CLASS_NAMES,
    DEFAULT_DATA_DIR,
    DEFAULT_MANUAL_DATA_DIR,
    IMAGE_SIZE,
    ROTATION_INVARIANT_CLASS_NAMES,
)
from pizza_ingredient_classifier.preprocess import load_preprocessed_image, preprocess_pil_image
from pizza_ingredient_classifier.utils import ensure_dir


ROTATION_ANGLES = (0, 45, 90, 135, 180, 225, 270, 315)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Expand a small manual sketch dataset with slight augmentations.")
    parser.add_argument(
        "--source-dir",
        type=Path,
        default=DEFAULT_MANUAL_DATA_DIR,
        help="Directory with original class folders, e.g. samples_raw/salami/*.png",
    )
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_DATA_DIR)
    parser.add_argument("--train-count", type=int, default=240)
    parser.add_argument("--val-count", type=int, default=60)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args()


def list_images(path: Path) -> list[Path]:
    return sorted(
        [p for p in path.iterdir() if p.is_file() and p.suffix.lower() in {".png", ".jpg", ".jpeg", ".bmp"}]
    )


def load_canvas(image_path: Path) -> Image.Image:
    return load_preprocessed_image(image_path)


def random_affine(image: Image.Image) -> Image.Image:
    angle = random.uniform(-18, 18)
    tx = random.randint(-6, 6)
    ty = random.randint(-6, 6)
    scale = random.uniform(0.9, 1.1)

    transformed = image.rotate(angle, resample=Image.Resampling.BILINEAR, fillcolor=255)
    transformed = transformed.transform(
        transformed.size,
        Image.Transform.AFFINE,
        (scale, 0, tx, 0, scale, ty),
        resample=Image.Resampling.BILINEAR,
        fillcolor=255,
    )
    return transformed


def random_stroke_change(image: Image.Image) -> Image.Image:
    roll = random.random()
    if roll < 0.25:
        return image.filter(ImageFilter.MaxFilter(size=3))
    if roll < 0.50:
        return image.filter(ImageFilter.MinFilter(size=3))
    if roll < 0.65:
        return image.filter(ImageFilter.GaussianBlur(radius=0.4))
    return image


def random_tone(image: Image.Image) -> Image.Image:
    contrast = ImageEnhance.Contrast(image).enhance(random.uniform(0.9, 1.15))
    brightness = ImageEnhance.Brightness(contrast).enhance(random.uniform(0.97, 1.03))
    return brightness


def random_flip(image: Image.Image) -> Image.Image:
    if random.random() < 0.25:
        return ImageOps.mirror(image)
    return image


def rotate_and_center(image: Image.Image, angle: float) -> Image.Image:
    rotated = image.rotate(angle, resample=Image.Resampling.BILINEAR, expand=True, fillcolor=255)
    return preprocess_pil_image(rotated)


def random_orientation(image: Image.Image, class_name: str) -> Image.Image:
    if class_name not in ROTATION_INVARIANT_CLASS_NAMES:
        return image
    return rotate_and_center(image, random.uniform(0, 360))


def random_crop_pad(image: Image.Image) -> Image.Image:
    border = random.randint(0, 6)
    expanded = ImageOps.expand(image, border=border, fill=255)
    left = random.randint(0, max(0, expanded.width - IMAGE_SIZE))
    top = random.randint(0, max(0, expanded.height - IMAGE_SIZE))
    cropped = expanded.crop((left, top, left + IMAGE_SIZE, top + IMAGE_SIZE))
    return cropped


def finalize(image: Image.Image) -> Image.Image:
    # Keep sketches close to binary black-on-white drawings.
    image = ImageOps.autocontrast(image)
    image = image.point(lambda p: 255 if p > 220 else p)
    return image.convert("L")


def augment(base_image: Image.Image, class_name: str) -> Image.Image:
    image = random_orientation(base_image, class_name)
    image = random_affine(image)
    image = random_flip(image)
    image = random_crop_pad(image)
    image = random_stroke_change(image)
    image = random_tone(image)
    return finalize(image)


def prepare_output_dir(output_dir: Path, overwrite: bool) -> None:
    if output_dir.exists() and overwrite:
        shutil.rmtree(output_dir)

    for split in ("train", "val"):
        for class_name in CLASS_NAMES:
            ensure_dir(output_dir / split / class_name)


def save_originals_and_augmented(
    class_name: str,
    originals: list[Path],
    split_dir: Path,
    target_count: int,
    prefix: str,
) -> None:
    if not originals:
        raise ValueError(f"No source images found for class '{class_name}'.")

    base_images = [load_canvas(path) for path in originals]
    saved = 0

    # Save originals first so each split definitely contains true manual examples.
    for image in base_images:
        if saved >= target_count:
            break
        image.save(split_dir / f"{prefix}_{saved:03d}.png")
        saved += 1

        if class_name in ROTATION_INVARIANT_CLASS_NAMES:
            for angle in ROTATION_ANGLES[1:]:
                if saved >= target_count:
                    break
                rotate_and_center(image, angle).save(split_dir / f"{prefix}_{saved:03d}.png")
                saved += 1

    while saved < target_count:
        base = random.choice(base_images)
        variant = augment(base, class_name)
        variant.save(split_dir / f"{prefix}_{saved:03d}.png")
        saved += 1


def main() -> None:
    args = parse_args()
    random.seed(args.seed)

    prepare_output_dir(args.output_dir, overwrite=args.overwrite)

    for class_name in CLASS_NAMES:
        originals = list_images(args.source_dir / class_name)
        random.shuffle(originals)

        if len(originals) < 2:
            raise ValueError(f"Class '{class_name}' needs at least 2 originals to build train/val splits.")

        val_original_count = max(1, round(len(originals) * 0.2))
        val_originals = originals[:val_original_count]
        train_originals = originals[val_original_count:]
        if not train_originals:
            train_originals = originals[1:]
            val_originals = originals[:1]

        save_originals_and_augmented(
            class_name=class_name,
            originals=train_originals,
            split_dir=args.output_dir / "train" / class_name,
            target_count=args.train_count,
            prefix=class_name,
        )
        save_originals_and_augmented(
            class_name=class_name,
            originals=val_originals,
            split_dir=args.output_dir / "val" / class_name,
            target_count=args.val_count,
            prefix=class_name,
        )

        print(
            f"class={class_name} originals={len(originals)} train={args.train_count} val={args.val_count}"
        )


if __name__ == "__main__":
    main()
