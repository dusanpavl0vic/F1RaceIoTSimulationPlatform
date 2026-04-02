namespace F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;

public sealed class SessionInfoParser(FeedFileReader fileReader, ILogger<SessionInfoParser> logger)
    : GenericFeedParser(fileReader, logger)
{
    public override bool CanHandle(string feedName) => string.Equals(feedName, "SessionInfo", StringComparison.OrdinalIgnoreCase);
}
