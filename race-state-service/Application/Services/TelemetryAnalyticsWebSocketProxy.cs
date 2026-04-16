using System.Net.WebSockets;
using System.Text.Json;
using F1.RaceState.Service.Infrastructure.Grpc;

namespace F1.RaceState.Service.Application.Services;

public sealed class TelemetryAnalyticsWebSocketProxy(
    TelemetryAnalyticsGateway telemetryAnalyticsGateway,
    ILogger<TelemetryAnalyticsWebSocketProxy> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TelemetryAnalyticsGateway _telemetryAnalyticsGateway = telemetryAnalyticsGateway;
    private readonly ILogger<TelemetryAnalyticsWebSocketProxy> _logger = logger;

    public async Task ProxyAsync(
        WebSocket socket,
        string sessionId,
        int driverNumber,
        int recentSampleCount,
        CancellationToken cancellationToken)
    {
        var readyPayload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "telemetry.stream.ready",
            sessionId,
            driverNumber,
            recentSampleCount
        }, SerializerOptions);

        await socket.SendAsync(readyPayload, WebSocketMessageType.Text, true, cancellationToken);

        try
        {
            await foreach (var sample in _telemetryAnalyticsGateway.StreamDriverTelemetryAsync(sessionId, driverNumber, recentSampleCount, cancellationToken))
            {
                if (socket.State != WebSocketState.Open)
                {
                    break;
                }

                var payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    type = "telemetry.sample",
                    data = new
                    {
                        sample.SessionId,
                        sample.DriverNumber,
                        sample.LapNumber,
                        sample.StintNumber,
                        sample.SampleIndex,
                        sample.Timestamp,
                        sample.Speed,
                        sample.Rpm,
                        sample.ThrottlePct,
                        sample.RawThrottle,
                        sample.BrakePct,
                        sample.RawBrake,
                        sample.Gear,
                        sample.DrsEnabled
                    }
                }, SerializerOptions);

                await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (WebSocketException) when (socket.State is WebSocketState.Aborted or WebSocketState.Closed)
        {
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Telemetry analytics WS proxy failed for session {SessionId} and driver {DriverNumber}.", sessionId, driverNumber);
            if (socket.State == WebSocketState.Open)
            {
                var errorPayload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    type = "telemetry.stream.error",
                    message = exception.Message
                }, SerializerOptions);
                await socket.SendAsync(errorPayload, WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        finally
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
}
