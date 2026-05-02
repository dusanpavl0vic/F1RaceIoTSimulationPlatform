from __future__ import annotations

import json
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Any

import pandas as pd

from app.model_contract import FEATURE_COLUMNS, TARGET_COLUMN

RELEVANT_EVENT_TYPES = {
    "timing.driver.updated",
    "timing.app.updated",
    "lap.series.updated",
    "tyres.current.updated",
    "tyres.stint.updated",
    "car.telemetry.updated",
}


@dataclass(slots=True)
class TelemetryLapAggregate:
    sample_count: int = 0
    speed_sum: float = 0.0
    max_speed: float | None = None
    rpm_sum: float = 0.0
    throttle_sum: float = 0.0
    raw_brake_sum: float = 0.0
    drs_open_count: int = 0
    gear_changes: int = 0
    previous_gear: int | None = None

    def add_sample(self, telemetry: dict[str, Any]) -> None:
        self.sample_count += 1

        speed = _parse_float(telemetry.get("speedKph") or telemetry.get("speed"))
        if speed is not None:
            self.speed_sum += speed
            self.max_speed = speed if self.max_speed is None else max(self.max_speed, speed)

        rpm = _parse_float(telemetry.get("rpm"))
        if rpm is not None:
            self.rpm_sum += rpm

        throttle_pct = _parse_float(telemetry.get("throttlePct"))
        if throttle_pct is not None:
            self.throttle_sum += throttle_pct

        raw_brake = _parse_float(telemetry.get("rawBrake"))
        if raw_brake is not None:
            self.raw_brake_sum += raw_brake

        drs_enabled = _parse_bool(telemetry.get("drsEnabled"))
        if drs_enabled:
            self.drs_open_count += 1

        gear = _parse_int(telemetry.get("gear"))
        if gear is not None:
            if self.previous_gear is not None and self.previous_gear != gear:
                self.gear_changes += 1
            self.previous_gear = gear

    def to_feature_dict(self) -> dict[str, float | int | None]:
        if self.sample_count <= 0:
            return {
                "avg_speed_last_lap": None,
                "max_speed_last_lap": None,
                "avg_rpm_last_lap": None,
                "avg_throttle_pct_last_lap": None,
                "avg_raw_brake_last_lap": None,
                "drs_open_ratio_last_lap": None,
                "gear_changes_last_lap": None,
            }

        return {
            "avg_speed_last_lap": self.speed_sum / self.sample_count if self.speed_sum else None,
            "max_speed_last_lap": self.max_speed,
            "avg_rpm_last_lap": self.rpm_sum / self.sample_count if self.rpm_sum else None,
            "avg_throttle_pct_last_lap": self.throttle_sum / self.sample_count if self.throttle_sum else None,
            "avg_raw_brake_last_lap": self.raw_brake_sum / self.sample_count if self.raw_brake_sum else None,
            "drs_open_ratio_last_lap": self.drs_open_count / self.sample_count,
            "gear_changes_last_lap": self.gear_changes,
        }


@dataclass(slots=True)
class DriverState:
    session_id: str
    driver_number: int
    position: int | None = None
    gap_to_leader: float | None = None
    gap_to_ahead: float | None = None
    stint_number: int | None = None
    tyre_compound: str | None = None
    tyre_is_new: bool | None = None
    tyre_laps_on_set: int | None = None
    best_lap_time: float | None = None
    in_pit: bool | None = None
    completed_laps: int = 0
    current_lap_number: int = 1
    lap_history: list[float] = field(default_factory=list)
    telemetry_by_lap: dict[int, TelemetryLapAggregate] = field(default_factory=dict)
    pending_features_by_target_lap: dict[int, dict[str, Any]] = field(default_factory=dict)
    recorded_laps: set[int] = field(default_factory=set)

    def aggregate_for_active_lap(self) -> TelemetryLapAggregate:
        return self.telemetry_by_lap.setdefault(self.current_lap_number, TelemetryLapAggregate())


