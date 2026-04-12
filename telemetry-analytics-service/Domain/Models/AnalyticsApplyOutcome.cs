namespace F1.TelemetryAnalytics.Service.Domain.Models;

public sealed record AnalyticsApplyOutcome(
    bool Applied,
    bool SessionChanged,
    bool DriverChanged,
    bool StintChanged,
    AnalyticsDriverState? Driver,
    LapSummaryRecord? CompletedLap,
    StintSummaryRecord? CurrentStint,
    TelemetrySampleRecord? TelemetrySample);
