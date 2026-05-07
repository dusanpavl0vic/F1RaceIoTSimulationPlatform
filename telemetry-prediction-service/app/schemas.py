from __future__ import annotations

from typing import Any

from pydantic import BaseModel, Field


class HealthResponse(BaseModel):
    service: str
    model_loaded: bool
    model_version: str | None = None


class ModelInfoResponse(BaseModel):
    loaded: bool
    metadata: dict[str, Any] | None = None


class NextLapPredictionFeatures(BaseModel):
    driver_number: int = Field(..., ge=1)
    lap_number: int = Field(..., ge=1)
    position: int | None = Field(default=None, ge=1)
    lap_time_last: float | None = Field(default=None, gt=0)
    lap_time_best: float | None = Field(default=None, gt=0)
    lap_time_avg_last_3: float | None = Field(default=None, gt=0)
    lap_time_avg_last_5: float | None = Field(default=None, gt=0)
    gap_to_leader: float | None = Field(default=None, ge=0)
    gap_to_ahead: float | None = Field(default=None, ge=0)
    stint_number: int | None = Field(default=None, ge=1)
    tyre_compound: str | None = None
    tyre_is_new: bool | None = None
    tyre_laps_on_set: int | None = Field(default=None, ge=0)
    in_pit: bool | None = None
    avg_speed_last_lap: float | None = Field(default=None, ge=0)
    max_speed_last_lap: float | None = Field(default=None, ge=0)
    avg_rpm_last_lap: float | None = Field(default=None, ge=0)
    avg_throttle_pct_last_lap: float | None = Field(default=None, ge=0, le=100)
    avg_raw_brake_last_lap: float | None = Field(default=None, ge=0, le=100)
    drs_open_ratio_last_lap: float | None = Field(default=None, ge=0, le=1)
    gear_changes_last_lap: int | None = Field(default=None, ge=0)


class NextLapPredictionRequest(BaseModel):
    features: NextLapPredictionFeatures


class NextLapPredictionBatchRequest(BaseModel):
    items: list[NextLapPredictionFeatures]


class NextLapPredictionResult(BaseModel):
    predicted_next_lap_time: float
    model_version: str | None = None


class NextLapPredictionBatchResult(BaseModel):
    predictions: list[NextLapPredictionResult]
    model_version: str | None = None
