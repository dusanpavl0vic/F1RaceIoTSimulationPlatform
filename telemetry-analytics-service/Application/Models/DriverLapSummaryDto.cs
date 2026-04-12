namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record DriverLapSummaryDto(
    int LapNumber,
    int? Position,
    string? LapTime,
    int? LapTimeMs,
    string? BestLapTime,
    int? BestLapTimeMs,
    string? Sector1,
    int? Sector1Ms,
    string? Sector2,
    int? Sector2Ms,
    string? Sector3,
    int? Sector3Ms,
    string? Compound,
    bool? TyreIsNew,
    int? TyreLaps,
    int StintNumber,
    double AverageSpeed,
    int MaxSpeed,
    double ThrottlePct,
    double BrakePct,
    double DrsPct);
