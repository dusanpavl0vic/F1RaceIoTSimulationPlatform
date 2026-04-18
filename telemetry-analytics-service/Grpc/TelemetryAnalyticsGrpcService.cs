using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Services;
using F1.TelemetryAnalytics.Service.Domain.Models;
using F1.TelemetryAnalytics.Service.Grpc.Mappers;

namespace F1.TelemetryAnalytics.Service.Grpc;

public sealed class TelemetryAnalyticsGrpcService(
    IAnalyticsQueryService analyticsQueryService,
    TelemetryStreamHub telemetryStreamHub) : TelemetryAnalytics.TelemetryAnalyticsBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService = analyticsQueryService;
    private readonly TelemetryStreamHub _telemetryStreamHub = telemetryStreamHub;

    public override async Task<ListSessionsResponse> ListSessions(ListSessionsRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var sessions = await _analyticsQueryService.ListSessionsAsync(context.CancellationToken);
        var response = new ListSessionsResponse();
        response.Sessions.AddRange(sessions.Select(TelemetryAnalyticsGrpcMapper.MapSessionOverview));
        return response;
    }

    public override async Task<SessionOverviewResponse> GetSessionOverview(SessionOverviewRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var session = await _analyticsQueryService.GetSessionOverviewAsync(request.SessionId, context.CancellationToken);
        return new SessionOverviewResponse
        {
            Session = session is null ? null : TelemetryAnalyticsGrpcMapper.MapSessionOverview(session)
        };
    }

    public override async Task<SessionDriversResponse> GetSessionDrivers(SessionDriversRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var drivers = await _analyticsQueryService.GetSessionDriversAsync(request.SessionId, context.CancellationToken);
        var response = new SessionDriversResponse
        {
            SessionId = request.SessionId
        };

        response.Drivers.AddRange(drivers.Select(TelemetryAnalyticsGrpcMapper.MapDriverOverview));

        return response;
    }

    public override async Task<DriverStintsResponse> GetDriverStints(DriverStintsRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var (driverName, stints) = await _analyticsQueryService.GetDriverStintsAsync(request.SessionId, request.DriverNumber, context.CancellationToken);
        var response = new DriverStintsResponse
        {
            SessionId = request.SessionId,
            DriverNumber = request.DriverNumber,
            DriverName = driverName
        };

        response.Stints.AddRange(stints.Select(TelemetryAnalyticsGrpcMapper.MapDriverStint));

        return response;
    }

    public override async Task<DriverLapSummariesResponse> GetDriverLapSummaries(DriverLapSummariesRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var (driverName, laps) = await _analyticsQueryService.GetDriverLapSummariesAsync(request.SessionId, request.DriverNumber, context.CancellationToken);
        var response = new DriverLapSummariesResponse
        {
            SessionId = request.SessionId,
            DriverNumber = request.DriverNumber,
            DriverName = driverName
        };

        response.Laps.AddRange(laps.Select(TelemetryAnalyticsGrpcMapper.MapDriverLapSummary));

        return response;
    }

    public override async Task<DriverSegmentBucketsResponse> GetDriverSegmentBuckets(DriverSegmentBucketsRequest request, global::Grpc.Core.ServerCallContext context)
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

    public override async Task<CompareDriversOnLapResponse> CompareDriversOnLap(CompareDriversOnLapRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var comparison = await _analyticsQueryService.CompareDriversOnLapAsync(
            request.SessionId,
            request.LeftDriverNumber,
            request.RightDriverNumber,
            request.LapNumber,
            request.BucketCount,
            context.CancellationToken);

        var response = new CompareDriversOnLapResponse
        {
            SessionId = comparison.SessionId,
            LapNumber = comparison.LapNumber,
            Left = TelemetryAnalyticsGrpcMapper.MapLapSeries(comparison.Left),
            Right = TelemetryAnalyticsGrpcMapper.MapLapSeries(comparison.Right)
        };

        response.BucketCompare.AddRange(comparison.BucketCompare.Select(TelemetryAnalyticsGrpcMapper.MapSegmentComparisonBucket));

        return response;
    }

    public override Task<DriverTelemetryResponse> GetLatestDriverTelemetry(DriverTelemetryRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var response = new DriverTelemetryResponse
        {
            SessionId = request.SessionId,
            DriverNumber = request.DriverNumber
        };
        response.Samples.AddRange(_telemetryStreamHub
            .GetRecent(request.SessionId, request.DriverNumber, request.MaxSamples <= 0 ? 200 : request.MaxSamples)
            .Select(TelemetryAnalyticsGrpcMapper.MapLiveTelemetrySample));

        return Task.FromResult(response);
    }

    public override async Task StreamDriverTelemetry(
        DriverTelemetryStreamRequest request,
        global::Grpc.Core.IServerStreamWriter<LiveTelemetrySample> responseStream,
        global::Grpc.Core.ServerCallContext context)
    {
        await foreach (var sample in _telemetryStreamHub.Subscribe(
                           request.SessionId,
                           request.DriverNumber,
                           request.RecentSampleCount,
                           context.CancellationToken))
        {
            await responseStream.WriteAsync(TelemetryAnalyticsGrpcMapper.MapLiveTelemetrySample(sample));
        }
    }
}
