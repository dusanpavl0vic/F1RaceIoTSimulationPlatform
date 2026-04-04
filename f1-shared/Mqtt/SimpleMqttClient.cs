using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace F1.Shared.Mqtt;

public sealed class SimpleMqttClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _nextPacketIdentifier = 1;

    public async Task ConnectAsync(
        string host,
        int port,
        string clientId,
        string? username,
        string? password,
        int keepAliveSeconds,
        CancellationToken cancellationToken)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken);
        _stream = _client.GetStream();

        var connectPacket = BuildConnectPacket(clientId, username, password, keepAliveSeconds);
        await _stream.WriteAsync(connectPacket, cancellationToken);
        await _stream.FlushAsync(cancellationToken);

        var connAck = new byte[4];
        await ReadExactAsync(_stream, connAck, cancellationToken);
        if (connAck[0] != 0x20 || connAck[1] != 0x02 || connAck[3] != 0x00)
        {
            throw new InvalidOperationException("MQTT broker rejected the connection.");
        }
    }

    public async Task SubscribeAsync(string topicFilter, CancellationToken cancellationToken)
    {
        var stream = EnsureStream();
        var packetIdentifier = GetNextPacketIdentifier();
        var subscribePacket = BuildSubscribePacket(packetIdentifier, topicFilter);

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await stream.WriteAsync(subscribePacket, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            await ReadSubAckAsync(stream, packetIdentifier, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task PublishAsync(string topic, byte[] payload, CancellationToken cancellationToken)
    {
        var stream = EnsureStream();
        var publishPacket = BuildPublishPacket(topic, payload);

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await stream.WriteAsync(publishPacket, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<MqttMessage?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        var stream = EnsureStream();

        while (true)
        {
            var packetTypeAndFlags = await ReadByteAsync(stream, cancellationToken);
            if (packetTypeAndFlags < 0)
            {
                return null;
            }

            var remainingLength = await ReadRemainingLengthAsync(stream, cancellationToken);
            var packet = new byte[remainingLength];
            await ReadExactAsync(stream, packet, cancellationToken);

            var packetType = packetTypeAndFlags >> 4;
            if (packetType == 13)
            {
                continue;
            }

            if (packetType != 3)
            {
                continue;
            }

            var topicLength = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(0, 2));
            var topic = Encoding.UTF8.GetString(packet, 2, topicLength);
            var payloadOffset = 2 + topicLength;

            if ((packetTypeAndFlags & 0b0000_0110) != 0)
            {
                payloadOffset += 2;
            }

            var payload = packet[payloadOffset..];
            return new MqttMessage(topic, payload);
        }
    }

    public async Task PingAsync(CancellationToken cancellationToken)
    {
        var stream = EnsureStream();
        var pingPacket = new byte[] { 0xC0, 0x00 };

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await stream.WriteAsync(pingPacket, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _writeGate.Dispose();
        return ValueTask.CompletedTask;
    }

    private NetworkStream EnsureStream()
        => _stream ?? throw new InvalidOperationException("MQTT client is not connected.");

    private int GetNextPacketIdentifier()
    {
        var next = Interlocked.Increment(ref _nextPacketIdentifier);
        return next > ushort.MaxValue ? 1 : next;
    }

    private static byte[] BuildConnectPacket(string clientId, string? username, string? password, int keepAliveSeconds)
    {
        using var payloadStream = new MemoryStream();
        WriteMqttString(payloadStream, clientId);
        if (!string.IsNullOrWhiteSpace(username))
        {
            WriteMqttString(payloadStream, username);
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            WriteMqttString(payloadStream, password);
        }

        using var variableHeaderStream = new MemoryStream();
        WriteMqttString(variableHeaderStream, "MQTT");
        variableHeaderStream.WriteByte(0x04);

        var connectFlags = (byte)0x02;
        if (!string.IsNullOrWhiteSpace(username))
        {
            connectFlags |= 0x80;
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            connectFlags |= 0x40;
        }

        variableHeaderStream.WriteByte(connectFlags);
        Span<byte> keepAlive = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(keepAlive, (ushort)keepAliveSeconds);
        variableHeaderStream.Write(keepAlive);

        return BuildPacket(0x10, variableHeaderStream.ToArray(), payloadStream.ToArray());
    }

    private static byte[] BuildSubscribePacket(int packetIdentifier, string topicFilter)
    {
        using var variableHeaderStream = new MemoryStream();
        Span<byte> packetIdentifierBytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(packetIdentifierBytes, (ushort)packetIdentifier);
        variableHeaderStream.Write(packetIdentifierBytes);

        using var payloadStream = new MemoryStream();
        WriteMqttString(payloadStream, topicFilter);
        payloadStream.WriteByte(0x00);

        return BuildPacket(0x82, variableHeaderStream.ToArray(), payloadStream.ToArray());
    }

    private static byte[] BuildPublishPacket(string topic, byte[] payload)
    {
        using var variableHeaderStream = new MemoryStream();
        WriteMqttString(variableHeaderStream, topic);
        return BuildPacket(0x30, variableHeaderStream.ToArray(), payload);
    }

    private static byte[] BuildPacket(byte fixedHeader, byte[] variableHeader, byte[] payload)
    {
        using var packetStream = new MemoryStream();
        packetStream.WriteByte(fixedHeader);
        WriteRemainingLength(packetStream, variableHeader.Length + payload.Length);
        packetStream.Write(variableHeader);
        packetStream.Write(payload);
        return packetStream.ToArray();
    }

    private static async Task ReadSubAckAsync(NetworkStream stream, int packetIdentifier, CancellationToken cancellationToken)
    {
        while (true)
        {
            var packetTypeAndFlags = await ReadByteAsync(stream, cancellationToken);
            if (packetTypeAndFlags < 0)
            {
                throw new IOException("Unexpected EOF while reading SUBACK from the MQTT broker.");
            }

            var remainingLength = await ReadRemainingLengthAsync(stream, cancellationToken);
            var packet = new byte[remainingLength];
            await ReadExactAsync(stream, packet, cancellationToken);

            if ((packetTypeAndFlags >> 4) == 9)
            {
                var receivedPacketIdentifier = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(0, 2));
                if (receivedPacketIdentifier != packetIdentifier)
                {
                    throw new InvalidOperationException("MQTT broker returned an unexpected SUBACK packet identifier.");
                }

                return;
            }
        }
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
                encodedByte |= 0x80;
            }

            stream.WriteByte(encodedByte);
        } while (value > 0);
    }

    private static async Task<int> ReadByteAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
        return bytesRead == 0 ? -1 : buffer[0];
    }

    private static async Task<int> ReadRemainingLengthAsync(Stream stream, CancellationToken cancellationToken)
    {
        var multiplier = 1;
        var value = 0;

        while (true)
        {
            var encodedByte = await ReadByteAsync(stream, cancellationToken);
            if (encodedByte < 0)
            {
                throw new IOException("Unexpected EOF while reading MQTT packet length.");
            }

            value += (encodedByte & 127) * multiplier;
            if ((encodedByte & 128) == 0)
            {
                return value;
            }

            multiplier *= 128;
            if (multiplier > 128 * 128 * 128)
            {
                throw new InvalidOperationException("MQTT packet uses an invalid remaining length.");
            }
        }
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
}
