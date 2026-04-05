using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Domain.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateViewFactory
{
    public IReadOnlyList<RaceLeaderboardEntryModel> BuildLeaderboard(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .OrderBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(driver => new RaceLeaderboardEntryModel(
                driver.DriverNumber,
                ResolveDriverName(driver),
                driver.TeamName ?? "-",
                driver.Position,
                driver.Line,
                driver.GridPosition,
                ResolveGapToLeader(driver),
                NormalizeTimingLabel(driver.IntervalToPositionAhead),
                driver.LastLapTime ?? "-",
                driver.BestLapTime ?? "-",
                driver.TyreCompound ?? "-",
                driver.TyreIsNew,
                driver.CurrentStintLapCount,
                ResolvePitFlag(driver),
                ResolveStatusLabel(driver),
                driver.Speed,
                driver.Gear))
            .ToArray();

    public IReadOnlyList<RaceMapPositionEntryModel> BuildMapPositions(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .Where(driver => driver.CurrentTrackPosition is not null)
            .OrderBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(driver => new RaceMapPositionEntryModel(
                driver.DriverNumber,
                ResolveDriverName(driver),
                driver.Position,
                driver.CurrentTrackPosition?["status"]?.ToString() ?? "-",
                TryParseInt(driver.CurrentTrackPosition?["x"]?.ToString()) ?? 0,
                TryParseInt(driver.CurrentTrackPosition?["y"]?.ToString()) ?? 0,
                TryParseInt(driver.CurrentTrackPosition?["z"]?.ToString()) ?? 0,
                TryParseDateTimeOffset(driver.CurrentTrackPosition?["timestamp"]?.ToString()),
                TryParseBool(driver.CurrentTrackPosition?["isEstimated"]?.ToString()) ?? false))
            .ToArray();

    public RaceDashboardViewModel BuildDashboard(RaceStateSnapshot snapshot)
        => new(
            BuildSessionView(snapshot),
            BuildLeaderboard(snapshot),
            BuildMapPositions(snapshot),
            snapshot);

    public RaceStateSnapshotMessage BuildSnapshotMessage(RaceStateSnapshot snapshot)
        => new(
            "race.state.snapshot",
            DateTimeOffset.UtcNow,
            BuildDashboard(snapshot));

    public RaceStateChangeMessage BuildChangeMessage(RaceStateSnapshot snapshot, string? stateKey, string eventType, int? driverNumber, DateTimeOffset eventTime, long sequence)
        => new(
            "race.state.change",
            DateTimeOffset.UtcNow,
            stateKey,
            eventType,
            snapshot.SessionId ?? string.Empty,
            driverNumber,
            eventTime,
            sequence,
            BuildSessionView(snapshot),
            BuildChangePayload(snapshot, eventType, driverNumber));

    private object BuildChangePayload(RaceStateSnapshot snapshot, string eventType, int? driverNumber)
        => eventType switch
        {
            "session.info.updated" or "track.status.updated" or "lap.count.updated" or "weather.updated"
                => new
                {
                    Section = "session",
                    Session = BuildSessionView(snapshot)
                },
            "race-control.message"
                => new
                {
                    Section = "raceControl",
                    Latest = snapshot.RaceControlMessages.LastOrDefault(),
                    Count = snapshot.RaceControlMessages.Count
                },
            "team-radio.capture"
                => new
                {
                    Section = "teamRadio",
                    Driver = driverNumber is int radioDriverNumber && snapshot.Drivers.TryGetValue(radioDriverNumber, out var radioDriver)
                        ? BuildDriverView(radioDriver)
                        : null,
                    Latest = snapshot.TeamRadioCaptures.LastOrDefault()
                },
            _ when driverNumber is int changedDriverNumber && snapshot.Drivers.TryGetValue(changedDriverNumber, out var driver)
                => new
                {
                    Section = "driver",
                    Driver = BuildDriverView(driver)
                },
            _ => new
            {
                Section = "snapshot",
                Snapshot = snapshot
            }
        };

    private static object BuildSessionView(RaceStateSnapshot snapshot)
        => new
        {
            snapshot.SessionId,
            snapshot.CurrentLap,
            snapshot.TotalLaps,
            snapshot.TrackStatusCode,
            snapshot.TrackStatusMessage,
            snapshot.Weather,
            snapshot.LastProcessedEventTime,
            snapshot.LastProcessedSequence,
            snapshot.UpdatedAt
        };

    private static object BuildDriverView(DriverRaceState driver)
        => new
        {
            driver.DriverNumber,
            DriverName = ResolveDriverName(driver),
            driver.TeamName,
            driver.Position,
            driver.Line,
            driver.GridPosition,
            driver.GapToLeader,
            driver.IntervalToPositionAhead,
            driver.InPit,
            driver.PitOut,
            driver.Retired,
            driver.Stopped,
            driver.Status,
            driver.LastLapTime,
            driver.BestLapTime,
            driver.Sectors,
            driver.Speeds,
            driver.TyreCompound,
            driver.TyreIsNew,
            driver.CurrentStintLapCount,
            driver.CurrentTrackPosition,
            driver.CurrentTrackPositionTimestamp,
            driver.Speed,
            driver.Gear,
            driver.Throttle,
            driver.Brake,
            driver.Drs
        };

    private static string ResolveDriverName(DriverRaceState driver)
        => !string.IsNullOrWhiteSpace(driver.BroadcastName)
            ? driver.BroadcastName!
            : !string.IsNullOrWhiteSpace(driver.FullName)
                ? driver.FullName!
                : driver.DriverNumber.ToString();

    private static string ResolveGapToLeader(DriverRaceState driver)
        => driver.Position == 1 ? "leader" : NormalizeTimingLabel(driver.GapToLeader);

    private static string NormalizeTimingLabel(string? value)
        => string.IsNullOrWhiteSpace(value) || string.Equals(value, "LAP 1", StringComparison.OrdinalIgnoreCase)
            ? "-"
            : value;

    private static string ResolvePitFlag(DriverRaceState driver)
    {
        if (driver.InPit)
        {
            return "IN";
        }

        if (driver.PitOut)
        {
            return "OUT";
        }

        return "-";
    }

    private static string ResolveStatusLabel(DriverRaceState driver)
    {
        if (driver.Retired)
        {
            return "RET";
        }

        if (driver.Stopped)
        {
            return "STOP";
        }

        return driver.Status switch
        {
            80 => "PIT",
            64 => "RUN",
            _ => driver.Status?.ToString() ?? "-"
        };
    }

    private static int? TryParseInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;
    private static bool? TryParseBool(string? value) => bool.TryParse(value, out var parsed) ? parsed : null;
    private static DateTimeOffset? TryParseDateTimeOffset(string? value) => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}
