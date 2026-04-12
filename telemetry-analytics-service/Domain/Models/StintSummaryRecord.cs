namespace F1.TelemetryAnalytics.Service.Domain.Models;

public sealed record StintSummaryRecord(
    string SessionId,
    int DriverNumber,
    int StintNumber,
    string? Compound,
    bool? TyreIsNew,
    int? StartLap,
    int? EndLap,
    int? LapCount,
    DateTimeOffset UpdatedAt);
