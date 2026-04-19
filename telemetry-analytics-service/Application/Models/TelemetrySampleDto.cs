namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record TelemetrySampleDto(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    int StintNumber,
    int SampleIndex,
    string Timestamp,
    int? Speed,
    int? Rpm,
    int? ThrottlePct,
    int? RawThrottle,
    double? BrakePct,
    int? RawBrake,
    int? Gear,
    bool? DrsEnabled);
