namespace F1.EventNormalizer.Service.Infrastructure.Configuration;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string InputTopicFilter { get; set; } = "f1/raw/#";
    public string OutputTopicPrefix { get; set; } = "f1/canonical";
    public string SubscriberClientId { get; set; } = "f1-event-normalizer-sub";
    public string PublisherClientId { get; set; } = "f1-event-normalizer-pub";
    public int KeepAliveSeconds { get; set; } = 30;
    public bool LogPayloads { get; set; } = true;
}
