from __future__ import annotations

import logging
from pathlib import Path
from typing import Any

import joblib
import pandas as pd

from app.config import Settings
from app.model_contract import FEATURE_COLUMNS

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
