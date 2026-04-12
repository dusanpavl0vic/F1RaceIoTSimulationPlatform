namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record TelemetryPointDto(
    double ProgressPct,
    string Timestamp,
    int Speed,
    int ThrottlePct,
    double BrakePct,
    int Gear,
    bool DrsEnabled);