def build_training_dataset(canonical_dir: Path) -> pd.DataFrame:
    events = _load_events(canonical_dir)
    if not events:
        return pd.DataFrame(columns=["session_id", *FEATURE_COLUMNS, TARGET_COLUMN])

    rows: list[dict[str, Any]] = []
    drivers: dict[tuple[str, int], DriverState] = {}
    sample_index = 0

    for event in events:
        event_type = event.get("eventType")
        payload = event.get("payload") or {}

        driver_number = _parse_int(event.get("driverNumber"))
        session_id = _as_text(event.get("sessionId")) or ""
        if driver_number is None or not session_id:
            continue

        state = drivers.setdefault((session_id, driver_number), DriverState(session_id=session_id, driver_number=driver_number))

        if event_type == "timing.driver.updated":
            timing = payload.get("timing") or {}
            state.position = _parse_int(timing.get("Position")) or state.position
            gap_to_leader = _parse_gap_seconds(timing.get("GapToLeader"))
            state.gap_to_leader = gap_to_leader if gap_to_leader is not None else state.gap_to_leader
            gap_to_ahead = _parse_gap_seconds((timing.get("IntervalToPositionAhead") or {}).get("Value"))
            state.gap_to_ahead = gap_to_ahead if gap_to_ahead is not None else state.gap_to_ahead
            in_pit = _parse_bool(timing.get("InPit"))
            state.in_pit = in_pit if in_pit is not None else state.in_pit

            incoming_completed_laps = _parse_int(timing.get("NumberOfLaps"))
            if incoming_completed_laps is not None and incoming_completed_laps > state.completed_laps:
                state.completed_laps = incoming_completed_laps
                state.current_lap_number = max(state.current_lap_number, incoming_completed_laps + 1)

            best_lap_time = _parse_lap_time_seconds(timing.get("BestLapTime"))
            if best_lap_time is not None:
                state.best_lap_time = best_lap_time if state.best_lap_time is None else min(state.best_lap_time, best_lap_time)

            completed_lap_time = _parse_lap_time_seconds(timing.get("LastLapTime"))
            completed_lap_number = incoming_completed_laps or state.completed_laps
            if completed_lap_time is not None and completed_lap_number is not None and completed_lap_number > 0:
                if completed_lap_number not in state.recorded_laps:
                    sample_index = _record_completed_lap(
                        state=state,
                        lap_number=completed_lap_number,
                        lap_time_seconds=completed_lap_time,
                        rows=rows,
                        sample_index=sample_index,
                    )
            continue

        if event_type == "lap.series.updated":
            lap_series = payload.get("lapSeries") or {}
            completed_lap_number, position = _resolve_lap_series_position(lap_series.get("LapPosition"))
            if position is not None:
                state.position = position
            if completed_lap_number is not None and completed_lap_number > state.completed_laps:
                state.completed_laps = completed_lap_number
                state.current_lap_number = max(state.current_lap_number, completed_lap_number + 1)
            continue

        if event_type == "tyres.current.updated":
            tyres = payload.get("tyres") or {}
            state.tyre_compound = _first_non_empty(tyres.get("Compound"), state.tyre_compound)
            tyre_is_new = _parse_bool(tyres.get("New"))
            state.tyre_is_new = tyre_is_new if tyre_is_new is not None else state.tyre_is_new
            continue

        if event_type == "tyres.stint.updated":
            _apply_stints(state, payload.get("stints"))
            continue

        if event_type == "timing.app.updated":
            timing_app = payload.get("timingApp") or {}
            _apply_stints(state, timing_app.get("Stints"))
            continue

        if event_type == "car.telemetry.updated":
            telemetry = payload.get("telemetry")
            if isinstance(telemetry, dict):
                state.aggregate_for_active_lap().add_sample(telemetry)

    dataset = pd.DataFrame(rows)
    if dataset.empty:
        return pd.DataFrame(columns=["session_id", *FEATURE_COLUMNS, TARGET_COLUMN])

    dataset = dataset.sort_values(["sample_index", "session_id", "driver_number", "lap_number"]).reset_index(drop=True)
    return dataset


def _record_completed_lap(
    *,
    state: DriverState,
    lap_number: int,
    lap_time_seconds: float,
    rows: list[dict[str, Any]],
    sample_index: int,
) -> int:
    state.recorded_laps.add(lap_number)

    pending = state.pending_features_by_target_lap.pop(lap_number, None)
    if pending is not None:
        pending[TARGET_COLUMN] = lap_time_seconds
        rows.append(pending)

    state.best_lap_time = lap_time_seconds if state.best_lap_time is None else min(state.best_lap_time, lap_time_seconds)
    telemetry_aggregate = state.telemetry_by_lap.pop(lap_number, TelemetryLapAggregate())
    lap_history_for_features = [*state.lap_history, lap_time_seconds]

    feature_row: dict[str, Any] = {
        "sample_index": sample_index,
        "session_id": state.session_id,
        "driver_number": state.driver_number,
        "lap_number": lap_number,
        "position": state.position,
        "lap_time_last": lap_time_seconds,
        "lap_time_best": state.best_lap_time,
        "lap_time_avg_last_3": _rolling_average(lap_history_for_features, 3),
        "lap_time_avg_last_5": _rolling_average(lap_history_for_features, 5),
        "gap_to_leader": state.gap_to_leader,
        "gap_to_ahead": state.gap_to_ahead,
        "stint_number": state.stint_number,
        "tyre_compound": state.tyre_compound,
        "tyre_is_new": state.tyre_is_new,
        "tyre_laps_on_set": state.tyre_laps_on_set,
        "in_pit": state.in_pit,
    }
    feature_row.update(telemetry_aggregate.to_feature_dict())

    state.pending_features_by_target_lap[lap_number + 1] = feature_row
    state.lap_history.append(lap_time_seconds)
    state.completed_laps = max(state.completed_laps, lap_number)
    state.current_lap_number = max(state.current_lap_number, lap_number + 1)
    return sample_index + 1


