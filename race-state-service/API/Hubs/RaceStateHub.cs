using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace F1.RaceState.Service.API.Hubs;

public sealed class RaceStateHub(
    RaceStateBroadcaster raceStateBroadcaster,
    ILogger<RaceStateHub> logger) : Hub
{
    private readonly RaceStateBroadcaster _raceStateBroadcaster = raceStateBroadcaster;
    private readonly ILogger<RaceStateHub> _logger = logger;

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR client connected to race-state hub. connectionId={ConnectionId}", Context.ConnectionId);
        await _raceStateBroadcaster.SendInitialStateAsync(Context.ConnectionId, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR client disconnected from race-state hub. connectionId={ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public Task SendCurrentRaceState()
        => _raceStateBroadcaster.SendInitialStateAsync(Context.ConnectionId, Context.ConnectionAborted);
}
