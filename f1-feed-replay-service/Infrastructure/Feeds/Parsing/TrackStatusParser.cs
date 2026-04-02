namespace F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;

public sealed class TrackStatusParser(FeedFileReader fileReader, ILogger<TrackStatusParser> logger)
    : GenericFeedParser(fileReader, logger)
{
    public override bool CanHandle(string feedName) => string.Equals(feedName, "TrackStatus", StringComparison.OrdinalIgnoreCase);
}
