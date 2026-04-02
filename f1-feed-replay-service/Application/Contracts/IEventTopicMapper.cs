using F1.FeedReplay.Service.Domain.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IEventTopicMapper
{
    string MapTopic(ReplayEvent replayEvent);
}
