using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Infrastructure.Grpc;
using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;

namespace F1.RaceState.Service.Application.Queries;

public sealed class RaceStateAnalyticsQueryHandler(
    TelemetryAnalyticsGateway telemetryAnalyticsGateway,
    IRaceStateStore raceStateStore)
{
    private readonly TelemetryAnalyticsGateway _telemetryAnalyticsGateway = telemetryAnalyticsGateway;
    private readonly IRaceStateStore _raceStateStore = raceStateStore;

    public string? Handle(ResolveAnalyticsSessionIdQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.RequestedSessionId))
        {
            return query.RequestedSessionId;
        }

        return _raceStateStore.GetSnapshot().SessionId;
    }

    public async Task<RaceAnalyticsDriverSegmentBucketsViewModel> HandleAsync(
        GetDriverSegmentBucketsQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetDriverSegmentBucketsAsync(
            query.SessionId,
            query.DriverNumber,
            query.LapNumber,
            query.BucketCount,
            cancellationToken);

        return new RaceAnalyticsDriverSegmentBucketsViewModel(
            query.SessionId,
            query.DriverNumber,
            query.LapNumber,
            query.BucketCount,
            response.Buckets);
    }

    public async Task<RaceAnalyticsDriverLapTelemetryViewModel> HandleAsync(
        GetDriverLapTelemetryQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetDriverLapTelemetryAsync(
            query.SessionId,
            query.DriverNumber,
            query.LapNumber,
            query.Metrics,
            cancellationToken);

        return new RaceAnalyticsDriverLapTelemetryViewModel(
            query.SessionId,
            query.DriverNumber,
            query.LapNumber,
            response.ReturnedMetrics,
            response.Samples.Select(sample => MapTelemetrySample(sample, response.ReturnedMetrics)).ToArray());
    }

    public async Task<RaceAnalyticsDriverTelemetryViewModel> HandleAsync(
        GetLatestDriverTelemetryQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetLatestDriverTelemetryAsync(
            query.SessionId,
            query.DriverNumber,
            query.MaxSamples,
            query.SinceTimestamp,
            query.Metrics,
            cancellationToken);

        return new RaceAnalyticsDriverTelemetryViewModel(
            query.SessionId,
            query.DriverNumber,
            response.ReturnedMetrics,
            response.Samples.Select(sample => MapTelemetrySample(sample, response.ReturnedMetrics)).ToArray());
    }

    private static RaceAnalyticsTelemetrySampleViewModel MapTelemetrySample(
        AnalyticsGrpc.LiveTelemetrySample sample,
        IReadOnlyCollection<string> metrics)
    {
        var requestedMetrics = new HashSet<string>(metrics, StringComparer.OrdinalIgnoreCase);

        return new RaceAnalyticsTelemetrySampleViewModel(
            sample.SessionId,
            sample.DriverNumber,
            sample.LapNumber,
            sample.StintNumber,
            sample.SampleIndex,
            sample.Timestamp)
        {
            Speed = requestedMetrics.Contains("speed") ? sample.Speed : null,
            Rpm = requestedMetrics.Contains("rpm") ? sample.Rpm : null,
            ThrottlePct = requestedMetrics.Contains("throttlePct") ? sample.ThrottlePct : null,
            RawThrottle = requestedMetrics.Contains("rawThrottle") ? sample.RawThrottle : null,
            BrakePct = requestedMetrics.Contains("brakePct") ? sample.BrakePct : null,
            RawBrake = requestedMetrics.Contains("rawBrake") ? sample.RawBrake : null,
            Gear = requestedMetrics.Contains("gear") ? sample.Gear : null,
            DrsEnabled = requestedMetrics.Contains("drsEnabled") ? sample.DrsEnabled : null
        };
    }
}
