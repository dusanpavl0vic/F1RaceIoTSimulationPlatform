using System.Text.Json;
using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Models;
using F1.EventNormalizer.Service.Infrastructure.Configuration;
using F1.Shared.Models;
using F1.Shared.Mqtt;
using Microsoft.Extensions.Options;

namespace F1.EventNormalizer.Service.Infrastructure.Mqtt;

public sealed class RawReplayEventSubscriber : IRawReplayEventSubscriber
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly SimpleMqttClient _client = new();
    private readonly ILogger<RawReplayEventSubscriber> _logger;
    private readonly MqttOptions _options;

    public RawReplayEventSubscriber(IOptions<MqttOptions> options, ILogger<RawReplayEventSubscriber> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await _client.ConnectAsync(
            _options.Host,
            _options.Port,
            _options.SubscriberClientId,
            null,
            null,
            _options.KeepAliveSeconds,
            cancellationToken);

        _logger.LogInformation(
            "Normalizer subscriber connected to MQTT broker {Host}:{Port} with topic filter {TopicFilter}.",
            _options.Host,
            _options.Port,
            _options.InputTopicFilter);
    }

    public Task SubscribeAsync(CancellationToken cancellationToken)
        => _client.SubscribeAsync(_options.InputTopicFilter, cancellationToken);

    public async Task<ConsumedRawEvent?> ReadAsync(CancellationToken cancellationToken)
    {
        var message = await _client.ReadMessageAsync(cancellationToken);
        if (message is null)
        {
            return null;
        }

        var rawEvent = JsonSerializer.Deserialize<RawReplayEvent>(message.Payload, SerializerOptions);
        if (rawEvent is null)
        {
            _logger.LogWarning("Received MQTT message on topic {Topic}, but payload could not be deserialized to RawReplayEvent.", message.Topic);
            return null;
        }

        return new ConsumedRawEvent(message.Topic, rawEvent);
    }

    public Task PingAsync(CancellationToken cancellationToken)
        => _client.PingAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _client.DisposeAsync();
}
