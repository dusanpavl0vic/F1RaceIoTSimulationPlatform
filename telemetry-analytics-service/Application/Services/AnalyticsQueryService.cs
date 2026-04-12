using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;
using Microsoft.Extensions.Options;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;

namespace F1.TelemetryAnalytics.Service.Application.Services;

public sealed class AnalyticsQueryService(
    IAnalyticsRepository analyticsRepository,
    IInfluxTelemetryClient influxTelemetryClient,
    IOptions<AnalyticsOptions> analyticsOptions) : IAnalyticsQueryService
{
    private readonly IAnalyticsRepository _analyticsRepository = analyticsRepository;
    private readonly IInfluxTelemetryClient _influxTelemetryClient = influxTelemetryClient;
    private readonly AnalyticsOptions _analyticsOptions = analyticsOptions.Value;

    public Task<(string DriverName, IReadOnlyList<DriverLapSummaryDto> Laps)> GetDriverLapSummariesAsync(
        string sessionId,
        int driverNumber,
        CancellationToken cancellationToken)
        => _analyticsRepository.GetDriverLapSummariesAsync(sessionId, driverNumber, cancellationToken);

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

    public async Task<CompareDriversOnLapDto> CompareDriversOnLapAsync(
        string sessionId,
        int leftDriverNumber,
        int rightDriverNumber,
        int lapNumber,
        int bucketCount,
        CancellationToken cancellationToken)
    {
        var effectiveBucketCount = bucketCount <= 0 ? _analyticsOptions.LapCompareBucketCount : bucketCount;

        var (leftDriverName, _) = await _analyticsRepository.GetDriverLapSummariesAsync(sessionId, leftDriverNumber, cancellationToken);
        var (rightDriverName, _) = await _analyticsRepository.GetDriverLapSummariesAsync(sessionId, rightDriverNumber, cancellationToken);
        var leftTelemetry = await _influxTelemetryClient.QueryLapTelemetryAsync(sessionId, leftDriverNumber, lapNumber, cancellationToken);
        var rightTelemetry = await _influxTelemetryClient.QueryLapTelemetryAsync(sessionId, rightDriverNumber, lapNumber, cancellationToken);

        var leftBuckets = BuildSegmentBuckets(leftTelemetry, effectiveBucketCount);
        var rightBuckets = BuildSegmentBuckets(rightTelemetry, effectiveBucketCount);
        var pairedBuckets = Enumerable.Range(0, Math.Min(leftBuckets.Count, rightBuckets.Count))
            .Select(index => new SegmentComparisonBucketDto(
                index + 1,
                leftBuckets[index].StartProgressPct,
                leftBuckets[index].EndProgressPct,
                leftBuckets[index].Behavior,
                rightBuckets[index].Behavior,
                leftBuckets[index].AverageSpeed - rightBuckets[index].AverageSpeed,
                leftBuckets[index].AverageThrottlePct - rightBuckets[index].AverageThrottlePct,
                leftBuckets[index].BrakeUsagePct - rightBuckets[index].BrakeUsagePct))
            .ToArray();

        return new CompareDriversOnLapDto(
            sessionId,
            lapNumber,
            new DriverLapSeriesDto(leftDriverNumber, leftDriverName, lapNumber, leftTelemetry),
            new DriverLapSeriesDto(rightDriverNumber, rightDriverName, lapNumber, rightTelemetry),
            pairedBuckets);
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
