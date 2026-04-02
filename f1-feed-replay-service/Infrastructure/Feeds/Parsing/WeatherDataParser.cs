namespace F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;

public sealed class WeatherDataParser(FeedFileReader fileReader, ILogger<WeatherDataParser> logger)
    : GenericFeedParser(fileReader, logger)
{
    public override bool CanHandle(string feedName) => string.Equals(feedName, "WeatherData", StringComparison.OrdinalIgnoreCase);
}
