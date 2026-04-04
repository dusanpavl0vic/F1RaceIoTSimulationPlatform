using F1.FeedReplay.Service.Domain.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IFeedParser
{
    Task<IReadOnlyList<ReplayEvent>> ParseAsync(
        ReplayConfiguration configuration,
        FeedDefinition feedDefinition,
        int feedOrder,
        CancellationToken cancellationToken);
}
