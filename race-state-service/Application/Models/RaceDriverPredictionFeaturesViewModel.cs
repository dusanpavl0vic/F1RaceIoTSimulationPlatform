using System.Text.Json.Serialization;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceDriverPredictionFeaturesViewModel(
    string? SessionId,
    int DriverNumber,
    string DriverName,
    int? Position,
    int? LastCompletedLapNumber,
    double? LastCompletedLapTimeSeconds,
    double? LapTimeAvgLast3,
    double? LapTimeAvgLast5,
    int? StintNumber,
    string? TyreCompound,
    bool? TyreIsNew,
    int? TyreLapsOnSet,
    bool InPit,
    double? GapToLeader,
    double? GapToAhead,
    RaceDriverPredictionFeaturePayload? Features);

public sealed record RaceDriverPredictionFeaturePayload(
    [property: JsonPropertyName("driver_number")] int DriverNumber,
    [property: JsonPropertyName("lap_number")] int LapNumber,
    [property: JsonPropertyName("position")] int? Position,
    [property: JsonPropertyName("lap_time_last")] double? LapTimeLast,
    [property: JsonPropertyName("lap_time_best")] double? LapTimeBest,
    [property: JsonPropertyName("lap_time_avg_last_3")] double? LapTimeAvgLast3,
    [property: JsonPropertyName("lap_time_avg_last_5")] double? LapTimeAvgLast5,
    [property: JsonPropertyName("gap_to_leader")] double? GapToLeader,
    [property: JsonPropertyName("gap_to_ahead")] double? GapToAhead,
    [property: JsonPropertyName("stint_number")] int? StintNumber,
    [property: JsonPropertyName("tyre_compound")] string? TyreCompound,
    [property: JsonPropertyName("tyre_is_new")] bool? TyreIsNew,
    [property: JsonPropertyName("tyre_laps_on_set")] int? TyreLapsOnSet,
    [property: JsonPropertyName("in_pit")] bool InPit,
    [property: JsonPropertyName("avg_speed_last_lap")] double? AvgSpeedLastLap,
    [property: JsonPropertyName("max_speed_last_lap")] double? MaxSpeedLastLap,
    [property: JsonPropertyName("avg_rpm_last_lap")] double? AvgRpmLastLap,
    [property: JsonPropertyName("avg_throttle_pct_last_lap")] double? AvgThrottlePctLastLap,
    [property: JsonPropertyName("avg_raw_brake_last_lap")] double? AvgRawBrakeLastLap,
    [property: JsonPropertyName("drs_open_ratio_last_lap")] double? DrsOpenRatioLastLap,
    [property: JsonPropertyName("gear_changes_last_lap")] int? GearChangesLastLap);
