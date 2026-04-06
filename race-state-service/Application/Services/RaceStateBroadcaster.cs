using System.Net.WebSockets;
using System.Text.Json;
using F1.RaceState.Service.Application.Contracts;
using F1.Shared.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateBroadcaster(
    IRaceStateStore raceStateStore,
    RaceStateViewFactory viewFactory,
    ILogger<RaceStateBroadcaster> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly List<WebSocket> _clients = [];
    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly RaceStateViewFactory _viewFactory = viewFactory;
    private readonly ILogger<RaceStateBroadcaster> _logger = logger;

    public async Task AddClientAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _clients.Add(socket);
            _logger.LogInformation("WS client connected to race state stream. Connected clients: {ClientCount}.", _clients.Count);
        }

        var initialPayload = JsonSerializer.SerializeToUtf8Bytes(
            _viewFactory.BuildSnapshotMessage(_raceStateStore.GetSnapshot()),
            SerializerOptions);
        await socket.SendAsync(initialPayload, WebSocketMessageType.Text, true, cancellationToken);

        var buffer = new byte[1024];
        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        finally
        {
            lock (_gate)
            {
                _clients.Remove(socket);
                _logger.LogInformation("WS client disconnected from race state stream. Connected clients: {ClientCount}.", _clients.Count);
            }

            if (socket.State != WebSocketState.Closed)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }

    public async Task BroadcastChangeAsync(string? stateKey, CanonicalEvent canonicalEvent, CancellationToken cancellationToken)
    {
        List<WebSocket> clients;
        lock (_gate)
        {
            clients = _clients.Where(client => client.State == WebSocketState.Open).ToList();
        }

        if (clients.Count == 0)
        {
            _logger.LogDebug(
                "Skipping WS change broadcast for {EventType} because there are no connected clients.",
                canonicalEvent.EventType);
            return;
        }

        var snapshot = _raceStateStore.GetSnapshot();
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            _viewFactory.BuildChangeMessage(
                snapshot,
                stateKey,
                canonicalEvent.EventType,
                canonicalEvent.DriverNumber,
                canonicalEvent.EventTime,
                canonicalEvent.Sequence),
            SerializerOptions);

        foreach (var client in clients)
        {
            await client.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
        }

        _logger.LogDebug(
            "Broadcast WS change for {EventType} stateKey={StateKey} driver={DriverNumber} to {ClientCount} clients.",
            canonicalEvent.EventType,
            stateKey,
            canonicalEvent.DriverNumber,
            clients.Count);
    }
}
