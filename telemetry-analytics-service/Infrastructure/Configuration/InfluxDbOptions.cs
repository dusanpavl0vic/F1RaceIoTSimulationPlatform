namespace F1.TelemetryAnalytics.Service.Infrastructure.Configuration;

public sealed class InfluxDbOptions
{
    public const string SectionName = "InfluxDb";

    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:8086";
    public string Organization { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string WritePrecision { get; set; } = "ns";
}
