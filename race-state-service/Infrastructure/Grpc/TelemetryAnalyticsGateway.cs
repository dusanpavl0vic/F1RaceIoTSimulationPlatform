using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;
using Grpc.Core;

namespace F1.RaceState.Service.Infrastructure.Grpc;

public sealed class TelemetryAnalyticsGateway(AnalyticsGrpc.TelemetryAnalytics.TelemetryAnalyticsClient client)
{
    private readonly AnalyticsGrpc.TelemetryAnalytics.TelemetryAnalyticsClient _client = client;

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

    public async Task<AnalyticsGrpc.DriverTelemetryResponse> GetLatestDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        DateTimeOffset? sinceTimestamp,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AnalyticsGrpc.DriverTelemetryRequest
            {
                SessionId = sessionId,
                DriverNumber = driverNumber,
                MaxSamples = maxSamples,
                SinceTimestamp = sinceTimestamp?.ToString("O") ?? string.Empty
            };

            if (requestedMetrics is not null)
            {
                request.RequestedMetrics.AddRange(requestedMetrics);
            }

            return await _client.GetLatestDriverTelemetryAsync(
                request,
                cancellationToken: cancellationToken).ResponseAsync;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException(exception.Status.Detail, nameof(requestedMetrics), exception);
        }
    }

    public async Task<AnalyticsGrpc.DriverLapTelemetryResponse> GetDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AnalyticsGrpc.DriverLapTelemetryRequest
            {
                SessionId = sessionId,
                DriverNumber = driverNumber,
                LapNumber = lapNumber
            };

            if (requestedMetrics is not null)
            {
                request.RequestedMetrics.AddRange(requestedMetrics);
            }

            return await _client.GetDriverLapTelemetryAsync(
                request,
                cancellationToken: cancellationToken).ResponseAsync;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException(exception.Status.Detail, nameof(requestedMetrics), exception);
        }
    }
}
