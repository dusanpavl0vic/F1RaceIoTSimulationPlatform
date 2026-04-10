namespace F1.FeedReplay.Service.Infrastructure.Configuration;

public sealed class ReplayServiceOptions
{
    public const string SectionName = "Replay";

    public string DefaultConfigurationPath { get; set; } = "./runtime-data/replay-config.generated.json";

    public bool StartFromSessionStatusStarted { get; set; } = true;

    public int SessionStartLeadInSeconds { get; set; } = 60;

    public string[] ExcludedFeeds { get; set; } =
    [
        "WeatherData",
        "ChampionshipPrediction",
        "ContentStreams",
        "ExtrapolatedClock",
        "Heartbeat",
        "TeamRadio",
        "WeatherDataSeries"
    ];
}
