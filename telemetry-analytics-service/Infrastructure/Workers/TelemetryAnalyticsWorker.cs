using System.Text.Json;
using F1.Shared.Models;
using F1.Shared.Mqtt;
using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Services;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F1.TelemetryAnalytics.Service.Infrastructure.Workers;

public sealed class TelemetryAnalyticsWorker(
    IAnalyticsRepository analyticsRepository,
    TelemetryAnalyticsIngestionService ingestionService,
    IOptions<MqttOptions> mqttOptions,
    ILogger<TelemetryAnalyticsWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAnalyticsRepository _analyticsRepository = analyticsRepository;
    private readonly TelemetryAnalyticsIngestionService _ingestionService = ingestionService;
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
                    if (canonicalEvent is null)
                    {
                        continue;
                    }

                    await _ingestionService.HandleAsync(canonicalEvent, stoppingToken);
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
