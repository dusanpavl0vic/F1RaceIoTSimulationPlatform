using F1.FeedReplay.Service.Domain.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IReplayConfigurationLoader
{
    Task<ReplayConfiguration> LoadAsync(string configurationPath, CancellationToken cancellationToken);
}
