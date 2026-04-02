namespace F1.FeedReplay.Service.Domain.Services;

public interface IReplayTimeProvider
{
    DateTimeOffset GetUtcNow();
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
