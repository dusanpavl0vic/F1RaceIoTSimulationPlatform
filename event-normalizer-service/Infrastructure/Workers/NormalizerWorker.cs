using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Models;
using F1.EventNormalizer.Service.Application.Services;

namespace F1.EventNormalizer.Service.Infrastructure.Workers;

public sealed class NormalizerWorker(
    ICanonicalEventFactory canonicalEventFactory,
    ICanonicalTopicMapper canonicalTopicMapper,
    IRawReplayEventSubscriber rawReplayEventSubscriber,
    ICanonicalEventPublisher canonicalEventPublisher,
    NormalizerStatusStore statusStore,
    ILogger<NormalizerWorker> logger) : BackgroundService
{
    private readonly ICanonicalEventFactory _canonicalEventFactory = canonicalEventFactory;
    private readonly ICanonicalTopicMapper _canonicalTopicMapper = canonicalTopicMapper;
    private readonly IRawReplayEventSubscriber _rawReplayEventSubscriber = rawReplayEventSubscriber;
    private readonly ICanonicalEventPublisher _canonicalEventPublisher = canonicalEventPublisher;
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
                    await PublishCanonicalEventsAsync(consumedEvent, stoppingToken);
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
            _statusStore.RecordPublished(topic, 1);
        }
    }
}
