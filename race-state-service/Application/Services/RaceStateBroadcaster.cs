using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateBroadcaster(
    IHubContext<RaceStateHub> hubContext,
    IRaceStateStore raceStateStore,
    RaceStateViewFactory viewFactory,
    ILogger<RaceStateBroadcaster> logger)
{
    private readonly IHubContext<RaceStateHub> _hubContext = hubContext;
    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly RaceStateViewFactory _viewFactory = viewFactory;
    private readonly ILogger<RaceStateBroadcaster> _logger = logger;

    public async Task SendInitialStateAsync(string connectionId, CancellationToken cancellationToken)
    {
        var payload = _viewFactory.BuildBroadcastMessage(_raceStateStore.GetSnapshot());
        await _hubContext.Clients.Client(connectionId).SendAsync(payload.Type, payload, cancellationToken);
        _logger.LogDebug("Sent initial SignalR race-state snapshot to connection {ConnectionId}.", connectionId);
    }

    public async Task BroadcastAsync(CancellationToken cancellationToken)
    {
        var snapshot = _raceStateStore.GetSnapshot();
        var payload = _viewFactory.BuildBroadcastMessage(snapshot);
        await _hubContext.Clients.All.SendAsync(payload.Type, payload, cancellationToken);
        _logger.LogDebug("Broadcast SignalR race-state update for session {SessionId}.", snapshot.SessionId);
    }
}
