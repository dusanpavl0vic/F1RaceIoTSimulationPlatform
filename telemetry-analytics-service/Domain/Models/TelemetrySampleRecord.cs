namespace F1.TelemetryAnalytics.Service.Domain.Models;

public sealed record TelemetrySampleRecord(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    int StintNumber,
    int SampleIndex,
    DateTimeOffset Timestamp,
    int? Speed,
    int? Rpm,
    int? Gear,
    int? ThrottlePct,
    bool? BrakeApplied,
    bool? DrsEnabled);
