using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Models;
using F1.EventNormalizer.Service.Application.Services;
using F1.EventNormalizer.Service.Infrastructure.Configuration;
using System.Text.Json;

namespace F1.EventNormalizer.Service.Infrastructure.Workers;

public sealed class NormalizerWorker(
    ICanonicalEventFactory canonicalEventFactory,
    ICanonicalTopicMapper canonicalTopicMapper,
    IRawReplayEventSubscriber rawReplayEventSubscriber,
    ICanonicalEventPublisher canonicalEventPublisher,
    IEventCaptureWriter eventCaptureWriter,
    NormalizerStatusStore statusStore,
    ILogger<NormalizerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ICanonicalEventFactory _canonicalEventFactory = canonicalEventFactory;
    private readonly ICanonicalTopicMapper _canonicalTopicMapper = canonicalTopicMapper;
    private readonly IRawReplayEventSubscriber _rawReplayEventSubscriber = rawReplayEventSubscriber;
    private readonly ICanonicalEventPublisher _canonicalEventPublisher = canonicalEventPublisher;
    private readonly IEventCaptureWriter _eventCaptureWriter = eventCaptureWriter;
    private readonly NormalizerStatusStore _statusStore = statusStore;
    private readonly ILogger<NormalizerWorker> _logger = logger;

    private bool _publishingActivated;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _rawReplayEventSubscriber.ConnectAsync(stoppingToken);
        await _canonicalEventPublisher.ConnectAsync(stoppingToken);
        await _rawReplayEventSubscriber.SubscribeAsync(stoppingToken);

        _logger.LogInformation("Event normalizer worker started.");

        var pingTask = RunPingLoopAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumedEvent = await _rawReplayEventSubscriber.ReadAsync(stoppingToken);
                    if (consumedEvent is null)
                    {
                        continue;
                    }

                    _statusStore.RecordConsumed(consumedEvent.Topic);
                    await _eventCaptureWriter.WriteAsync(
                        "raw-input-authoritative",
                        consumedEvent.Topic,
                        JsonSerializer.Serialize(consumedEvent.Event, SerializerOptions),
                        stoppingToken);

                    if (_publishingActivated)
                    {
                        await PublishCanonicalEventsAsync(consumedEvent, stoppingToken);
                        continue;
                    }

                    if (IsSessionStarted(consumedEvent))
                    {
                        _publishingActivated = true;
                        _logger.LogInformation(
                            "Canonical publishing activated for session {SessionId} at SessionStatus Started. Event time: {SessionStart}.",
                            consumedEvent.Event.SessionId,
                            consumedEvent.Event.EventTime);

                        await PublishCanonicalEventsAsync(consumedEvent, stoppingToken);
                    }
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    _statusStore.RecordError(exception.Message);
                    _logger.LogError(exception, "Event normalizer failed while processing MQTT message.");
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _rawReplayEventSubscriber.DisposeAsync();
        await _canonicalEventPublisher.DisposeAsync();
    }

    private async Task RunPingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            await _rawReplayEventSubscriber.PingAsync(cancellationToken);
            await _canonicalEventPublisher.PingAsync(cancellationToken);
        }
    }

    private async Task PublishCanonicalEventsAsync(ConsumedRawEvent consumedEvent, CancellationToken cancellationToken)
    {
        var canonicalEvents = _canonicalEventFactory.Create(consumedEvent.Event);
        foreach (var canonicalEvent in canonicalEvents)
        {
            var topic = _canonicalTopicMapper.Map(canonicalEvent);
            await _canonicalEventPublisher.PublishAsync(topic, canonicalEvent, cancellationToken);
            await _eventCaptureWriter.WriteAsync(
                "canonical-output-authoritative",
                topic,
                JsonSerializer.Serialize(canonicalEvent, SerializerOptions),
                cancellationToken);
            _statusStore.RecordPublished(topic, 1);
        }
    }

    private static bool IsSessionStarted(ConsumedRawEvent consumedEvent)
        => string.Equals(consumedEvent.Event.SourceFeed, "SessionStatus", StringComparison.Ordinal)
           && string.Equals(consumedEvent.Event.Payload["rawData"]?["Status"]?.ToString(), "Started", StringComparison.OrdinalIgnoreCase);
}
