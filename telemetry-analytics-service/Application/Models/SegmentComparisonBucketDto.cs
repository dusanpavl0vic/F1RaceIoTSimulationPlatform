namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record SegmentComparisonBucketDto(
    int BucketIndex,
    double StartProgressPct,
    double EndProgressPct,
    string LeftBehavior,
    string RightBehavior,
    double AverageSpeedDelta,
    double ThrottleDelta,
    double BrakeDelta);
