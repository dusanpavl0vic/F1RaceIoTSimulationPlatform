using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;

namespace F1.TelemetryAnalytics.Service.Grpc.Mappers;

internal static class TelemetryAnalyticsGrpcMapper
{
    public static TyreStintStrategyResponse MapTyreStintStrategy(TyreStintStrategyDto strategy)
    => new()
    {
        SessionId = strategy.SessionId,
        TotalLaps = strategy.TotalLaps
    };

    public static TyreStintDriver MapTyreStintDriver(TyreStintDriverDto driver)
    {
        var result = new TyreStintDriver
        {
            DriverNumber = driver.DriverNumber,
            DriverName = driver.DriverName,
            TeamName = driver.TeamName ?? string.Empty,
            TeamColor = driver.TeamColor ?? string.Empty,
            GridPosition = driver.GridPosition ?? 0,
            Position = driver.Position ?? 0
        };

        result.Stints.AddRange(driver.Stints.Select(MapTyreStint));
        return result;
    }

    private static TyreStint MapTyreStint(TyreStintDto stint)
        => new()
        {
            StintNumber = stint.StintNumber,
            Compound = stint.Compound ?? string.Empty,
            TyreIsNew = stint.TyreIsNew ?? false,
            StartLap = stint.StartLap ?? 0,
            EndLap = stint.EndLap ?? 0,
            LapCount = stint.LapCount ?? 0
        };

    public static LiveTelemetrySample MapLiveTelemetrySample(TelemetrySampleDto sample)
        => new()
        {
            SessionId = sample.SessionId,
            DriverNumber = sample.DriverNumber,
            LapNumber = sample.LapNumber,
            StintNumber = sample.StintNumber,
            SampleIndex = sample.SampleIndex,
            Timestamp = sample.Timestamp,
            Speed = sample.Speed ?? 0,
            Rpm = sample.Rpm ?? 0,
            ThrottlePct = sample.ThrottlePct ?? 0,
            RawThrottle = sample.RawThrottle ?? 0,
            BrakePct = sample.BrakePct ?? 0d,
            RawBrake = sample.RawBrake ?? 0,
            Gear = sample.Gear ?? 0,
            DrsEnabled = sample.DrsEnabled ?? false
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
