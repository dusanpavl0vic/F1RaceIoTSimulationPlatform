using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Domain.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateReadService(IRaceStateStore raceStateStore, RaceStateViewFactory viewFactory)
{
    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly RaceStateViewFactory _viewFactory = viewFactory;

    public RaceCurrentStateViewModel GetCurrent()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return _viewFactory.BuildCurrentState(snapshot);
    }

    public RaceRecoveryStateViewModel GetRecoveryState()
    {
        var checkpoint = _raceStateStore.GetCheckpoint();
        return new RaceRecoveryStateViewModel(
            checkpoint.Snapshot.SessionId,
            checkpoint.Snapshot.UpdatedAt,
            checkpoint.Snapshot.LastProcessedEventTime,
            checkpoint.Snapshot.LastProcessedSequence,
            checkpoint.Snapshot.Drivers.Count,
            checkpoint.LastAppliedEventVersions.Count);
    }

    public IReadOnlyList<RaceLeaderboardEntryModel> GetDrivers()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return _viewFactory.BuildLeaderboard(snapshot);
    }

    public RaceLeaderboardViewModel GetLeaderboard()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return new RaceLeaderboardViewModel(
            snapshot.SessionId,
            snapshot.CurrentLap,
            snapshot.TotalLaps,
            snapshot.TrackStatusCode,
            snapshot.TrackStatusMessage,
            _viewFactory.BuildLeaderboard(snapshot));
    }

    public RaceMapViewModel GetMap()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return new RaceMapViewModel(
            snapshot.SessionId,
            _viewFactory.BuildMapPositions(snapshot));
    }

    public RaceDashboardViewModel GetDashboard()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return _viewFactory.BuildDashboard(snapshot);
    }

    public RaceDriverPredictionFeaturesViewModel? GetDriverPredictionFeatures(int driverNumber)
    {
        var snapshot = _raceStateStore.GetSnapshot();
        if (!snapshot.Drivers.TryGetValue(driverNumber, out var driver))
        {
            return null;
        }

        var lastCompletedLapNumber = driver.LastRecordedLapTimeLapNumber;
        var lastCompletedLapTimeSeconds = driver.LapTimeHistorySeconds.Count > 0
            ? driver.LapTimeHistorySeconds[^1]
            : (double?)null;
        var lapTimeAvgLast3 = CalculateRollingAverage(driver.LapTimeHistorySeconds, 3);
        var lapTimeAvgLast5 = CalculateRollingAverage(driver.LapTimeHistorySeconds, 5);
        var stintNumber = ResolveCurrentStintNumber(driver);

        RaceDriverPredictionFeaturePayload? features = null;
        if (lastCompletedLapNumber is not null && lastCompletedLapTimeSeconds is not null)
        {
            features = new RaceDriverPredictionFeaturePayload(
                driver.DriverNumber,
                lastCompletedLapNumber.Value,
                driver.Position ?? driver.LapSeriesPosition,
                lastCompletedLapTimeSeconds,
                TryParseLapTimeSeconds(driver.BestLapTime),
                lapTimeAvgLast3 ?? lastCompletedLapTimeSeconds,
                lapTimeAvgLast5 ?? lastCompletedLapTimeSeconds,
                TryParseGapSeconds(driver.GapToLeader),
                TryParseGapSeconds(driver.IntervalToPositionAhead),
                stintNumber,
                driver.TyreCompound,
                driver.TyreIsNew,
                driver.CurrentStintLapCount,
                driver.InPit,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        return new RaceDriverPredictionFeaturesViewModel(
            snapshot.SessionId,
            driver.DriverNumber,
            ResolveDriverName(driver),
            driver.Position ?? driver.LapSeriesPosition,
            lastCompletedLapNumber,
            lastCompletedLapTimeSeconds,
            lapTimeAvgLast3,
            lapTimeAvgLast5,
            stintNumber,
            driver.TyreCompound,
            driver.TyreIsNew,
            driver.CurrentStintLapCount,
            driver.InPit,
            TryParseGapSeconds(driver.GapToLeader),
            TryParseGapSeconds(driver.IntervalToPositionAhead),
            features);
    }

    private static string ResolveDriverName(DriverRaceState driver)
        => !string.IsNullOrWhiteSpace(driver.Tla)
            ? driver.Tla!
            : !string.IsNullOrWhiteSpace(driver.BroadcastName)
                ? driver.BroadcastName!
                : !string.IsNullOrWhiteSpace(driver.FullName)
                    ? driver.FullName!
                    : $"#{driver.DriverNumber}";

    private static int? ResolveCurrentStintNumber(DriverRaceState driver)
    {
        if (driver.TyreStints is null || driver.TyreStints.Count == 0)
        {
            return null;
        }

        return driver.TyreStints
            .Select(entry => int.TryParse(entry.Key, out var parsed) ? parsed + 1 : (int?)null)
            .Where(value => value is not null)
            .DefaultIfEmpty()
            .Max();
    }

    private static double? CalculateRollingAverage(IReadOnlyList<double> values, int window)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var selected = values.TakeLast(Math.Min(window, values.Count)).ToArray();
        return selected.Sum() / selected.Length;
    }

    private static double? TryParseLapTimeSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is "-" or "PIT" or "PIT IN" or "PIT OUT" or "STOP")
        {
            return null;
        }

        if (normalized.Contains(':'))
        {
            var parts = normalized.Split(':', 2);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var minutes)
                && double.TryParse(parts[1], out var seconds))
            {
                return minutes * 60d + seconds;
            }

            return null;
        }

        return double.TryParse(normalized, out var parsed) ? parsed : null;
    }

    private static double? TryParseGapSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is "-" or "LEADER" || normalized.StartsWith("LAP ", StringComparison.Ordinal))
        {
            return null;
        }

        return double.TryParse(normalized.TrimStart('+'), out var parsed) ? parsed : null;
    }

}
