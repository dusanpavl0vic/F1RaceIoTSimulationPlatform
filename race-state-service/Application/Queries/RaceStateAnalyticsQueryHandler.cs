using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Infrastructure.Grpc;
using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;

namespace F1.RaceState.Service.Application.Queries;

public sealed class RaceStateAnalyticsQueryHandler(
    TelemetryAnalyticsGateway telemetryAnalyticsGateway,
    IRaceStateStore raceStateStore)
{
    private readonly TelemetryAnalyticsGateway _telemetryAnalyticsGateway = telemetryAnalyticsGateway;
    private readonly IRaceStateStore _raceStateStore = raceStateStore;

    public string? Handle(ResolveAnalyticsSessionIdQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.RequestedSessionId))
        {
            return query.RequestedSessionId;
        }

        return _raceStateStore.GetSnapshot().SessionId;
    }

    public async Task<RaceAnalyticsTyreStintStrategyViewModel> HandleAsync(
        GetTyreStintStrategyQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetTyreStintStrategyAsync(
            query.SessionId,
            cancellationToken);

        return new RaceAnalyticsTyreStintStrategyViewModel(
            response.SessionId,
            response.TotalLaps,
            response.Drivers.Select(MapTyreStintDriver).ToArray());
    }

    public async Task<RaceAnalyticsDriverLapTelemetryViewModel> HandleAsync(
        GetDriverLapTelemetryQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.GetDriverLapTelemetryAsync(
            query.SessionId,
            query.DriverNumber,
            query.LapNumber,
            query.Metrics,
            cancellationToken);

        return new RaceAnalyticsDriverLapTelemetryViewModel(
            query.SessionId,
            query.DriverNumber,
            query.LapNumber,
            response.ReturnedMetrics,
            response.Samples.Select(sample => MapTelemetrySample(sample, response.ReturnedMetrics)).ToArray());
    }

    private static RaceAnalyticsTelemetrySampleViewModel MapTelemetrySample(
        AnalyticsGrpc.LiveTelemetrySample sample,
        IReadOnlyCollection<string> metrics)
    {
        var requestedMetrics = new HashSet<string>(metrics, StringComparer.OrdinalIgnoreCase);

        return new RaceAnalyticsTelemetrySampleViewModel(
            sample.SessionId,
            sample.DriverNumber,
            sample.LapNumber,
            sample.StintNumber,
            sample.SampleIndex,
            sample.Timestamp)
        {
            Speed = requestedMetrics.Contains("speed") ? sample.Speed : null,
            Rpm = requestedMetrics.Contains("rpm") ? sample.Rpm : null,
            ThrottlePct = requestedMetrics.Contains("throttlePct") ? sample.ThrottlePct : null,
            RawThrottle = requestedMetrics.Contains("rawThrottle") ? sample.RawThrottle : null,
            BrakePct = requestedMetrics.Contains("brakePct") ? sample.BrakePct : null,
            RawBrake = requestedMetrics.Contains("rawBrake") ? sample.RawBrake : null,
            Gear = requestedMetrics.Contains("gear") ? sample.Gear : null,
            DrsEnabled = requestedMetrics.Contains("drsEnabled") ? sample.DrsEnabled : null
        };
    }

    private static RaceAnalyticsTyreStintDriverViewModel MapTyreStintDriver(
        AnalyticsGrpc.TyreStintDriver driver)
        => new(
            driver.DriverNumber,
            driver.DriverName,
            string.IsNullOrWhiteSpace(driver.TeamName) ? null : driver.TeamName,
            string.IsNullOrWhiteSpace(driver.TeamColor) ? null : driver.TeamColor,
            driver.GridPosition <= 0 ? null : driver.GridPosition,
            driver.Position <= 0 ? null : driver.Position,
            driver.Stints.Select(MapTyreStint).ToArray());

    private static RaceAnalyticsTyreStintViewModel MapTyreStint(
        AnalyticsGrpc.TyreStint stint)
    {
        var startLap = Math.Max(1, stint.StartLap);
        var lapCount = stint.LapCount > 0
            ? stint.LapCount
            : Math.Max(1, stint.EndLap - startLap + 1);
        var endLap = stint.EndLap > 0
            ? stint.EndLap
            : startLap + lapCount - 1;

        return new RaceAnalyticsTyreStintViewModel(
            stint.StintNumber,
            string.IsNullOrWhiteSpace(stint.Compound) ? null : stint.Compound,
            stint.TyreIsNew,
            startLap,
            endLap,
            lapCount);
    }
}
