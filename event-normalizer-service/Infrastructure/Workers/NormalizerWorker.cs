using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Services;
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

                    var canonicalEvents = _canonicalEventFactory.Create(consumedEvent.Event);
                    foreach (var canonicalEvent in canonicalEvents)
                    {
                        var topic = _canonicalTopicMapper.Map(canonicalEvent);
                        await _canonicalEventPublisher.PublishAsync(topic, canonicalEvent, stoppingToken);
                        await _eventCaptureWriter.WriteAsync(
                            "canonical-output-authoritative",
                            topic,
                            JsonSerializer.Serialize(canonicalEvent, SerializerOptions),
                            stoppingToken);
                        _statusStore.RecordPublished(topic, 1);
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
}
