from pathlib import Path


PACKAGE_ROOT = Path(__file__).resolve().parent
PYTHON_PROJECT_ROOT = PACKAGE_ROOT.parent
REPO_ROOT = PYTHON_PROJECT_ROOT.parent
ARTIFACTS_DIR = PACKAGE_ROOT / "artifacts"
DEFAULT_DATA_DIR = PACKAGE_ROOT / "data" / "raw"
DEFAULT_MANUAL_DATA_DIR = REPO_ROOT / "samples_raw"

CLASS_NAMES = [
    "salami",
    "ananas",
    "pieczarki",
    "bekon",
    "szpinak",
]

ROTATION_INVARIANT_CLASS_NAMES = [
    "ananas",
    "bekon",
]

INVALID_LABEL = "invalid"
IMAGE_SIZE = 64
DEFAULT_CONFIDENCE_THRESHOLD = 0.75
