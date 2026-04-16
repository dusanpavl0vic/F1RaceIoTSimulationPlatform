using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;

namespace F1.RaceState.Service.Infrastructure.Grpc;

public sealed class TelemetryAnalyticsGateway(AnalyticsGrpc.TelemetryAnalytics.TelemetryAnalyticsClient client)
{
    private readonly AnalyticsGrpc.TelemetryAnalytics.TelemetryAnalyticsClient _client = client;

    public async Task<AnalyticsGrpc.ListSessionsResponse> ListSessionsAsync(CancellationToken cancellationToken)
        => await _client.ListSessionsAsync(new AnalyticsGrpc.ListSessionsRequest(), cancellationToken: cancellationToken).ResponseAsync;

    public async Task<AnalyticsGrpc.SessionOverviewResponse> GetSessionOverviewAsync(string sessionId, CancellationToken cancellationToken)
        => await _client.GetSessionOverviewAsync(new AnalyticsGrpc.SessionOverviewRequest { SessionId = sessionId }, cancellationToken: cancellationToken).ResponseAsync;

    public async Task<AnalyticsGrpc.SessionDriversResponse> GetSessionDriversAsync(string sessionId, CancellationToken cancellationToken)
        => await _client.GetSessionDriversAsync(new AnalyticsGrpc.SessionDriversRequest { SessionId = sessionId }, cancellationToken: cancellationToken).ResponseAsync;

    public async Task<AnalyticsGrpc.DriverStintsResponse> GetDriverStintsAsync(string sessionId, int driverNumber, CancellationToken cancellationToken)
        => await _client.GetDriverStintsAsync(
            new AnalyticsGrpc.DriverStintsRequest
            {
                SessionId = sessionId,
                DriverNumber = driverNumber
            },
            cancellationToken: cancellationToken).ResponseAsync;

    public async Task<AnalyticsGrpc.DriverLapSummariesResponse> GetDriverLapSummariesAsync(string sessionId, int driverNumber, CancellationToken cancellationToken)
        => await _client.GetDriverLapSummariesAsync(
            new AnalyticsGrpc.DriverLapSummariesRequest
            {
                SessionId = sessionId,
                DriverNumber = driverNumber
            },
            cancellationToken: cancellationToken).ResponseAsync;

    public async Task<AnalyticsGrpc.DriverSegmentBucketsResponse> GetDriverSegmentBucketsAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        int bucketCount,
        CancellationToken cancellationToken)
        => await _client.GetDriverSegmentBucketsAsync(
            new AnalyticsGrpc.DriverSegmentBucketsRequest
            {
                SessionId = sessionId,
                DriverNumber = driverNumber,
                LapNumber = lapNumber,
                BucketCount = bucketCount
            },
            cancellationToken: cancellationToken).ResponseAsync;

    public async Task<AnalyticsGrpc.CompareDriversOnLapResponse> CompareDriversOnLapAsync(
        string sessionId,
        int leftDriverNumber,
        int rightDriverNumber,
        int lapNumber,
        int bucketCount,
        CancellationToken cancellationToken)
        => await _client.CompareDriversOnLapAsync(
            new AnalyticsGrpc.CompareDriversOnLapRequest
            {
                SessionId = sessionId,
                LeftDriverNumber = leftDriverNumber,
                RightDriverNumber = rightDriverNumber,
                LapNumber = lapNumber,
                BucketCount = bucketCount
            },
            cancellationToken: cancellationToken).ResponseAsync;

    public async Task<AnalyticsGrpc.DriverTelemetryResponse> GetLatestDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        CancellationToken cancellationToken)
        => await _client.GetLatestDriverTelemetryAsync(
            new AnalyticsGrpc.DriverTelemetryRequest
            {
                SessionId = sessionId,
                DriverNumber = driverNumber,
                MaxSamples = maxSamples
            },
            cancellationToken: cancellationToken).ResponseAsync;

    public async IAsyncEnumerable<AnalyticsGrpc.LiveTelemetrySample> StreamDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int recentSampleCount,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var call = _client.StreamDriverTelemetry(
            new AnalyticsGrpc.DriverTelemetryStreamRequest
            {
                SessionId = sessionId,
                DriverNumber = driverNumber,
                RecentSampleCount = recentSampleCount
            },
            cancellationToken: cancellationToken);

        while (await call.ResponseStream.MoveNext(cancellationToken))
        {
            yield return call.ResponseStream.Current;
        }
    }
}
