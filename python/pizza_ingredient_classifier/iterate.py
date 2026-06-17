import argparse
import json
import shutil
import subprocess
import sys
from dataclasses import asdict, dataclass
from pathlib import Path

from pizza_ingredient_classifier.config import ARTIFACTS_DIR, DEFAULT_DATA_DIR, DEFAULT_MANUAL_DATA_DIR, PYTHON_PROJECT_ROOT
from pizza_ingredient_classifier.utils import ensure_dir, write_json


@dataclass(frozen=True)
class Experiment:
    name: str
    train_count: int
    val_count: int
    epochs: int
    batch_size: int
    lr: float
    seed: int


EXPERIMENTS = [
    Experiment("centered_240_lr1e3", 240, 60, 30, 32, 1e-3, 42),
    Experiment("centered_320_lr1e3", 320, 80, 35, 32, 1e-3, 43),
    Experiment("centered_320_lr7e4", 320, 80, 40, 32, 7e-4, 44),
    Experiment("centered_400_lr1e3", 400, 100, 40, 32, 1e-3, 45),
    Experiment("centered_400_lr5e4", 400, 100, 45, 32, 5e-4, 46),
    Experiment("centered_480_lr7e4", 480, 120, 45, 32, 7e-4, 47),
    Experiment("centered_480_lr3e4", 480, 120, 55, 32, 3e-4, 48),
    Experiment("centered_640_lr5e4", 640, 160, 55, 32, 5e-4, 49),
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Iteratively augment, train, evaluate, and keep the best classifier.")
    parser.add_argument("--data-dir", type=Path, default=DEFAULT_DATA_DIR)
    parser.add_argument("--source-dir", type=Path, default=DEFAULT_MANUAL_DATA_DIR)
    parser.add_argument("--artifacts-dir", type=Path, default=ARTIFACTS_DIR)
    parser.add_argument("--experiments-dir", type=Path)
    parser.add_argument("--target-val-accuracy", type=float, default=0.95)
    parser.add_argument("--max-rounds", type=int, default=len(EXPERIMENTS))
    parser.add_argument("--num-workers", type=int, default=2)
    parser.add_argument("--cpu", action="store_true")
    parser.add_argument("--keep-going-after-pass", action="store_true")
    return parser.parse_args()


def run_command(command: list[str]) -> None:
    print("+ " + " ".join(command), flush=True)
    subprocess.run(command, cwd=PYTHON_PROJECT_ROOT, check=True)


def promote_artifacts(source_dir: Path, target_dir: Path) -> None:
    ensure_dir(target_dir)
    for path in source_dir.iterdir():
        if path.is_file() and path.name != "iteration_summary.json":
            shutil.copy2(path, target_dir / path.name)


def round_score(report: dict) -> tuple[bool, float, float, float]:
    return (
        bool(report["passed"]),
        float(report["validation"]["accuracy"]),
        float(report["rotated_manual_tests"]["accuracy"]),
        float(report["manual_tests"]["accuracy"]),
    )


def main() -> None:
    args = parse_args()
    experiments_dir = args.experiments_dir or args.artifacts_dir / "experiments"
    ensure_dir(experiments_dir)

    best_round: dict | None = None
    rounds = []
    selected_experiments = EXPERIMENTS[: args.max_rounds]

    for index, experiment in enumerate(selected_experiments, start=1):
        round_dir = experiments_dir / f"{index:02d}_{experiment.name}"
        ensure_dir(round_dir)
        print(f"\n=== round={index} experiment={experiment.name} ===", flush=True)

        run_command(
            [
                sys.executable,
                "-m",
                "pizza_ingredient_classifier.augment",
                "--source-dir",
                str(args.source_dir),
                "--output-dir",
                str(args.data_dir),
                "--train-count",
                str(experiment.train_count),
                "--val-count",
                str(experiment.val_count),
                "--seed",
                str(experiment.seed),
                "--overwrite",
            ]
        )

        train_command = [
            sys.executable,
            "-m",
            "pizza_ingredient_classifier.train",
            "--data-dir",
            str(args.data_dir),
            "--artifacts-dir",
            str(round_dir),
            "--epochs",
            str(experiment.epochs),
            "--batch-size",
            str(experiment.batch_size),
            "--lr",
            str(experiment.lr),
            "--num-workers",
            str(args.num_workers),
        ]
        if args.cpu:
            train_command.append("--cpu")
        run_command(train_command)

        evaluation_path = round_dir / "evaluation.json"
        evaluate_command = [
            sys.executable,
            "-m",
            "pizza_ingredient_classifier.evaluate",
            "--data-dir",
            str(args.data_dir),
            "--artifacts-dir",
            str(round_dir),
            "--manual-test-dir",
            str(args.source_dir),
            "--target-val-accuracy",
            str(args.target_val_accuracy),
            "--output",
            str(evaluation_path),
            "--num-workers",
            str(args.num_workers),
        ]
        if args.cpu:
            evaluate_command.append("--cpu")
        run_command(evaluate_command)

        report = json.loads(evaluation_path.read_text(encoding="utf-8"))
        round_result = {
            "round": index,
            "experiment": asdict(experiment),
            "artifacts_dir": str(round_dir),
            "passed": report["passed"],
            "val_accuracy": report["validation"]["accuracy"],
            "manual_accuracy": report["manual_tests"]["accuracy"],
            "rotated_manual_accuracy": report["rotated_manual_tests"]["accuracy"],
            "manual_all_correct": report["manual_tests"]["all_correct"],
            "rotated_manual_all_correct": report["rotated_manual_tests"]["all_correct"],
        }
        rounds.append(round_result)
        print(
            "result "
            f"passed={round_result['passed']} "
            f"val_acc={round_result['val_accuracy']:.4f} "
            f"manual_acc={round_result['manual_accuracy']:.4f} "
            f"rotated_manual_acc={round_result['rotated_manual_accuracy']:.4f}",
            flush=True,
        )

        if best_round is None or round_score(report) > round_score(best_round["report"]):
            best_round = {"result": round_result, "report": report, "dir": round_dir}
            promote_artifacts(round_dir, args.artifacts_dir)

        if report["passed"] and not args.keep_going_after_pass:
            break

    summary = {
        "target_val_accuracy": args.target_val_accuracy,
        "passed": bool(best_round and best_round["report"]["passed"]),
        "best_round": best_round["result"] if best_round else None,
        "rounds": rounds,
    }
    write_json(args.artifacts_dir / "iteration_summary.json", summary)
    print(json.dumps(summary, indent=2), flush=True)


if __name__ == "__main__":
    main()
