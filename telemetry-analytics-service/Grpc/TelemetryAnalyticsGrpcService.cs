using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Grpc.Mappers;
using Grpc.Core;

namespace F1.TelemetryAnalytics.Service.Grpc;

public sealed class TelemetryAnalyticsGrpcService(
    IAnalyticsQueryService analyticsQueryService) : TelemetryAnalytics.TelemetryAnalyticsBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService = analyticsQueryService;

    public override async Task<TyreStintStrategyResponse> GetTyreStintStrategy(
        TyreStintStrategyRequest request,
        global::Grpc.Core.ServerCallContext context)
    {
        var strategy = await _analyticsQueryService.GetTyreStintStrategyAsync(
            request.SessionId,
            context.CancellationToken);

        var response = TelemetryAnalyticsGrpcMapper.MapTyreStintStrategy(strategy);
        response.Drivers.AddRange(strategy.Drivers.Select(TelemetryAnalyticsGrpcMapper.MapTyreStintDriver));
        return response;
    }

    public override Task<DriverLapTelemetryResponse> GetDriverLapTelemetry(
        DriverLapTelemetryRequest request,
        ServerCallContext context)
        => BuildDriverLapTelemetryResponseAsync(request, context.CancellationToken);

    private async Task<DriverLapTelemetryResponse> BuildDriverLapTelemetryResponseAsync(
        DriverLapTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var samples = await _analyticsQueryService.GetDriverLapTelemetryAsync(
                request.SessionId,
                request.DriverNumber,
                request.LapNumber,
                request.RequestedMetrics,
                cancellationToken);

            var response = new DriverLapTelemetryResponse
            {
                SessionId = request.SessionId,
                DriverNumber = request.DriverNumber,
                LapNumber = request.LapNumber
            };

            response.ReturnedMetrics.AddRange(samples.Metrics);
            response.Samples.AddRange(samples.Samples.Select(TelemetryAnalyticsGrpcMapper.MapLiveTelemetrySample));
            return response;
        }
        catch (ArgumentException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
    }
}
