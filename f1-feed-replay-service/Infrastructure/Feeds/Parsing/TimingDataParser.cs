namespace F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;

public sealed class TimingDataParser(FeedFileReader fileReader, ILogger<TimingDataParser> logger)
    : GenericFeedParser(fileReader, logger)
{
    public override bool CanHandle(string feedName) => string.Equals(feedName, "TimingData", StringComparison.OrdinalIgnoreCase);
}
