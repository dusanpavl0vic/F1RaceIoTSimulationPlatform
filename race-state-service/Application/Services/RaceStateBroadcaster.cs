using System.Net.WebSockets;
using System.Text.Json;
using F1.RaceState.Service.Application.Contracts;
using F1.Shared.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateBroadcaster(IRaceStateStore raceStateStore, RaceStateViewFactory viewFactory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly List<WebSocket> _clients = [];
    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly RaceStateViewFactory _viewFactory = viewFactory;

    public async Task AddClientAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _clients.Add(socket);
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
    }
}
