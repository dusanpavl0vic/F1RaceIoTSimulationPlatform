using System.Text;
using System.Text.Json;
using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Infrastructure.Configuration;
using F1.Shared.Models;
using F1.Shared.Mqtt;
using Microsoft.Extensions.Options;

namespace F1.EventNormalizer.Service.Infrastructure.Mqtt;

public sealed class CanonicalEventPublisher : ICanonicalEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly SimpleMqttClient _client = new();
    private readonly ILogger<CanonicalEventPublisher> _logger;
    private readonly MqttOptions _options;

    public CanonicalEventPublisher(IOptions<MqttOptions> options, ILogger<CanonicalEventPublisher> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await _client.ConnectAsync(
            _options.Host,
            _options.Port,
            _options.PublisherClientId,
            null,
            null,
            _options.KeepAliveSeconds,
            cancellationToken);

        _logger.LogInformation(
            "Normalizer publisher connected to MQTT broker {Host}:{Port}.",
            _options.Host,
            _options.Port);
    }

    public async Task PublishAsync(string topic, CanonicalEvent canonicalEvent, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(canonicalEvent, SerializerOptions);
        await _client.PublishAsync(topic, payload, cancellationToken);

        if (_options.LogPayloads)
        {
            _logger.LogInformation("Published canonical event to {Topic}. Payload: {Payload}", topic, Encoding.UTF8.GetString(payload));
        }
        else
        {
            _logger.LogInformation("Published canonical event to {Topic}.", topic);
        }
    }

    public Task PingAsync(CancellationToken cancellationToken)
        => _client.PingAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _client.DisposeAsync();
}
