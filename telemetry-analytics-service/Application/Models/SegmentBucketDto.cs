namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record SegmentBucketDto(
    int BucketIndex,
    double StartProgressPct,
    double EndProgressPct,
    double AverageSpeed,
    int MaxSpeed,
    double AverageThrottlePct,
    double BrakeUsagePct,
    double DrsUsagePct,
    string Behavior);
