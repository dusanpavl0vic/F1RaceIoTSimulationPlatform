namespace F1.TelemetryAnalytics.Service.Infrastructure.Configuration;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string TopicFilter { get; set; } = "f1/canonical/#";
    public string SubscriberClientId { get; set; } = "f1-telemetry-analytics-service";
    public int KeepAliveSeconds { get; set; } = 30;
}