def _load_events(canonical_dir: Path) -> list[dict[str, Any]]:
    if not canonical_dir.exists():
        return []

    loaded: list[tuple[datetime, int, dict[str, Any]]] = []
    for path in sorted(canonical_dir.glob("f1_canonical_*.jsonl")):
        try:
            with path.open("r", encoding="utf-8") as handle:
                for line in handle:
                    stripped = line.strip()
                    if not stripped:
                        continue

                    try:
                        event = json.loads(stripped)
                    except json.JSONDecodeError:
                        continue

                    event_type = event.get("eventType")
                    if event_type not in RELEVANT_EVENT_TYPES:
                        continue

                    sort_time = _parse_datetime(event.get("publishTime")) or _parse_datetime(event.get("eventTime"))
                    if sort_time is None:
                        continue

                    loaded.append((sort_time, int(event.get("sequence") or 0), event))
        except OSError:
            continue

    loaded.sort(key=lambda item: (item[0], item[1]))
    return [event for _, __, event in loaded]


def _apply_stints(state: DriverState, stints_node: Any) -> None:
    if not isinstance(stints_node, dict) or len(stints_node) == 0:
        return

    parsed = [
        (_parse_int(stint_number), payload)
        for stint_number, payload in stints_node.items()
        if isinstance(payload, dict)
    ]
    parsed = [(stint_number, payload) for stint_number, payload in parsed if stint_number is not None]
    if not parsed:
        return

    latest_stint_index, latest_stint = sorted(parsed, key=lambda item: item[0])[-1]
    state.stint_number = latest_stint_index + 1
    state.tyre_compound = _first_non_empty(latest_stint.get("Compound"), state.tyre_compound)

    tyre_is_new = _parse_bool(latest_stint.get("New"))
    state.tyre_is_new = tyre_is_new if tyre_is_new is not None else state.tyre_is_new

    total_laps = _parse_int(latest_stint.get("TotalLaps"))
    if total_laps is not None:
        state.tyre_laps_on_set = total_laps


def _resolve_lap_series_position(lap_position_node: Any) -> tuple[int | None, int | None]:
    if isinstance(lap_position_node, dict):
        parsed = []
        for lap_number, position in lap_position_node.items():
            parsed_lap_number = _parse_int(lap_number)
            parsed_position = _parse_int(position)
            if parsed_lap_number is not None and parsed_position is not None:
                parsed.append((parsed_lap_number, parsed_position))

        if parsed:
            latest_lap_number, latest_position = sorted(parsed, key=lambda item: item[0])[-1]
            return latest_lap_number, latest_position

    if isinstance(lap_position_node, list):
        values = [_parse_int(value) for value in lap_position_node]
        filtered = [value for value in values if value is not None]
        if filtered:
            return None, filtered[-1]

    return None, None


def _rolling_average(values: list[float], window: int) -> float | None:
    if not values:
        return None

    selected = values[-window:]
    return sum(selected) / len(selected)


def _parse_datetime(value: Any) -> datetime | None:
    if value is None:
        return None

    try:
        normalized = str(value).replace("Z", "+00:00")
        return datetime.fromisoformat(normalized)
    except ValueError:
        return None


def _parse_int(value: Any) -> int | None:
    if value is None or value == "":
        return None
    try:
        return int(str(value))
    except (TypeError, ValueError):
        return None


def _parse_float(value: Any) -> float | None:
    if value is None or value == "":
        return None
    try:
        return float(str(value))
    except (TypeError, ValueError):
        return None


def _parse_bool(value: Any) -> bool | None:
    if value is None or value == "":
        return None

    normalized = str(value).strip().lower()
    if normalized in {"true", "1", "yes", "y", "on"}:
        return True
    if normalized in {"false", "0", "no", "n", "off"}:
        return False
    return None


def _parse_gap_seconds(value: Any) -> float | None:
    text = _extract_value_text(value)
    if not text:
        return None

    normalized = text.strip().lstrip("+")
    try:
        return float(normalized)
    except ValueError:
        return None


def _parse_lap_time_seconds(value: Any) -> float | None:
    text = _extract_value_text(value)
    if not text:
        return None

    normalized = text.strip().upper()
    if normalized in {"PIT", "PIT IN", "PIT OUT", "STOP"}:
        return None

    if ":" in normalized:
        parts = normalized.split(":")
        try:
            minutes = int(parts[0])
            seconds = float(parts[1])
            return minutes * 60 + seconds
        except (IndexError, ValueError):
            return None

    try:
        return float(normalized)
    except ValueError:
        return None


def _extract_value_text(value: Any) -> str | None:
    if isinstance(value, dict):
        return _as_text(value.get("Value"))
    return _as_text(value)


def _as_text(value: Any) -> str | None:
    if value is None:
        return None
    text = str(value).strip()
    return text or None


def _first_non_empty(*values: Any) -> str | None:
    for value in values:
        text = _as_text(value)
        if text:
            return text
    return None
