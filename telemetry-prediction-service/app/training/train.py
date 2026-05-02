from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import joblib
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestRegressor
from sklearn.impute import SimpleImputer
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder

from app.model_contract import CATEGORICAL_COLUMNS, FEATURE_COLUMNS, NUMERIC_COLUMNS, TARGET_COLUMN
from app.training.dataset_builder import build_training_dataset


def train_model_from_sources(
    *,
    canonical_dir: Path | None,
    dataset_path: Path | None,
    output_model_path: Path,
    output_dataset_path: Path | None,
    min_training_rows: int,
    random_state: int,
) -> dict[str, Any]:
    dataset = _load_or_build_dataset(
        canonical_dir=canonical_dir,
        dataset_path=dataset_path,
        output_dataset_path=output_dataset_path,
    )

    if dataset.empty:
        raise ValueError("Training dataset is empty.")

    if len(dataset) < min_training_rows:
        raise ValueError(
            f"Training dataset has only {len(dataset)} rows. Required minimum is {min_training_rows}."
        )

    dataset = dataset.dropna(subset=[TARGET_COLUMN]).copy()
    if len(dataset) < min_training_rows:
        raise ValueError(
            f"Training dataset has only {len(dataset)} labeled rows after cleanup. Required minimum is {min_training_rows}."
        )

    if "sample_index" not in dataset.columns:
        dataset["sample_index"] = range(len(dataset))

    dataset = dataset.sort_values(
        ["sample_index", "session_id", "driver_number", "lap_number"],
        na_position="last",
    ).reset_index(drop=True)

    split_index = max(int(len(dataset) * 0.8), 1)
    if split_index >= len(dataset):
        split_index = len(dataset) - 1

    train_df = dataset.iloc[:split_index].copy()
    test_df = dataset.iloc[split_index:].copy()

    preprocessor = ColumnTransformer(
        transformers=[
            (
                "numeric",
                Pipeline(
                    steps=[
                        ("imputer", SimpleImputer(strategy="median")),
                    ]
                ),
                NUMERIC_COLUMNS,
            ),
            (
                "categorical",
                Pipeline(
                    steps=[
                        ("imputer", SimpleImputer(strategy="most_frequent")),
                        ("encoder", OneHotEncoder(handle_unknown="ignore")),
                    ]
                ),
                CATEGORICAL_COLUMNS,
            ),
        ]
    )

    model = Pipeline(
        steps=[
            ("preprocessor", preprocessor),
            (
                "regressor",
                RandomForestRegressor(
                    n_estimators=320,
                    random_state=random_state,
                    min_samples_leaf=2,
                    n_jobs=-1,
                ),
            ),
        ]
    )

    model.fit(train_df[FEATURE_COLUMNS], train_df[TARGET_COLUMN])

    predictions = model.predict(test_df[FEATURE_COLUMNS])
    metrics = {
        "mae": float(mean_absolute_error(test_df[TARGET_COLUMN], predictions)),
        "rmse": float(mean_squared_error(test_df[TARGET_COLUMN], predictions) ** 0.5),
        "r2": float(r2_score(test_df[TARGET_COLUMN], predictions)),
        "train_rows": int(len(train_df)),
        "test_rows": int(len(test_df)),
        "total_rows": int(len(dataset)),
    }

    metadata = {
        "model_type": "RandomForestRegressor",
        "target": TARGET_COLUMN,
        "features": FEATURE_COLUMNS,
        "trained_at_utc": datetime.now(timezone.utc).isoformat(),
        "metrics": metrics,
        "dataset_source": str(dataset_path) if dataset_path else str(canonical_dir) if canonical_dir else None,
    }

    output_model_path.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump({"pipeline": model, "metadata": metadata}, output_model_path)
    return metadata


def _load_or_build_dataset(
    *,
    canonical_dir: Path | None,
    dataset_path: Path | None,
    output_dataset_path: Path | None,
) -> pd.DataFrame:
    if dataset_path and dataset_path.exists():
        return pd.read_csv(dataset_path)

    if canonical_dir is None:
        raise ValueError("Either dataset_path or canonical_dir must be provided for training.")

    dataset = build_training_dataset(canonical_dir)
    if output_dataset_path is not None:
        output_dataset_path.parent.mkdir(parents=True, exist_ok=True)
        dataset.to_csv(output_dataset_path, index=False)
    return dataset


def _build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Train next-lap prediction model.")
    parser.add_argument("--canonical-dir", type=Path, default=None, help="Directory with canonical JSONL capture files.")
    parser.add_argument("--dataset-path", type=Path, default=None, help="Existing CSV dataset to use for training.")
    parser.add_argument("--output-model", type=Path, required=True, help="Output .joblib model artifact path.")
    parser.add_argument("--output-dataset", type=Path, default=None, help="Optional output CSV dataset path.")
    parser.add_argument("--min-training-rows", type=int, default=40)
    parser.add_argument("--random-state", type=int, default=42)
    return parser


def main() -> None:
    parser = _build_arg_parser()
    args = parser.parse_args()

    metadata = train_model_from_sources(
        canonical_dir=args.canonical_dir,
        dataset_path=args.dataset_path,
        output_model_path=args.output_model,
        output_dataset_path=args.output_dataset,
        min_training_rows=args.min_training_rows,
        random_state=args.random_state,
    )
    print(json.dumps(metadata, indent=2))


if __name__ == "__main__":
    main()
