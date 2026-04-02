namespace F1.FeedReplay.Service.Infrastructure.Configuration;

public sealed class ReplayBootstrapOptions
{
    public const string SectionName = "ReplayBootstrap";

    public bool Enabled { get; set; }
    public bool DownloadOnStartup { get; set; }
    public bool AutoLoadOnStartup { get; set; }
    public bool AutoStartOnStartup { get; set; }
    public bool OverwriteExistingFiles { get; set; }
    public string? IndexUrl { get; set; }
    public string StorageRootPath { get; set; } = "./runtime-data";
    public string GeneratedReplayConfigurationFileName { get; set; } = "replay-config.generated.json";
    public string IndexFileName { get; set; } = "Index.json";
    public string? SessionId { get; set; }
    public double ReplaySpeed { get; set; } = 1.0d;
    public int StartOffsetMs { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 10;
    public int RetryDelaySeconds { get; set; } = 3;
}
