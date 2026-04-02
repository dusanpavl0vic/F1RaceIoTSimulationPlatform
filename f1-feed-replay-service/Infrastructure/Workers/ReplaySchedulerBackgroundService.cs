using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Models;
using F1.FeedReplay.Service.Domain.Services;

namespace F1.FeedReplay.Service.Infrastructure.Workers;

public sealed class ReplaySchedulerBackgroundService(
    IReplayExecutionQueue executionQueue,
    IEventTopicMapper topicMapper,
    IRawEventPublisher rawEventPublisher,
    IReplayTimeProvider timeProvider,
    ILogger<ReplaySchedulerBackgroundService> logger) : BackgroundService
{
    private readonly IReplayExecutionQueue _executionQueue = executionQueue;
    private readonly IEventTopicMapper _topicMapper = topicMapper;
    private readonly IRawEventPublisher _rawEventPublisher = rawEventPublisher;
    private readonly IReplayTimeProvider _timeProvider = timeProvider;
    private readonly ILogger<ReplaySchedulerBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var request = await _executionQueue.DequeueAsync(stoppingToken);
            await ExecuteReplayAsync(request, stoppingToken);
        }
    }

    private async Task ExecuteReplayAsync(ReplayExecutionRequest request, CancellationToken stoppingToken)
    {
        using var linkedCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, request.RunCancellationToken);

        try
        {
            foreach (var replayEvent in request.Session.Events)
            {
                await request.Clock.WaitUntilAsync(replayEvent.OffsetFromStart, linkedCancellationTokenSource.Token);

                var rawReplayEvent = replayEvent.ToRawReplayEvent(_timeProvider.GetUtcNow());
                var topic = _topicMapper.MapTopic(replayEvent);
                await _rawEventPublisher.PublishAsync(topic, rawReplayEvent, linkedCancellationTokenSource.Token);
                await request.OnEventPublishedAsync(replayEvent);
            }

            await request.OnCompletedAsync();
        }
        catch (OperationCanceledException) when (request.RunCancellationToken.IsCancellationRequested || stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Replay scheduler canceled for session {SessionId}.", request.Session.SessionId);
        }
        catch (Exception exception)
        {
            await request.OnFaultedAsync(exception);
        }
    }
}
