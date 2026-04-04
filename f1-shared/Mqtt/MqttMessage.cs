namespace F1.Shared.Mqtt;

public sealed record MqttMessage(
    string Topic,
    byte[] Payload);
