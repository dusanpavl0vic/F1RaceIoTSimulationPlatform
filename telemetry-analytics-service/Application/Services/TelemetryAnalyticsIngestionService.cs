using System.Text.Json;
using F1.Shared.Models;
using F1.TelemetryAnalytics.Service.Application.Contracts;

namespace F1.TelemetryAnalytics.Service.Application.Services;

public sealed class TelemetryAnalyticsIngestionService(
    AnalyticsStateStore analyticsStateStore,
    TelemetryStreamHub telemetryStreamHub,
    IAnalyticsRepository analyticsRepository,
    IInfluxTelemetryClient influxTelemetryClient)
{
    private static readonly HashSet<string> RelevantEventTypes = new(StringComparer.Ordinal)
    {
        "session.info.updated",
        "session.status.updated",
        "track.status.updated",
        "lap.count.updated",
        "driver.list.updated",
        "timing.driver.updated",
        "timing.stats.updated",
        "timing.app.updated",
        "lap.series.updated",
        "tyres.current.updated",
        "tyres.stint.updated",
        "car.telemetry.updated"
    };

    private readonly AnalyticsStateStore _analyticsStateStore = analyticsStateStore;
    private readonly TelemetryStreamHub _telemetryStreamHub = telemetryStreamHub;
    private readonly IAnalyticsRepository _analyticsRepository = analyticsRepository;
    private readonly IInfluxTelemetryClient _influxTelemetryClient = influxTelemetryClient;

    public async Task HandleAsync(CanonicalEvent canonicalEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(canonicalEvent);

        if (!RelevantEventTypes.Contains(canonicalEvent.EventType))
        {
            return;
        }

        var outcome = _analyticsStateStore.Apply(canonicalEvent);
        if (!outcome.Applied)
        {
            return;
        }

        if (outcome.SessionChanged)
        {
            await _analyticsRepository.UpsertSessionAsync(_analyticsStateStore.Snapshot(), cancellationToken);
        }

        if (outcome.DriverChanged && outcome.Driver is not null)
        {
            await _analyticsRepository.UpsertDriverAsync(canonicalEvent.SessionId, outcome.Driver, cancellationToken);
        }

        if (outcome.CurrentStint is not null)
        {
            var stint = outcome.CurrentStint with { SessionId = canonicalEvent.SessionId };
            await _analyticsRepository.UpsertStintSummaryAsync(stint, cancellationToken);
        }

        if (outcome.CompletedLap is not null)
        {
            await _analyticsRepository.UpsertLapSummaryAsync(outcome.CompletedLap, cancellationToken);
        }

        if (outcome.TelemetrySample is not null)
        {
            _telemetryStreamHub.Publish(outcome.TelemetrySample);
            await _influxTelemetryClient.WriteTelemetrySampleAsync(outcome.TelemetrySample, cancellationToken);
        }
    }
}
