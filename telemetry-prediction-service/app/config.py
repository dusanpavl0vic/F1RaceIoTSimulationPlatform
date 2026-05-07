from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


def _parse_bool(value: str | None, default: bool = False) -> bool:
    if value is None:
        return default

    normalized = value.strip().lower()
    if normalized in {"1", "true", "yes", "y", "on"}:
        return True
    if normalized in {"0", "false", "no", "n", "off"}:
        return False
    return default


def _parse_csv(value: str | None, default: tuple[str, ...]) -> tuple[str, ...]:
    if value is None or not value.strip():
        return default

    parsed = tuple(
        item.strip()
        for item in value.split(",")
        if item.strip()
    )
    return parsed or default


@dataclass(slots=True)
class Settings:
    app_name: str
    model_path: Path
    dataset_path: Path
    canonical_data_dir: Path
    min_training_rows: int
    random_state: int
    cors_allowed_origins: tuple[str, ...]

    @classmethod
    def from_env(cls) -> "Settings":
        return cls(
            app_name=os.getenv("APP_NAME", "telemetry-prediction-service"),
            model_path=Path(os.getenv("MODEL_PATH", "/models/next_lap_model.joblib")),
            dataset_path=Path(os.getenv("DATASET_PATH", "/models/training/next_lap_training.csv")),
            canonical_data_dir=Path(os.getenv("CANONICAL_DATA_DIR", "/data/canonical")),
            min_training_rows=int(os.getenv("MIN_TRAINING_ROWS", "40")),
            random_state=int(os.getenv("MODEL_RANDOM_STATE", "42")),
            cors_allowed_origins=_parse_csv(
                os.getenv("CORS_ALLOWED_ORIGINS"),
                default=("http://localhost:3000", "http://127.0.0.1:3000"),
            ),
        )
