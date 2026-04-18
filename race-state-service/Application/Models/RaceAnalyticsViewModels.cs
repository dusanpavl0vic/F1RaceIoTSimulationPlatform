using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceAnalyticsSessionsViewModel(IReadOnlyList<AnalyticsGrpc.SessionOverview> Sessions);

public sealed record RaceAnalyticsSessionDriversViewModel(
    string SessionId,
    IReadOnlyList<AnalyticsGrpc.DriverSessionOverview> Drivers);

public sealed record RaceAnalyticsDriverStintsViewModel(
    string SessionId,
    int DriverNumber,
    string DriverName,
    IReadOnlyList<AnalyticsGrpc.DriverStint> Stints);

public sealed record RaceAnalyticsDriverLapSummariesViewModel(
    string SessionId,
    int DriverNumber,
    string DriverName,
    IReadOnlyList<AnalyticsGrpc.DriverLapSummary> Laps);

public sealed record RaceAnalyticsDriverSegmentBucketsViewModel(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    int BucketCount,
    IReadOnlyList<AnalyticsGrpc.SegmentBucket> Buckets);

public sealed record RaceAnalyticsDriverTelemetryViewModel(
    string SessionId,
    int DriverNumber,
    IReadOnlyList<AnalyticsGrpc.LiveTelemetrySample> Samples);
