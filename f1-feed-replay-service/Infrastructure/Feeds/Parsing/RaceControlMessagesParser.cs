namespace F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;

public sealed class RaceControlMessagesParser(FeedFileReader fileReader, ILogger<RaceControlMessagesParser> logger)
    : GenericFeedParser(fileReader, logger)
{
    public override bool CanHandle(string feedName) => string.Equals(feedName, "RaceControlMessages", StringComparison.OrdinalIgnoreCase);
}
