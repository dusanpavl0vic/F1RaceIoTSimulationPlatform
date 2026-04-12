namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record CompareDriversOnLapDto(
    string SessionId,
    int LapNumber,
    DriverLapSeriesDto Left,
    DriverLapSeriesDto Right,
    IReadOnlyList<SegmentComparisonBucketDto> BucketCompare);
