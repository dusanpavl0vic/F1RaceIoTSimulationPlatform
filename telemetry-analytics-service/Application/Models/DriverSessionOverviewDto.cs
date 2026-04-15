namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record DriverSessionOverviewDto(
    int DriverNumber,
    string DriverName,
    string? TeamName,
    string? TeamColor,
    int? GridPosition,
    int? Position,
    int? CompletedLaps,
    string? BestLapTime,
    int? BestLapTimeMs,
    string? LastLapTime,
    int? LastLapTimeMs,
    string? CurrentCompound,
    int? TyreLaps,
    int? CurrentStintNumber,
    string? GapToLeader,
    string? IntervalToAhead);
