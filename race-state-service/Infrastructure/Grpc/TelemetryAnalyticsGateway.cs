using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;
using Grpc.Core;

namespace F1.RaceState.Service.Infrastructure.Grpc;

public sealed class TelemetryAnalyticsGateway(AnalyticsGrpc.TelemetryAnalytics.TelemetryAnalyticsClient client)
{
    private readonly AnalyticsGrpc.TelemetryAnalytics.TelemetryAnalyticsClient _client = client;

    public async Task<AnalyticsGrpc.TyreStintStrategyResponse> GetTyreStintStrategyAsync(
        string sessionId,
        CancellationToken cancellationToken)
        => await _client.GetTyreStintStrategyAsync(
            new AnalyticsGrpc.TyreStintStrategyRequest
            {
                SessionId = sessionId
            },
            cancellationToken: cancellationToken).ResponseAsync;

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
