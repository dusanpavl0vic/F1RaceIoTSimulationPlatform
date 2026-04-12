namespace F1.FeedReplay.Service.Infrastructure.Configuration;

public sealed class ReplayServiceOptions
{
    public static readonly string[] DefaultInitialWindowFeedNames =
    [
        "SessionInfo",
        "SessionStatus",
        "TrackStatus",
        "LapCount",
        "DriverList",
        "TimingData",
        "TimingStats",
        "TimingAppData",
        "LapSeries",
        "CurrentTyres",
        "TyreStintSeries",
        "RaceControlMessages"
    ];

    public const string SectionName = "Replay";

    public string DefaultConfigurationPath { get; set; } = "./runtime-data/replay-config.generated.json";

    public bool StartFromSessionStatusStarted { get; set; } = true;

    public int SessionStartLeadInSeconds { get; set; } = 60;

    public int InitialFeedWindowSeconds { get; set; } = 120;

    public string[] InitialWindowFeedNames { get; set; } = DefaultInitialWindowFeedNames;

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
