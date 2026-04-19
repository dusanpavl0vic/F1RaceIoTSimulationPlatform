using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Grpc.Mappers;
using Grpc.Core;

namespace F1.TelemetryAnalytics.Service.Grpc;

public sealed class TelemetryAnalyticsGrpcService(
    IAnalyticsQueryService analyticsQueryService) : TelemetryAnalytics.TelemetryAnalyticsBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService = analyticsQueryService;

    public override async Task<DriverSegmentBucketsResponse> GetDriverSegmentBuckets(
        DriverSegmentBucketsRequest request,
        global::Grpc.Core.ServerCallContext context)
    {
        var buckets = await _analyticsQueryService.GetDriverSegmentBucketsAsync(
            request.SessionId,
            request.DriverNumber,
            request.LapNumber,
            request.BucketCount,
            context.CancellationToken);

        var response = new DriverSegmentBucketsResponse
        {
            SessionId = request.SessionId,
            DriverNumber = request.DriverNumber,
            LapNumber = request.LapNumber
        };

        response.Buckets.AddRange(buckets.Select(TelemetryAnalyticsGrpcMapper.MapSegmentBucket));
        return response;
    }

    public override Task<DriverTelemetryResponse> GetLatestDriverTelemetry(
        DriverTelemetryRequest request,
        global::Grpc.Core.ServerCallContext context)
        => BuildLatestDriverTelemetryResponseAsync(request, context.CancellationToken);

    public override Task<DriverLapTelemetryResponse> GetDriverLapTelemetry(
        DriverLapTelemetryRequest request,
        ServerCallContext context)
        => BuildDriverLapTelemetryResponseAsync(request, context.CancellationToken);

    private async Task<DriverTelemetryResponse> BuildLatestDriverTelemetryResponseAsync(
        DriverTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTimeOffset? sinceTimestamp = DateTimeOffset.TryParse(request.SinceTimestamp, out var parsedSinceTimestamp)
                ? parsedSinceTimestamp
                : null;
            var samples = await _analyticsQueryService.GetLatestDriverTelemetryAsync(
                request.SessionId,
                request.DriverNumber,
                request.MaxSamples,
                sinceTimestamp,
                request.RequestedMetrics,
                cancellationToken);

            var response = new DriverTelemetryResponse
            {
                SessionId = request.SessionId,
                DriverNumber = request.DriverNumber
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
