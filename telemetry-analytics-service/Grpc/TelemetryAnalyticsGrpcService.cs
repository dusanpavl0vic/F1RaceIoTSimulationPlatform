using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Services;
using F1.TelemetryAnalytics.Service.Domain.Models;

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
        response.Sessions.AddRange(sessions.Select(MapSessionOverview));
        return response;
    }

    public override async Task<SessionOverviewResponse> GetSessionOverview(SessionOverviewRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var session = await _analyticsQueryService.GetSessionOverviewAsync(request.SessionId, context.CancellationToken);
        return new SessionOverviewResponse
        {
            Session = session is null ? null : MapSessionOverview(session)
        };
    }

    public override async Task<SessionDriversResponse> GetSessionDrivers(SessionDriversRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var drivers = await _analyticsQueryService.GetSessionDriversAsync(request.SessionId, context.CancellationToken);
        var response = new SessionDriversResponse
        {
            SessionId = request.SessionId
        };

        response.Drivers.AddRange(drivers.Select(driver => new DriverSessionOverview
        {
            DriverNumber = driver.DriverNumber,
            DriverName = driver.DriverName,
            TeamName = driver.TeamName ?? string.Empty,
            TeamColor = driver.TeamColor ?? string.Empty,
            GridPosition = driver.GridPosition ?? 0,
            Position = driver.Position ?? 0,
            CompletedLaps = driver.CompletedLaps ?? 0,
            BestLapTime = driver.BestLapTime ?? string.Empty,
            BestLapTimeMs = driver.BestLapTimeMs ?? 0,
            LastLapTime = driver.LastLapTime ?? string.Empty,
            LastLapTimeMs = driver.LastLapTimeMs ?? 0,
            CurrentCompound = driver.CurrentCompound ?? string.Empty,
            TyreLaps = driver.TyreLaps ?? 0,
            CurrentStintNumber = driver.CurrentStintNumber ?? 0,
            GapToLeader = driver.GapToLeader ?? string.Empty,
            IntervalToAhead = driver.IntervalToAhead ?? string.Empty
        }));

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

        response.Stints.AddRange(stints.Select(stint => new DriverStint
        {
            StintNumber = stint.StintNumber,
            Compound = stint.Compound ?? string.Empty,
            TyreIsNew = stint.TyreIsNew ?? false,
            StartLap = stint.StartLap ?? 0,
            EndLap = stint.EndLap ?? 0,
            LapCount = stint.LapCount ?? 0,
            UpdatedAt = stint.UpdatedAt.ToString("O")
        }));

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

    public override async Task<DriverTelemetryResponse> GetLatestDriverTelemetry(DriverTelemetryRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var response = new DriverTelemetryResponse
        {
            SessionId = request.SessionId,
            DriverNumber = request.DriverNumber
        };
        response.Samples.AddRange(_telemetryStreamHub
            .GetRecent(request.SessionId, request.DriverNumber, request.MaxSamples <= 0 ? 200 : request.MaxSamples)
            .Select(MapLiveTelemetrySample));

        return response;
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
            await responseStream.WriteAsync(MapLiveTelemetrySample(sample));
        }
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
            DrsEnabled = point.DrsEnabled,
            Rpm = point.Rpm,
            SampleIndex = point.SampleIndex,
            RawThrottle = point.RawThrottle,
            RawBrake = point.RawBrake
        }));

        return output;
    }

    private static SessionOverview MapSessionOverview(Application.Models.SessionOverviewDto session)
        => new()
        {
            SessionId = session.SessionId,
            MeetingName = session.MeetingName ?? string.Empty,
            SessionName = session.SessionName ?? string.Empty,
            SessionStatus = session.SessionStatus ?? string.Empty,
            TrackStatusCode = session.TrackStatusCode ?? string.Empty,
            TrackStatusLabel = session.TrackStatusLabel ?? string.Empty,
            CurrentLap = session.CurrentLap ?? 0,
            TotalLaps = session.TotalLaps ?? 0,
            DriverCount = session.DriverCount,
            CompletedLapCount = session.CompletedLapCount,
            UpdatedAt = session.UpdatedAt.ToString("O")
        };

    private static LiveTelemetrySample MapLiveTelemetrySample(TelemetrySampleRecord sample)
        => new()
        {
            SessionId = sample.SessionId,
            DriverNumber = sample.DriverNumber,
            LapNumber = sample.LapNumber,
            StintNumber = sample.StintNumber,
            SampleIndex = sample.SampleIndex,
            Timestamp = sample.Timestamp.ToString("O"),
            Speed = sample.Speed ?? 0,
            Rpm = sample.Rpm ?? 0,
            ThrottlePct = sample.ThrottlePct ?? 0,
            RawThrottle = sample.RawThrottle ?? 0,
            BrakePct = sample.RawBrake ?? (sample.BrakeApplied is true ? 100d : 0d),
            RawBrake = sample.RawBrake ?? 0,
            Gear = sample.Gear ?? 0,
            DrsEnabled = sample.DrsEnabled ?? false
        };
}
