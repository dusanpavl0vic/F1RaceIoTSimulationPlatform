using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;

namespace F1.TelemetryAnalytics.Service.Grpc.Mappers;

internal static class TelemetryAnalyticsGrpcMapper
{
    public static SessionOverview MapSessionOverview(SessionOverviewDto session)
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

    public static DriverSessionOverview MapDriverOverview(DriverSessionOverviewDto driver)
        => new()
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
        };

    public static DriverStint MapDriverStint(DriverStintDto stint)
        => new()
        {
            StintNumber = stint.StintNumber,
            Compound = stint.Compound ?? string.Empty,
            TyreIsNew = stint.TyreIsNew ?? false,
            StartLap = stint.StartLap ?? 0,
            EndLap = stint.EndLap ?? 0,
            LapCount = stint.LapCount ?? 0,
            UpdatedAt = stint.UpdatedAt.ToString("O")
        };

    public static DriverLapSummary MapDriverLapSummary(DriverLapSummaryDto lap)
        => new()
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
        };

    public static SegmentBucket MapSegmentBucket(SegmentBucketDto bucket)
        => new()
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
        };

    public static SegmentComparisonBucket MapSegmentComparisonBucket(SegmentComparisonBucketDto bucket)
        => new()
        {
            BucketIndex = bucket.BucketIndex,
            StartProgressPct = bucket.StartProgressPct,
            EndProgressPct = bucket.EndProgressPct,
            LeftBehavior = bucket.LeftBehavior,
            RightBehavior = bucket.RightBehavior,
            AvgSpeedDelta = bucket.AverageSpeedDelta,
            ThrottleDelta = bucket.ThrottleDelta,
            BrakeDelta = bucket.BrakeDelta
        };

    public static DriverLapSeries MapLapSeries(DriverLapSeriesDto series)
    {
        var output = new DriverLapSeries
        {
            DriverNumber = series.DriverNumber,
            DriverName = series.DriverName,
            LapNumber = series.LapNumber
        };

        output.Telemetry.AddRange(series.Telemetry.Select(MapTelemetryPoint));
        return output;
    }

    public static TelemetryPoint MapTelemetryPoint(TelemetryPointDto point)
        => new()
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
        };

    public static LiveTelemetrySample MapLiveTelemetrySample(TelemetrySampleRecord sample)
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
