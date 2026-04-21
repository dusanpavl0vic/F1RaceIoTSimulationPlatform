using System.Text.Json.Serialization;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceAnalyticsDriverLapTelemetryViewModel(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    IReadOnlyList<string> Metrics,
    IReadOnlyList<RaceAnalyticsTelemetrySampleViewModel> Samples);

public sealed record RaceAnalyticsTelemetrySampleViewModel(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    int StintNumber,
    int SampleIndex,
    string Timestamp)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Speed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rpm { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ThrottlePct { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RawThrottle { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? BrakePct { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RawBrake { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Gear { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DrsEnabled { get; init; }
}

public sealed record RaceAnalyticsTyreStintStrategyViewModel(
    string SessionId,
    int TotalLaps,
    IReadOnlyList<RaceAnalyticsTyreStintDriverViewModel> Drivers);

public sealed record RaceAnalyticsTyreStintDriverViewModel(
    int DriverNumber,
    string DriverName,
    string? TeamName,
    string? TeamColor,
    int? GridPosition,
    int? Position,
    IReadOnlyList<RaceAnalyticsTyreStintViewModel> Stints);

public sealed record RaceAnalyticsTyreStintViewModel(
    int StintNumber,
    string? Compound,
    bool? TyreIsNew,
    int StartLap,
    int EndLap,
    int LapCount);
