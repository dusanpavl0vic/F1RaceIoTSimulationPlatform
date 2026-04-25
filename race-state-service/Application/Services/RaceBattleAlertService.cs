using System.Collections.Concurrent;
using System.Globalization;
using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceBattleAlertService(
    IRaceStateStore raceStateStore,
    RaceStateViewFactory viewFactory,
    RaceStateBroadcaster raceStateBroadcaster,
    ILogger<RaceBattleAlertService> logger)
{
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromSeconds(8);

    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly RaceStateViewFactory _viewFactory = viewFactory;
    private readonly RaceStateBroadcaster _raceStateBroadcaster = raceStateBroadcaster;
    private readonly ILogger<RaceBattleAlertService> _logger = logger;
    private readonly ConcurrentDictionary<string, AlertDispatchState> _dispatchStates = new(StringComparer.Ordinal);

    public async Task<BattleAlertMessage?> PublishAsync(BattleAlertCandidateRequest candidate, CancellationToken cancellationToken)
    {
        if (candidate.DriverNumber <= 0)
        {
            return null;
        }

        var snapshot = _raceStateStore.GetSnapshot();
        if (string.IsNullOrWhiteSpace(snapshot.SessionId))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(candidate.SessionId)
            && !string.Equals(candidate.SessionId, snapshot.SessionId, StringComparison.Ordinal))
        {
            _logger.LogDebug(
                "Ignoring battle alert candidate for stale session {CandidateSessionId}. Current session is {CurrentSessionId}.",
                candidate.SessionId,
                snapshot.SessionId);
            return null;
        }

        var leaderboard = _viewFactory.BuildLeaderboard(snapshot);
        var trailingDriver = leaderboard.FirstOrDefault(driver => driver.DriverNumber == candidate.DriverNumber);
        if (trailingDriver?.Position is not int trailingPosition || trailingPosition <= 1)
        {
            return null;
        }

        var battleForPosition = candidate.Position is > 1 ? candidate.Position.Value - 1 : trailingPosition - 1;
        if (battleForPosition <= 0)
        {
            return null;
        }

        var aheadDriver = leaderboard.FirstOrDefault(driver => driver.Position == battleForPosition);
        if (aheadDriver is null || aheadDriver.DriverNumber == trailingDriver.DriverNumber)
        {
            return null;
        }

        var dispatchKey = $"{snapshot.SessionId}:{trailingDriver.DriverNumber}:{aheadDriver.DriverNumber}:{battleForPosition}";
        var dispatchState = _dispatchStates.GetOrAdd(dispatchKey, _ => new AlertDispatchState(DateTimeOffset.MinValue, DateTimeOffset.MinValue));
        var now = DateTimeOffset.UtcNow;
        if (candidate.EventTime <= dispatchState.LastEventTime || now - dispatchState.LastSentAt < AlertCooldown)
        {
            return null;
        }

        var gapLabel = FormatGapLabel(candidate.GapSeconds, candidate.GapLabel);
        var trailingLabel = FormatDriverLabel(trailingDriver.Tla, trailingDriver.BroadcastName, trailingDriver.FullName, trailingDriver.DriverNumber);
        var aheadLabel = FormatDriverLabel(aheadDriver.Tla, aheadDriver.BroadcastName, aheadDriver.FullName, aheadDriver.DriverNumber);
        var message = new BattleAlertMessage(
            "battle.alert",
            now,
            snapshot.SessionId!,
            trailingDriver.DriverNumber,
            trailingLabel,
            aheadDriver.DriverNumber,
            aheadLabel,
            battleForPosition,
            candidate.GapSeconds,
            gapLabel,
            $"Battle for P{battleForPosition}: {trailingLabel} is {gapLabel} behind {aheadLabel}.");

        _dispatchStates[dispatchKey] = new AlertDispatchState(candidate.EventTime, now);
        await _raceStateBroadcaster.BroadcastBattleAlertAsync(message, cancellationToken);
        _logger.LogInformation(
            "Broadcast battle alert for session {SessionId}. trailingDriver={DriverNumber}, aheadDriver={AheadDriverNumber}, position={Position}, gap={GapLabel}.",
            snapshot.SessionId,
            trailingDriver.DriverNumber,
            aheadDriver.DriverNumber,
            battleForPosition,
            gapLabel);

        return message;
    }

    private static string FormatDriverLabel(string? tla, string? broadcastName, string? fullName, int driverNumber)
    {
        var name = FirstNonEmpty(tla, broadcastName, fullName);
        return string.IsNullOrWhiteSpace(name)
            ? $"#{driverNumber}"
            : $"{name.Trim()} #{driverNumber}";
    }

    private static string FormatGapLabel(double? gapSeconds, string? rawGapLabel)
    {
        if (gapSeconds is >= 0)
        {
            return $"{gapSeconds.Value.ToString("0.000", CultureInfo.InvariantCulture)}s";
        }

        var trimmed = rawGapLabel?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "under 1.000s";
        }

        var normalized = trimmed.TrimStart('+');
        return normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{normalized}s";
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private sealed record AlertDispatchState(
        DateTimeOffset LastEventTime,
        DateTimeOffset LastSentAt);
}
