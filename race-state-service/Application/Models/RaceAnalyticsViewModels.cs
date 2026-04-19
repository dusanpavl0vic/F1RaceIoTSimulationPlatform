using System.Text.Json.Serialization;
using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceAnalyticsDriverSegmentBucketsViewModel(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    int BucketCount,
    IReadOnlyList<AnalyticsGrpc.SegmentBucket> Buckets);

public sealed record RaceAnalyticsDriverTelemetryViewModel(
    string SessionId,
    int DriverNumber,
    IReadOnlyList<string> Metrics,
    IReadOnlyList<RaceAnalyticsTelemetrySampleViewModel> Samples);

public sealed record RaceAnalyticsDriverLapTelemetryViewModel(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    IReadOnlyList<string> Metrics,
    IReadOnlyList<RaceAnalyticsTelemetrySampleViewModel> Samples);

public sealed record RaceAnalyticsTelemetrySampleViewModel(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    int StintNumber,
    int SampleIndex,
    string Timestamp)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Speed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rpm { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ThrottlePct { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RawThrottle { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? BrakePct { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RawBrake { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Gear { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DrsEnabled { get; init; }
}
