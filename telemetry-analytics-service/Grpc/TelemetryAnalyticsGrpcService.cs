using F1.TelemetryAnalytics.Service.Application.Contracts;

namespace F1.TelemetryAnalytics.Service.Grpc;

public sealed class TelemetryAnalyticsGrpcService(IAnalyticsQueryService analyticsQueryService) : TelemetryAnalytics.TelemetryAnalyticsBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService = analyticsQueryService;

    public override async Task<DriverLapSummariesResponse> GetDriverLapSummaries(DriverLapSummariesRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var (driverName, laps) = await _analyticsQueryService.GetDriverLapSummariesAsync(request.SessionId, request.DriverNumber, context.CancellationToken);
        var response = new DriverLapSummariesResponse
        {
            SessionId = request.SessionId,
            DriverNumber = request.DriverNumber,
            DriverName = driverName
        };

        response.Laps.AddRange(laps.Select(lap => new DriverLapSummary
        {
            LapNumber = lap.LapNumber,
            Position = lap.Position ?? 0,
            LapTime = lap.LapTime ?? string.Empty,
            LapTimeMs = lap.LapTimeMs ?? 0,
            BestLapTime = lap.BestLapTime ?? string.Empty,
            BestLapTimeMs = lap.BestLapTimeMs ?? 0,
            Sector1 = lap.Sector1 ?? string.Empty,
            Sector1Ms = lap.Sector1Ms ?? 0,
            Sector2 = lap.Sector2 ?? string.Empty,
            Sector2Ms = lap.Sector2Ms ?? 0,
            Sector3 = lap.Sector3 ?? string.Empty,
            Sector3Ms = lap.Sector3Ms ?? 0,
            Compound = lap.Compound ?? string.Empty,
            TyreLaps = lap.TyreLaps ?? 0,
            StintNumber = lap.StintNumber,
            AvgSpeed = lap.AverageSpeed,
            MaxSpeed = lap.MaxSpeed,
            ThrottlePct = lap.ThrottlePct,
            BrakePct = lap.BrakePct,
            DrsPct = lap.DrsPct
        }));

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

        response.Buckets.AddRange(buckets.Select(bucket => new SegmentBucket
        {
            BucketIndex = bucket.BucketIndex,
            StartProgressPct = bucket.StartProgressPct,
            EndProgressPct = bucket.EndProgressPct,
            AvgSpeed = bucket.AverageSpeed,
            MaxSpeed = bucket.MaxSpeed,
            AvgThrottlePct = bucket.AverageThrottlePct,
            BrakeUsagePct = bucket.BrakeUsagePct,
            DrsUsagePct = bucket.DrsUsagePct,
            Behavior = bucket.Behavior
        }));

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
            Left = MapLapSeries(comparison.Left),
            Right = MapLapSeries(comparison.Right)
        };

        response.BucketCompare.AddRange(comparison.BucketCompare.Select(bucket => new SegmentComparisonBucket
        {
            BucketIndex = bucket.BucketIndex,
            StartProgressPct = bucket.StartProgressPct,
            EndProgressPct = bucket.EndProgressPct,
            LeftBehavior = bucket.LeftBehavior,
            RightBehavior = bucket.RightBehavior,
            AvgSpeedDelta = bucket.AverageSpeedDelta,
            ThrottleDelta = bucket.ThrottleDelta,
            BrakeDelta = bucket.BrakeDelta
        }));

        return response;
    }

    private static DriverLapSeries MapLapSeries(Application.Models.DriverLapSeriesDto series)
    {
        var output = new DriverLapSeries
        {
            DriverNumber = series.DriverNumber,
            DriverName = series.DriverName,
            LapNumber = series.LapNumber
        };

        output.Telemetry.AddRange(series.Telemetry.Select(point => new TelemetryPoint
        {
            ProgressPct = point.ProgressPct,
            Timestamp = point.Timestamp,
            Speed = point.Speed,
            ThrottlePct = point.ThrottlePct,
            BrakePct = point.BrakePct,
            Gear = point.Gear,
            DrsEnabled = point.DrsEnabled
        }));

        return output;
    }
}
