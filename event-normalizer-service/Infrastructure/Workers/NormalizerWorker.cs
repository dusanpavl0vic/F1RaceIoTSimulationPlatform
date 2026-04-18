using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Models;
using F1.EventNormalizer.Service.Application.Services;

namespace F1.EventNormalizer.Service.Infrastructure.Workers;

public sealed class NormalizerWorker(
    IRawReplayEventSubscriber rawReplayEventSubscriber,
    ICanonicalEventPublisher canonicalEventPublisher,
    NormalizerProcessingService processingService,
    NormalizerStatusStore statusStore,
    ILogger<NormalizerWorker> logger) : BackgroundService
{
    private readonly IRawReplayEventSubscriber _rawReplayEventSubscriber = rawReplayEventSubscriber;
    private readonly ICanonicalEventPublisher _canonicalEventPublisher = canonicalEventPublisher;
    private readonly NormalizerProcessingService _processingService = processingService;
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

                    await _processingService.ProcessAsync(consumedEvent, stoppingToken);
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
