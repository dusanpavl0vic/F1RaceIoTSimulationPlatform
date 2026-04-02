namespace F1.FeedReplay.Service.Infrastructure.Configuration;

public sealed class ReplayServiceOptions
{
    public const string SectionName = "Replay";

    public string DefaultConfigurationPath { get; set; } = "./runtime-data/replay-config.generated.json";
}
