using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Domain.Models;
using F1.FeedReplay.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F1.FeedReplay.Service.Infrastructure.Mqtt;

public sealed class MqttPublisher : IRawEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<MqttPublisher> _logger;
    private readonly MqttOptions _options;

    private TcpClient? _client;
    private NetworkStream? _stream;

    public MqttPublisher(IOptions<MqttOptions> options, ILogger<MqttPublisher> logger)
    {
        _logger = logger;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _options.ClientId = $"f1-feed-replay-service-{Environment.MachineName}".ToLowerInvariant();
        }
    }

    public async Task PublishAsync(string topic, RawReplayEvent rawReplayEvent, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(rawReplayEvent, SerializerOptions);
        var payloadText = _options.LogPayloads ? Encoding.UTF8.GetString(payload) : null;
        if (!_options.Enabled)
        {
            if (_options.LogPayloads)
            {
                _logger.LogInformation("MQTT disabled. Topic {Topic} payload: {Payload}", topic, payloadText);
            }
            else
            {
                _logger.LogInformation(
                    "MQTT disabled. Skipping publish to topic {Topic}. Payload size: {PayloadSize} bytes.",
                    topic,
                    payload.Length);
            }

            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
            var packet = BuildPublishPacket(topic, payload);
            await _stream!.WriteAsync(packet, cancellationToken);
            await _stream.FlushAsync(cancellationToken);

            if (_options.LogPayloads)
            {
                _logger.LogInformation("Published replay event to topic {Topic}. Payload: {Payload}", topic, payloadText);
            }
            else
            {
                _logger.LogInformation("Published replay event to topic {Topic}.", topic);
            }
        }
        catch
        {
            ResetConnection();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            ResetConnection();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client is { Connected: true } && _stream is not null)
        {
            return;
        }

        _client = new TcpClient();
        await _client.ConnectAsync(_options.Host, _options.Port, cancellationToken);
        _stream = _client.GetStream();

        var connectPacket = BuildConnectPacket();
        await _stream.WriteAsync(connectPacket, cancellationToken);
        await _stream.FlushAsync(cancellationToken);

        var connAck = new byte[4];
        await ReadExactAsync(_stream, connAck, cancellationToken);
        if (connAck[0] != 0x20 || connAck[1] != 0x02 || connAck[3] != 0x00)
        {
            throw new InvalidOperationException("MQTT broker rejected the connection.");
        }

        _logger.LogInformation("Connected to MQTT broker {Host}:{Port}.", _options.Host, _options.Port);
    }

    private byte[] BuildConnectPacket()
    {
        using var payloadStream = new MemoryStream();
        WriteMqttString(payloadStream, _options.ClientId);
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            WriteMqttString(payloadStream, _options.Username!);
        }

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            WriteMqttString(payloadStream, _options.Password!);
        }

        using var variableHeaderStream = new MemoryStream();
        WriteMqttString(variableHeaderStream, "MQTT");
        variableHeaderStream.WriteByte(0x04);

        var connectFlags = (byte)0x02;
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            connectFlags |= 0x80;
        }

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            connectFlags |= 0x40;
        }

        variableHeaderStream.WriteByte(connectFlags);
        Span<byte> keepAlive = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(keepAlive, (ushort)_options.KeepAliveSeconds);
        variableHeaderStream.Write(keepAlive);

        var remainingLength = (int)(variableHeaderStream.Length + payloadStream.Length);
        using var packetStream = new MemoryStream();
        packetStream.WriteByte(0x10);
        WriteRemainingLength(packetStream, remainingLength);
        variableHeaderStream.Position = 0;
        variableHeaderStream.CopyTo(packetStream);
        payloadStream.Position = 0;
        payloadStream.CopyTo(packetStream);
        return packetStream.ToArray();
    }

    private static byte[] BuildPublishPacket(string topic, byte[] payload)
    {
        using var variableHeaderStream = new MemoryStream();
        WriteMqttString(variableHeaderStream, topic);

        var remainingLength = (int)(variableHeaderStream.Length + payload.Length);
        using var packetStream = new MemoryStream();
        packetStream.WriteByte(0x30);
        WriteRemainingLength(packetStream, remainingLength);
        variableHeaderStream.Position = 0;
        variableHeaderStream.CopyTo(packetStream);
        packetStream.Write(payload);
        return packetStream.ToArray();
    }

    private static void WriteMqttString(Stream stream, string value)
    {
        var encoded = Encoding.UTF8.GetBytes(value);
        Span<byte> lengthBytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(lengthBytes, (ushort)encoded.Length);
        stream.Write(lengthBytes);
        stream.Write(encoded);
    }

    private static void WriteRemainingLength(Stream stream, int value)
    {
        do
        {
            var encodedByte = (byte)(value % 128);
            value /= 128;
            if (value > 0)
            {
                encodedByte |= 128;
            }

            stream.WriteByte(encodedByte);
        } while (value > 0);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
            if (bytesRead == 0)
            {
                throw new IOException("Unexpected EOF while reading from the MQTT broker.");
            }

            totalRead += bytesRead;
        }
    }

    private void ResetConnection()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }
}
