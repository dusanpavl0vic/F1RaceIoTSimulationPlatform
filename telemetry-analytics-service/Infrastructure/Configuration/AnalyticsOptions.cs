namespace F1.TelemetryAnalytics.Service.Infrastructure.Configuration;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public bool StoreRawPayloads { get; set; } = true;
    public int LapCompareBucketCount { get; set; } = 20;
    public int LiveTelemetryBufferSize { get; set; } = 1000;
}
