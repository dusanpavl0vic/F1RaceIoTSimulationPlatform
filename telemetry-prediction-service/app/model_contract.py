from __future__ import annotations

FEATURE_COLUMNS = [
    "driver_number",
    "lap_number",
    "position",
    "lap_time_last",
    "lap_time_best",
    "lap_time_avg_last_3",
    "lap_time_avg_last_5",
    "gap_to_leader",
    "gap_to_ahead",
    "stint_number",
    "tyre_compound",
    "tyre_is_new",
    "tyre_laps_on_set",
    "in_pit",
    "avg_speed_last_lap",
    "max_speed_last_lap",
    "avg_rpm_last_lap",
    "avg_throttle_pct_last_lap",
    "avg_raw_brake_last_lap",
    "drs_open_ratio_last_lap",
    "gear_changes_last_lap",
]

TARGET_COLUMN = "target_next_lap_time"

CATEGORICAL_COLUMNS = [
    "tyre_compound",
    "tyre_is_new",
    "in_pit",
]

NUMERIC_COLUMNS = [column for column in FEATURE_COLUMNS if column not in CATEGORICAL_COLUMNS]
