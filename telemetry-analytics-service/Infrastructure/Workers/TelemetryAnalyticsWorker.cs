using System.Text.Json;
using F1.Shared.Models;
using F1.Shared.Mqtt;
using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Services;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F1.TelemetryAnalytics.Service.Infrastructure.Workers;

public sealed class TelemetryAnalyticsWorker(
    AnalyticsStateStore analyticsStateStore,
    TelemetryStreamHub telemetryStreamHub,
    IAnalyticsRepository analyticsRepository,
    IInfluxTelemetryClient influxTelemetryClient,
    IOptions<MqttOptions> mqttOptions,
    ILogger<TelemetryAnalyticsWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
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
    private readonly MqttOptions _mqttOptions = mqttOptions.Value;
    private readonly ILogger<TelemetryAnalyticsWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _analyticsRepository.InitializeAsync(stoppingToken);

        if (!_mqttOptions.Enabled)
        {
            _logger.LogInformation("Telemetry analytics MQTT worker is disabled.");
            return;
        }

        await using var subscriber = new SimpleMqttClient();
        await subscriber.ConnectAsync(
            _mqttOptions.Host,
            _mqttOptions.Port,
            _mqttOptions.SubscriberClientId,
            null,
            null,
            _mqttOptions.KeepAliveSeconds,
            stoppingToken);

        await subscriber.SubscribeAsync(_mqttOptions.TopicFilter, stoppingToken);
        _logger.LogInformation("Telemetry analytics service subscribed to {TopicFilter}.", _mqttOptions.TopicFilter);

        var pingTask = RunPingLoopAsync(subscriber, stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await subscriber.ReadMessageAsync(stoppingToken);
                    if (message is null)
                    {
                        continue;
                    }

                    var canonicalEvent = JsonSerializer.Deserialize<CanonicalEvent>(message.Payload, SerializerOptions);
                    if (canonicalEvent is null || !RelevantEventTypes.Contains(canonicalEvent.EventType))
                    {
                        continue;
                    }

                    var outcome = _analyticsStateStore.Apply(canonicalEvent);
                    if (!outcome.Applied)
                    {
                        continue;
                    }

                    if (outcome.SessionChanged)
                    {
                        await _analyticsRepository.UpsertSessionAsync(_analyticsStateStore.Snapshot(), stoppingToken);
                    }

                    if (outcome.DriverChanged && outcome.Driver is not null)
                    {
                        await _analyticsRepository.UpsertDriverAsync(canonicalEvent.SessionId, outcome.Driver, stoppingToken);
                    }

                    if (outcome.CurrentStint is not null)
                    {
                        var stint = outcome.CurrentStint with { SessionId = canonicalEvent.SessionId };
                        await _analyticsRepository.UpsertStintSummaryAsync(stint, stoppingToken);
                    }

                    if (outcome.CompletedLap is not null)
                    {
                        await _analyticsRepository.UpsertLapSummaryAsync(outcome.CompletedLap, stoppingToken);
                    }

                    if (outcome.TelemetrySample is not null)
                    {
                        _telemetryStreamHub.Publish(outcome.TelemetrySample);
                        await _influxTelemetryClient.WriteTelemetrySampleAsync(outcome.TelemetrySample, stoppingToken);
                    }
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(exception, "Telemetry analytics ingestion failed while processing canonical event.");
                }
            }
        }
        finally
        {
            try
            {
                await pingTask;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }

    private static async Task RunPingLoopAsync(SimpleMqttClient subscriber, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            await subscriber.PingAsync(cancellationToken);
        }
    }
}
