from __future__ import annotations

from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException

from app.config import Settings
from app.model_store import ModelStore
from app.schemas import (
    HealthResponse,
    ModelInfoResponse,
    NextLapPredictionBatchRequest,
    NextLapPredictionBatchResult,
    NextLapPredictionRequest,
    NextLapPredictionResult,
)

settings = Settings.from_env()
model_store = ModelStore(settings)


@asynccontextmanager
async def lifespan(_: FastAPI):
    model_store.initialize()
    yield


app = FastAPI(
    title="Telemetry Prediction Service",
    version="1.0.0",
    lifespan=lifespan,
)


def _current_model_version() -> str | None:
    metadata = model_store.metadata or {}
    return metadata.get("trained_at_utc")


@app.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    return HealthResponse(
        service=settings.app_name,
        model_loaded=model_store.model_loaded,
        train_on_startup=settings.train_on_startup,
        model_version=_current_model_version(),
    )


@app.get("/v1/model/info", response_model=ModelInfoResponse)
async def model_info() -> ModelInfoResponse:
    return ModelInfoResponse(
        loaded=model_store.model_loaded,
        metadata=model_store.metadata,
    )


@app.post("/v1/predictions/next-lap", response_model=NextLapPredictionResult)
async def predict_next_lap(request: NextLapPredictionRequest) -> NextLapPredictionResult:
    if not model_store.model_loaded:
        raise HTTPException(status_code=503, detail="Prediction model is not loaded.")

    prediction = model_store.predict_next_lap([request.features.model_dump()])[0]
    return NextLapPredictionResult(
        predicted_next_lap_time=prediction,
        model_version=_current_model_version(),
    )


@app.post("/v1/predictions/next-lap/batch", response_model=NextLapPredictionBatchResult)
async def predict_next_lap_batch(request: NextLapPredictionBatchRequest) -> NextLapPredictionBatchResult:
    if not model_store.model_loaded:
        raise HTTPException(status_code=503, detail="Prediction model is not loaded.")

    predictions = model_store.predict_next_lap([item.model_dump() for item in request.items])
    return NextLapPredictionBatchResult(
        predictions=[
            NextLapPredictionResult(
                predicted_next_lap_time=value,
                model_version=_current_model_version(),
            )
            for value in predictions
        ],
        model_version=_current_model_version(),
    )
