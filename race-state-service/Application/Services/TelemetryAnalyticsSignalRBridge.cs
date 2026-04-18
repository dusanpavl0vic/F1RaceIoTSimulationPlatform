using System.Collections.Concurrent;
using F1.RaceState.Service.API.Hubs;
using F1.RaceState.Service.Infrastructure.Grpc;
using Microsoft.AspNetCore.SignalR;

namespace F1.RaceState.Service.Application.Services;

public sealed class TelemetryAnalyticsSignalRBridge(
    TelemetryAnalyticsGateway telemetryAnalyticsGateway,
    IHubContext<RaceStateHub> hubContext,
    ILogger<TelemetryAnalyticsSignalRBridge> logger)
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TelemetrySubscription>> _subscriptions = new(StringComparer.Ordinal);
    private readonly TelemetryAnalyticsGateway _telemetryAnalyticsGateway = telemetryAnalyticsGateway;
    private readonly IHubContext<RaceStateHub> _hubContext = hubContext;
    private readonly ILogger<TelemetryAnalyticsSignalRBridge> _logger = logger;

    public async Task SubscribeAsync(
        string connectionId,
        string sessionId,
        int driverNumber,
        int recentSampleCount,
        CancellationToken connectionCancellationToken)
    {
        if (driverNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(driverNumber), "driverNumber must be greater than zero.");
        }

        var subscriptionKey = BuildSubscriptionKey(sessionId, driverNumber);
        var connectionSubscriptions = _subscriptions.GetOrAdd(connectionId, _ => new ConcurrentDictionary<string, TelemetrySubscription>(StringComparer.Ordinal));

        if (connectionSubscriptions.TryRemove(subscriptionKey, out var existing))
        {
            existing.Dispose();
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(connectionCancellationToken);
        var subscription = new TelemetrySubscription(cts);
        connectionSubscriptions[subscriptionKey] = subscription;

        await _hubContext.Clients.Client(connectionId).SendAsync(
            "telemetry.stream.ready",
            new
            {
                type = "telemetry.stream.ready",
                sessionId,
                driverNumber,
                recentSampleCount
            },
            connectionCancellationToken);

        subscription.RunTask = RunSubscriptionAsync(connectionId, sessionId, driverNumber, recentSampleCount, subscriptionKey, subscription);
    }

    public Task UnsubscribeAsync(string connectionId, string sessionId, int driverNumber)
    {
        if (_subscriptions.TryGetValue(connectionId, out var connectionSubscriptions)
            && connectionSubscriptions.TryRemove(BuildSubscriptionKey(sessionId, driverNumber), out var subscription))
        {
            subscription.Dispose();
            if (connectionSubscriptions.IsEmpty)
            {
                _subscriptions.TryRemove(connectionId, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        if (_subscriptions.TryRemove(connectionId, out var connectionSubscriptions))
        {
            foreach (var subscription in connectionSubscriptions.Values)
            {
                subscription.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    private async Task RunSubscriptionAsync(
        string connectionId,
        string sessionId,
        int driverNumber,
        int recentSampleCount,
        string subscriptionKey,
        TelemetrySubscription subscription)
    {
        try
        {
            await foreach (var sample in _telemetryAnalyticsGateway.StreamDriverTelemetryAsync(
                               sessionId,
                               driverNumber,
                               recentSampleCount,
                               subscription.CancellationTokenSource.Token))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync(
                    "telemetry.sample",
                    new
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
                    },
                    subscription.CancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException) when (subscription.CancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (!subscription.CancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "SignalR telemetry stream failed for connection {ConnectionId}, session {SessionId}, driver {DriverNumber}.",
                connectionId,
                sessionId,
                driverNumber);

            try
            {
                await _hubContext.Clients.Client(connectionId).SendAsync(
                    "telemetry.stream.error",
                    new
                    {
                        type = "telemetry.stream.error",
                        message = exception.Message
                    },
                    CancellationToken.None);
            }
            catch
            {
            }
        }
        finally
        {
            RemoveSubscriptionIfCurrent(connectionId, subscriptionKey, subscription);
        }
    }

    private void RemoveSubscriptionIfCurrent(string connectionId, string subscriptionKey, TelemetrySubscription subscription)
    {
        if (_subscriptions.TryGetValue(connectionId, out var connectionSubscriptions)
            && connectionSubscriptions.TryGetValue(subscriptionKey, out var current)
            && ReferenceEquals(current, subscription))
        {
            connectionSubscriptions.TryRemove(subscriptionKey, out _);
            if (connectionSubscriptions.IsEmpty)
            {
                _subscriptions.TryRemove(connectionId, out _);
            }
        }

        subscription.Dispose();
    }

    private static string BuildSubscriptionKey(string sessionId, int driverNumber)
        => $"{sessionId}:{driverNumber}";

    private sealed class TelemetrySubscription(CancellationTokenSource cancellationTokenSource) : IDisposable
    {
        private int _disposed;

        public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
        public Task? RunTask { get; set; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }
    }
}
