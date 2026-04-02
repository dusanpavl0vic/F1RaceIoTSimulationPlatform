using F1.FeedReplay.Service.Domain.Services;

namespace F1.FeedReplay.Service.Infrastructure.Time;

public sealed class SystemReplayTimeProvider : IReplayTimeProvider
{
    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        => delay <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(delay, cancellationToken);
}
