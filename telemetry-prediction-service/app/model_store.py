from __future__ import annotations

import logging
from pathlib import Path
from typing import Any

import joblib
import pandas as pd

from app.config import Settings
from app.model_contract import FEATURE_COLUMNS
from app.training.train import train_model_from_sources

logger = logging.getLogger(__name__)


class ModelStore:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._bundle: dict[str, Any] | None = None

    @property
    def model_loaded(self) -> bool:
        return self._bundle is not None

    @property
    def metadata(self) -> dict[str, Any] | None:
        if self._bundle is None:
            return None
        return self._bundle.get("metadata")

    def initialize(self) -> None:
        if self._settings.train_on_startup:
            logger.info("TRAIN_ON_STARTUP=true. Training model before loading it.")
            train_model_from_sources(
                canonical_dir=self._settings.canonical_data_dir if self._settings.canonical_data_dir.exists() else None,
                dataset_path=self._settings.dataset_path if self._settings.dataset_path.exists() else None,
                output_model_path=self._settings.model_path,
                output_dataset_path=self._settings.dataset_path,
                min_training_rows=self._settings.min_training_rows,
                random_state=self._settings.random_state,
            )

        if not self._settings.model_path.exists():
            logger.warning("Prediction model file does not exist at %s.", self._settings.model_path)
            return

        self.load(self._settings.model_path)

    def load(self, model_path: Path) -> None:
        bundle = joblib.load(model_path)
        if not isinstance(bundle, dict) or "pipeline" not in bundle:
            raise ValueError("Invalid model bundle format. Expected dict with 'pipeline'.")

        self._bundle = bundle
        logger.info("Loaded prediction model from %s.", model_path)

    def predict_next_lap(self, items: list[dict[str, Any]]) -> list[float]:
        if self._bundle is None:
            raise RuntimeError("Prediction model is not loaded.")

        frame = pd.DataFrame(items)
        for column in FEATURE_COLUMNS:
            if column not in frame.columns:
                frame[column] = None

        frame = frame[FEATURE_COLUMNS]
        pipeline = self._bundle["pipeline"]
        predictions = pipeline.predict(frame)
        return [float(value) for value in predictions]
