using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace F1.RaceState.Service.API.Hubs;

public sealed class RaceStateHub(
    IRaceStateStore raceStateStore,
    RaceStateBroadcaster raceStateBroadcaster,
    TelemetryAnalyticsSignalRBridge telemetryBridge,
    ILogger<RaceStateHub> logger) : Hub
{
    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly RaceStateBroadcaster _raceStateBroadcaster = raceStateBroadcaster;
    private readonly TelemetryAnalyticsSignalRBridge _telemetryBridge = telemetryBridge;
    private readonly ILogger<RaceStateHub> _logger = logger;

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR client connected to race-state hub. connectionId={ConnectionId}", Context.ConnectionId);
        await _raceStateBroadcaster.SendInitialStateAsync(Context.ConnectionId, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _telemetryBridge.RemoveConnectionAsync(Context.ConnectionId);
        _logger.LogInformation("SignalR client disconnected from race-state hub. connectionId={ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public Task SendCurrentRaceState()
        => _raceStateBroadcaster.SendInitialStateAsync(Context.ConnectionId, Context.ConnectionAborted);

    public Task SubscribeTelemetry(int driverNumber, string? sessionId = null, int recentSampleCount = 100)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (string.IsNullOrWhiteSpace(resolvedSessionId))
        {
            throw new HubException("sessionId is required.");
        }

        return _telemetryBridge.SubscribeAsync(
            Context.ConnectionId,
            resolvedSessionId,
            driverNumber,
            recentSampleCount,
            Context.ConnectionAborted);
    }

    public Task UnsubscribeTelemetry(int driverNumber, string? sessionId = null)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (string.IsNullOrWhiteSpace(resolvedSessionId))
        {
            throw new HubException("sessionId is required.");
        }

        return _telemetryBridge.UnsubscribeAsync(Context.ConnectionId, resolvedSessionId, driverNumber);
    }

    private string? ResolveSessionId(string? requestedSessionId)
    {
        if (!string.IsNullOrWhiteSpace(requestedSessionId))
        {
            return requestedSessionId;
        }

        return _raceStateStore.GetSnapshot().SessionId;
    }
}
