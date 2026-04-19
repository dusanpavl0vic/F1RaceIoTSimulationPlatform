using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;
using Microsoft.Extensions.Options;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;

namespace F1.TelemetryAnalytics.Service.Application.Services;

public sealed class AnalyticsQueryService(
    IInfluxTelemetryClient influxTelemetryClient,
    IOptions<AnalyticsOptions> analyticsOptions) : IAnalyticsQueryService
{
    private readonly IInfluxTelemetryClient _influxTelemetryClient = influxTelemetryClient;
    private readonly AnalyticsOptions _analyticsOptions = analyticsOptions.Value;

    public async Task<IReadOnlyList<SegmentBucketDto>> GetDriverSegmentBucketsAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        int bucketCount,
        CancellationToken cancellationToken)
    {
        var telemetry = await _influxTelemetryClient.QueryLapTelemetryAsync(sessionId, driverNumber, lapNumber, cancellationToken);
        return BuildSegmentBuckets(telemetry, bucketCount <= 0 ? _analyticsOptions.SegmentBucketCount : bucketCount);
    }

    public async Task<DriverTelemetryQueryResultDto> GetDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken)
    {
        var metrics = TelemetryMetricCatalog.NormalizeRequestedMetrics(requestedMetrics);
        var samples = await _influxTelemetryClient.QueryDriverLapTelemetryAsync(
            sessionId,
            driverNumber,
            lapNumber,
            metrics,
            cancellationToken);

        return new DriverTelemetryQueryResultDto(metrics, samples);
    }

    public Task<DriverTelemetryQueryResultDto> GetLatestDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        DateTimeOffset? sinceTimestamp,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken)
        => GetLatestDriverTelemetryCoreAsync(
            sessionId,
            driverNumber,
            maxSamples,
            sinceTimestamp,
            requestedMetrics,
            cancellationToken);

    private async Task<DriverTelemetryQueryResultDto> GetLatestDriverTelemetryCoreAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        DateTimeOffset? sinceTimestamp,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken)
    {
        var metrics = TelemetryMetricCatalog.NormalizeRequestedMetrics(requestedMetrics);
        var samples = await _influxTelemetryClient.QueryDriverTelemetryAsync(
            sessionId,
            driverNumber,
            maxSamples,
            sinceTimestamp,
            metrics,
            cancellationToken);

        return new DriverTelemetryQueryResultDto(metrics, samples);
    }

    private static IReadOnlyList<SegmentBucketDto> BuildSegmentBuckets(IReadOnlyList<TelemetryPointDto> telemetry, int bucketCount)
    {
        if (telemetry.Count == 0 || bucketCount <= 0)
        {
            return [];
        }

        var buckets = new List<SegmentBucketDto>(bucketCount);
        for (var bucketIndex = 0; bucketIndex < bucketCount; bucketIndex++)
        {
            var start = bucketIndex / (double)bucketCount;
            var end = (bucketIndex + 1) / (double)bucketCount;
            var slice = telemetry
                .Where(point =>
                {
                    var normalized = point.ProgressPct / 100d;
                    return normalized >= start && (bucketIndex == bucketCount - 1 ? normalized <= end : normalized < end);
                })
                .ToArray();

            if (slice.Length == 0)
            {
                buckets.Add(new SegmentBucketDto(bucketIndex + 1, start * 100d, end * 100d, 0d, 0, 0d, 0d, 0d, "NO_DATA"));
                continue;
            }

            var avgSpeed = slice.Average(point => point.Speed);
            var maxSpeed = slice.Max(point => point.Speed);
            var avgThrottle = slice.Average(point => point.ThrottlePct);
            var avgBrake = slice.Average(point => point.BrakePct);
            var avgDrs = slice.Average(point => point.DrsEnabled ? 100d : 0d);

            buckets.Add(new SegmentBucketDto(
                bucketIndex + 1,
                start * 100d,
                end * 100d,
                avgSpeed,
                maxSpeed,
                avgThrottle,
                avgBrake,
                avgDrs,
                ClassifyBehavior(avgSpeed, maxSpeed, avgThrottle, avgBrake)));
        }

        return buckets;
    }

    private static string ClassifyBehavior(double averageSpeed, int maxSpeed, double averageThrottle, double averageBrake)
    {
        if (averageBrake >= 35d)
        {
            return "HARD_BRAKING_ZONE";
        }

        if (maxSpeed >= 280 && averageThrottle >= 75d)
        {
            return "TOP_SPEED_ZONE";
        }

        if (averageThrottle >= 65d && averageSpeed <= 210d)
        {
            return "TRACTION_ZONE";
        }

        return "FLOW_ZONE";
    }
}
