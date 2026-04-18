using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Infrastructure.Grpc;
using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateAnalyticsService(
    TelemetryAnalyticsGateway telemetryAnalyticsGateway,
    IRaceStateStore raceStateStore)
{
    private readonly TelemetryAnalyticsGateway _telemetryAnalyticsGateway = telemetryAnalyticsGateway;
    private readonly IRaceStateStore _raceStateStore = raceStateStore;

    public async Task<RaceAnalyticsSessionsViewModel> ListSessionsAsync(CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.ListSessionsAsync(cancellationToken);
        return new RaceAnalyticsSessionsViewModel(response.Sessions);
    }

    public string? ResolveSessionId(string? requestedSessionId)
    {
        if (!string.IsNullOrWhiteSpace(requestedSessionId))
        {
            return requestedSessionId;
        }

        return _raceStateStore.GetSnapshot().SessionId;
    }

    public async Task<AnalyticsGrpc.SessionOverview?> GetSessionOverviewAsync(string sessionId, CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetSessionOverviewAsync(sessionId, cancellationToken);
        return response.Session;
    }

    public async Task<RaceAnalyticsSessionDriversViewModel> GetSessionDriversAsync(string sessionId, CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetSessionDriversAsync(sessionId, cancellationToken);
        return new RaceAnalyticsSessionDriversViewModel(sessionId, response.Drivers);
    }

    public async Task<RaceAnalyticsDriverStintsViewModel> GetDriverStintsAsync(string sessionId, int driverNumber, CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetDriverStintsAsync(sessionId, driverNumber, cancellationToken);
        return new RaceAnalyticsDriverStintsViewModel(sessionId, driverNumber, response.DriverName, response.Stints);
    }

    public async Task<RaceAnalyticsDriverLapSummariesViewModel> GetDriverLapSummariesAsync(string sessionId, int driverNumber, CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetDriverLapSummariesAsync(sessionId, driverNumber, cancellationToken);
        return new RaceAnalyticsDriverLapSummariesViewModel(sessionId, driverNumber, response.DriverName, response.Laps);
    }

    public async Task<RaceAnalyticsDriverSegmentBucketsViewModel> GetDriverSegmentBucketsAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        int bucketCount,
        CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetDriverSegmentBucketsAsync(
            sessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            cancellationToken);

        return new RaceAnalyticsDriverSegmentBucketsViewModel(
            sessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            response.Buckets);
    }

    public async Task<RaceAnalyticsDriverTelemetryViewModel> GetLatestDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetLatestDriverTelemetryAsync(
            sessionId,
            driverNumber,
            maxSamples,
            cancellationToken);

        return new RaceAnalyticsDriverTelemetryViewModel(sessionId, driverNumber, response.Samples);
    }

    public async Task<AnalyticsGrpc.CompareDriversOnLapResponse> CompareDriversOnLapAsync(
        string sessionId,
        int leftDriverNumber,
        int rightDriverNumber,
        int lapNumber,
        int bucketCount,
        CancellationToken cancellationToken)
        => await _telemetryAnalyticsGateway.CompareDriversOnLapAsync(
            sessionId,
            leftDriverNumber,
            rightDriverNumber,
            lapNumber,
            bucketCount,
            cancellationToken);
}
