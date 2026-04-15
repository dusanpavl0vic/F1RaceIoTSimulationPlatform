namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record DriverStintDto(
    int StintNumber,
    string? Compound,
    bool? TyreIsNew,
    int? StartLap,
    int? EndLap,
    int? LapCount,
    DateTimeOffset UpdatedAt);
