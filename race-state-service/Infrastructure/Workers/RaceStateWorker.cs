using System.Text.Json;
using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Infrastructure.Configuration;
using F1.RaceState.Service.Infrastructure.Persistence;
using F1.Shared.Models;
using F1.Shared.Mqtt;
using Microsoft.Extensions.Options;

namespace F1.RaceState.Service.Infrastructure.Workers;

public sealed class RaceStateWorker(
    IRaceStateStore raceStateStore,
    StatePersistenceService persistenceService,
    IOptions<MqttOptions> mqttOptions,
    ILogger<RaceStateWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly StatePersistenceService _persistenceService = persistenceService;
    private readonly MqttOptions _mqttOptions = mqttOptions.Value;
    private readonly ILogger<RaceStateWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_mqttOptions.Enabled)
        {
            _logger.LogInformation("Race state MQTT worker is disabled.");
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
        _logger.LogInformation("Race state service subscribed to {TopicFilter}.", _mqttOptions.TopicFilter);

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

                    var applyResult = _raceStateStore.Apply(canonicalEvent);
                    if (!applyResult.Applied)
                    {
                        _logger.LogDebug(
                            "Canonical event {EventType} with eventTime {EventTime:o} and sequence {Sequence} was not applied. Reason: {Reason}",
                            canonicalEvent.EventType,
                            canonicalEvent.EventTime,
                            canonicalEvent.Sequence,
                            applyResult.Reason);
                        continue;
                    }

                    await _persistenceService.PersistAsync(_raceStateStore.GetCheckpoint(), stoppingToken);

                    _logger.LogInformation(
                        "Applied canonical event {EventType} for session {SessionId}. eventTime={EventTime:o}, sequence={Sequence}.",
                        canonicalEvent.EventType,
                        canonicalEvent.SessionId,
                        canonicalEvent.EventTime,
                        canonicalEvent.Sequence);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(exception, "Race state service failed while processing canonical event.");
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
