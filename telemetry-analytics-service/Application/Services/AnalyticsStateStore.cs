using System.Globalization;
using System.Text.Json.Nodes;
using F1.Shared.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;

namespace F1.TelemetryAnalytics.Service.Application.Services;

public sealed class AnalyticsStateStore
{
    private readonly object _gate = new();
    private readonly AnalyticsSessionState _session = new();

    public AnalyticsSessionState Snapshot()
    {
        lock (_gate)
        {
            return new AnalyticsSessionState
            {
                SessionId = _session.SessionId,
                MeetingName = _session.MeetingName,
                SessionName = _session.SessionName,
                SessionStatus = _session.SessionStatus,
                TrackStatusCode = _session.TrackStatusCode,
                TrackStatusLabel = _session.TrackStatusLabel,
                CurrentLap = _session.CurrentLap,
                TotalLaps = _session.TotalLaps,
                UpdatedAt = _session.UpdatedAt
            };
        }
    }

    public AnalyticsApplyOutcome Apply(CanonicalEvent canonicalEvent)
    {
        lock (_gate)
        {
            _session.SessionId = canonicalEvent.SessionId;
            _session.UpdatedAt = canonicalEvent.EventTime;

            return canonicalEvent.EventType switch
            {
                "session.info.updated" => ApplySessionInfo(canonicalEvent),
                "session.status.updated" => ApplySessionStatus(canonicalEvent),
                "track.status.updated" => ApplyTrackStatus(canonicalEvent),
                "lap.count.updated" => ApplyLapCount(canonicalEvent),
                "driver.list.updated" => ApplyDriverMetadata(canonicalEvent),
                "timing.driver.updated" => ApplyTiming(canonicalEvent),
                "timing.stats.updated" => ApplyTimingStats(canonicalEvent),
                "timing.app.updated" => ApplyTimingApp(canonicalEvent),
                "lap.series.updated" => ApplyLapSeries(canonicalEvent),
                "tyres.current.updated" => ApplyCurrentTyres(canonicalEvent),
                "tyres.stint.updated" => ApplyTyreStints(canonicalEvent),
                "car.telemetry.updated" => ApplyTelemetry(canonicalEvent),
                _ => new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null)
            };
        }
    }

    private AnalyticsApplyOutcome ApplySessionInfo(CanonicalEvent canonicalEvent)
    {
        var data = canonicalEvent.Payload["data"];
        _session.MeetingName = FirstNonEmpty(
            ResolveText(data, "Meeting", "Name"),
            ResolveText(data, "MeetingName"),
            _session.MeetingName);
        _session.SessionName = FirstNonEmpty(
            ResolveText(data, "Name"),
            ResolveText(data, "Session", "Name"),
            ResolveText(data, "OfficialName"),
            _session.SessionName);

        return new AnalyticsApplyOutcome(true, true, false, false, null, null, null, null);
    }

    private AnalyticsApplyOutcome ApplySessionStatus(CanonicalEvent canonicalEvent)
    {
        var data = canonicalEvent.Payload["data"];
        _session.SessionStatus = FirstNonEmpty(
            data?["Status"]?.ToString(),
            data?["status"]?.ToString(),
            _session.SessionStatus);

        return new AnalyticsApplyOutcome(true, true, false, false, null, null, null, null);
    }

    private AnalyticsApplyOutcome ApplyTrackStatus(CanonicalEvent canonicalEvent)
    {
        var data = canonicalEvent.Payload["data"];
        _session.TrackStatusCode = FirstNonEmpty(data?["Status"]?.ToString(), _session.TrackStatusCode);
        _session.TrackStatusLabel = FirstNonEmpty(
            ResolveTrackStatusLabel(_session.TrackStatusCode, data?["Message"]?.ToString()),
            _session.TrackStatusLabel);

        return new AnalyticsApplyOutcome(true, true, false, false, null, null, null, null);
    }

    private AnalyticsApplyOutcome ApplyLapCount(CanonicalEvent canonicalEvent)
    {
        var data = canonicalEvent.Payload["data"];
        _session.CurrentLap = FirstNonNull(TryParseInt(data?["CurrentLap"]?.ToString()), _session.CurrentLap);
        _session.TotalLaps = FirstNonNull(_session.TotalLaps, TryParseInt(data?["TotalLaps"]?.ToString()))
            ?? TryParseInt(data?["TotalLaps"]?.ToString());

        return new AnalyticsApplyOutcome(true, true, false, false, null, null, null, null);
    }

    private AnalyticsApplyOutcome ApplyDriverMetadata(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        var payload = canonicalEvent.Payload["driver"];
        driver.Tla = FirstNonEmpty(payload?["Tla"]?.ToString(), driver.Tla);
        driver.BroadcastName = FirstNonEmpty(payload?["BroadcastName"]?.ToString(), driver.BroadcastName);
        driver.FullName = FirstNonEmpty(
            payload?["FullName"]?.ToString(),
            BuildFullName(payload?["FirstName"]?.ToString(), payload?["LastName"]?.ToString()),
            driver.FullName);
        driver.TeamName = FirstNonEmpty(payload?["TeamName"]?.ToString(), driver.TeamName);
        driver.TeamColor = FirstNonEmpty(NormalizeColor(payload?["TeamColour"]?.ToString()), driver.TeamColor);
        driver.LastUpdateTimestamp = canonicalEvent.EventTime;

        return new AnalyticsApplyOutcome(true, false, true, false, driver, null, null, null);
    }

    private AnalyticsApplyOutcome ApplyTiming(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        var timing = canonicalEvent.Payload["timing"];
        var previousCompletedLaps = driver.CompletedLaps ?? 0;
        var incomingCompletedLaps = TryParseInt(timing?["NumberOfLaps"]?.ToString()) ?? driver.CompletedLaps;

        driver.Position = FirstNonNull(TryParseInt(timing?["Position"]?.ToString()), driver.Position);
        driver.Line = FirstNonNull(TryParseInt(timing?["Line"]?.ToString()), driver.Line);
        driver.GapToLeader = FirstNonEmpty(ExtractTimingValue(timing?["GapToLeader"]), driver.GapToLeader);
        driver.IntervalToPositionAhead = FirstNonEmpty(ExtractTimingValue(timing?["IntervalToPositionAhead"]), driver.IntervalToPositionAhead);
        driver.InPit = TryParseBool(timing?["InPit"]?.ToString()) ?? driver.InPit;
        driver.PitOut = TryParseBool(timing?["PitOut"]?.ToString()) ?? driver.PitOut;
        driver.Retired = TryParseBool(timing?["Retired"]?.ToString()) ?? driver.Retired;
        driver.Stopped = TryParseBool(timing?["Stopped"]?.ToString()) ?? driver.Stopped;
        driver.Status = FirstNonNull(TryParseInt(timing?["Status"]?.ToString()), driver.Status);
        driver.NumberOfPitStops = FirstNonNull(TryParseInt(timing?["NumberOfPitStops"]?.ToString()), driver.NumberOfPitStops);
        driver.BestLapTime = FirstNonEmpty(ExtractTimingValue(timing?["BestLapTime"]), driver.BestLapTime);
        driver.LastLapTime = FirstNonEmpty(ExtractTimingValue(timing?["LastLapTime"]), driver.LastLapTime);
        driver.Sector1Time = FirstNonEmpty(ExtractSectorValue(timing?["Sectors"], 0), driver.Sector1Time);
        driver.Sector2Time = FirstNonEmpty(ExtractSectorValue(timing?["Sectors"], 1), driver.Sector2Time);
        driver.Sector3Time = FirstNonEmpty(ExtractSectorValue(timing?["Sectors"], 2), driver.Sector3Time);
        driver.CompletedLaps = FirstNonNull(incomingCompletedLaps, driver.CompletedLaps);
        driver.LastUpdateTimestamp = canonicalEvent.EventTime;

        LapSummaryRecord? completedLap = null;
        if (incomingCompletedLaps is int completedLaps && completedLaps > previousCompletedLaps)
        {
            var finishedLapNumber = completedLaps;
            if (driver.CurrentLapAggregate?.LapNumber == finishedLapNumber)
            {
                completedLap = BuildLapSummary(_session.SessionId, driver, finishedLapNumber, driver.CurrentLapAggregate, canonicalEvent.EventTime);
                driver.CurrentLapAggregate = new LapAggregateState { LapNumber = finishedLapNumber + 1 };
                driver.NextTelemetrySampleIndex = 0;
            }
        }

        return new AnalyticsApplyOutcome(true, false, true, false, driver, completedLap, null, null);
    }

    private AnalyticsApplyOutcome ApplyTimingStats(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        var timingStats = canonicalEvent.Payload["timingStats"];
        driver.Line = FirstNonNull(TryParseInt(timingStats?["Line"]?.ToString()), driver.Line);
        driver.BestLapTime = FirstNonEmpty(ExtractTimingValue(timingStats?["PersonalBestLapTime"]), driver.BestLapTime);
        driver.LastUpdateTimestamp = canonicalEvent.EventTime;

        return new AnalyticsApplyOutcome(true, false, true, false, driver, null, null, null);
    }

    private AnalyticsApplyOutcome ApplyTimingApp(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        var payload = canonicalEvent.Payload["timingApp"];
        driver.Line = FirstNonNull(TryParseInt(payload?["Line"]?.ToString()), driver.Line);
        driver.GridPosition = FirstNonNull(TryParseInt(payload?["GridPos"]?.ToString()), driver.GridPosition);
        driver.LastUpdateTimestamp = canonicalEvent.EventTime;

        var stintSummary = payload?["Stints"] is JsonObject stints
            ? ApplyStints(driver, stints, canonicalEvent.EventTime)
            : null;

        return new AnalyticsApplyOutcome(true, false, true, stintSummary is not null, driver, null, stintSummary, null);
    }

    private AnalyticsApplyOutcome ApplyLapSeries(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        var (lapNumber, position) = ResolveLapSeriesPosition(canonicalEvent.Payload["lapSeries"]?["LapPosition"]);

        driver.Position = FirstNonNull(position, driver.Position);
        if (lapNumber is int completedLapFromSeries)
        {
            driver.CompletedLaps = Math.Max(driver.CompletedLaps ?? 0, completedLapFromSeries);
        }

        driver.LastUpdateTimestamp = canonicalEvent.EventTime;

        return new AnalyticsApplyOutcome(true, false, true, false, driver, null, null, null);
    }

    private AnalyticsApplyOutcome ApplyCurrentTyres(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        var tyres = canonicalEvent.Payload["tyres"];
        driver.CurrentCompound = FirstNonEmpty(tyres?["Compound"]?.ToString(), driver.CurrentCompound);
        driver.TyreIsNew = TryParseBool(tyres?["New"]?.ToString()) ?? driver.TyreIsNew;
        driver.LastUpdateTimestamp = canonicalEvent.EventTime;

        return new AnalyticsApplyOutcome(true, false, true, false, driver, null, null, null);
    }

    private AnalyticsApplyOutcome ApplyTyreStints(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber || canonicalEvent.Payload["stints"] is not JsonObject stints)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        driver.LastUpdateTimestamp = canonicalEvent.EventTime;
        var stintSummary = ApplyStints(driver, stints, canonicalEvent.EventTime);

        return new AnalyticsApplyOutcome(true, false, true, stintSummary is not null, driver, null, stintSummary, null);
    }

    private AnalyticsApplyOutcome ApplyTelemetry(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return new AnalyticsApplyOutcome(false, false, false, false, null, null, null, null);
        }

        var driver = GetOrCreateDriver(driverNumber);
        var telemetry = canonicalEvent.Payload["telemetry"];
        var lapNumber = driver.CurrentTelemetryLapNumber;
        if (driver.CurrentLapAggregate is null || driver.CurrentLapAggregate.LapNumber != lapNumber)
        {
            driver.CurrentLapAggregate = new LapAggregateState { LapNumber = lapNumber };
            driver.NextTelemetrySampleIndex = 0;
        }

        var speed = TryParseInt(telemetry?["speed"]?.ToString());
        var rpm = TryParseInt(telemetry?["rpm"]?.ToString());
        var gear = TryParseInt(telemetry?["gear"]?.ToString());
        var throttlePct = TryParseInt(telemetry?["throttlePct"]?.ToString());
        var brakeApplied = TryParseBool(telemetry?["brakeApplied"]?.ToString());
        var drsEnabled = TryParseBool(telemetry?["drsEnabled"]?.ToString());

        driver.CurrentLapAggregate.Absorb(speed, throttlePct, brakeApplied, drsEnabled);
        driver.LastUpdateTimestamp = canonicalEvent.EventTime;

        return new AnalyticsApplyOutcome(
            true,
            false,
            false,
            false,
            driver,
            null,
            null,
            new TelemetrySampleRecord(
                canonicalEvent.SessionId,
                driverNumber,
                lapNumber,
                Math.Max(1, driver.StintNumber),
                ++driver.NextTelemetrySampleIndex,
                canonicalEvent.EventTime,
                speed,
                rpm,
                gear,
                throttlePct,
                brakeApplied,
                drsEnabled));
    }

    private AnalyticsDriverState GetOrCreateDriver(int driverNumber)
    {
        if (!_session.Drivers.TryGetValue(driverNumber, out var driver))
        {
            driver = new AnalyticsDriverState { DriverNumber = driverNumber };
            _session.Drivers[driverNumber] = driver;
        }

        return driver;
    }

    private static StintSummaryRecord? ApplyStints(AnalyticsDriverState driver, JsonObject stints, DateTimeOffset updatedAt)
    {
        var current = stints
            .Select(pair => (Index: TryParseInt(pair.Key), Data: pair.Value as JsonObject))
            .Where(item => item.Index is not null && item.Data is not null)
            .OrderByDescending(item => item.Index)
            .FirstOrDefault();

        if (current.Index is null || current.Data is null)
        {
            return null;
        }

        var startLaps = TryParseInt(current.Data["StartLaps"]?.ToString());
        var totalLaps = TryParseInt(current.Data["TotalLaps"]?.ToString());

        driver.StintNumber = current.Index.Value + 1;
        driver.CurrentCompound = FirstNonEmpty(current.Data["Compound"]?.ToString(), driver.CurrentCompound);
        driver.TyreIsNew = TryParseBool(current.Data["New"]?.ToString()) ?? driver.TyreIsNew;
        driver.TyreLaps = FirstNonNull(totalLaps, driver.TyreLaps);
        driver.CurrentStintStartLap = FirstNonNull(startLaps, driver.CurrentStintStartLap);

        return new StintSummaryRecord(
            string.Empty,
            driver.DriverNumber,
            driver.StintNumber,
            driver.CurrentCompound,
            driver.TyreIsNew,
            driver.CurrentStintStartLap,
            driver.CompletedLaps,
            totalLaps,
            updatedAt);
    }

    private static LapSummaryRecord BuildLapSummary(string sessionId, AnalyticsDriverState driver, int lapNumber, LapAggregateState aggregate, DateTimeOffset updatedAt)
        => new(
            sessionId,
            driver.DriverNumber,
            lapNumber,
            driver.Position,
            driver.GapToLeader,
            driver.IntervalToPositionAhead,
            driver.LastLapTime,
            ParseLapTimeToMilliseconds(driver.LastLapTime),
            driver.BestLapTime,
            ParseLapTimeToMilliseconds(driver.BestLapTime),
            driver.Sector1Time,
            ParseLapTimeToMilliseconds(driver.Sector1Time),
            driver.Sector2Time,
            ParseLapTimeToMilliseconds(driver.Sector2Time),
            driver.Sector3Time,
            ParseLapTimeToMilliseconds(driver.Sector3Time),
            driver.CurrentCompound,
            driver.TyreIsNew,
            driver.TyreLaps,
            Math.Max(1, driver.StintNumber),
            aggregate.AverageSpeed,
            aggregate.MaxSpeed,
            aggregate.AverageThrottlePct,
            aggregate.BrakeUsagePct,
            aggregate.DrsUsagePct,
            updatedAt);

    private static string? ResolveText(JsonNode? node, params string[] path)
    {
        var current = node;
        foreach (var segment in path)
        {
            current = current?[segment];
            if (current is null)
            {
                return null;
            }
        }

        return current?.ToString();
    }

    private static string? ResolveTrackStatusLabel(string? code, string? message)
        => code switch
        {
            "1" => "GREEN",
            "2" => "YELLOW",
            "4" => "SAFETY_CAR",
            "5" => "RED_FLAG",
            "6" => "VIRTUAL_SAFETY_CAR",
            "7" => "VSC_ENDING",
            _ => FirstNonEmpty(message, "UNKNOWN")
        };

    private static string? ExtractTimingValue(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue)
        {
            return NormalizeTimingValue(node.ToString());
        }

        return NormalizeTimingValue(node["Value"]?.ToString());
    }

    private static string? ExtractSectorValue(JsonNode? node, int index)
        => node is JsonArray sectors
            ? ExtractTimingValue(sectors.ElementAtOrDefault(index))
            : ExtractTimingValue(node?[index.ToString()]);

    private static (int? LapNumber, int? Position) ResolveLapSeriesPosition(JsonNode? lapPositionNode)
    {
        if (lapPositionNode is JsonObject lapPositions)
        {
            var latest = lapPositions
                .Select(entry => (LapNumber: TryParseInt(entry.Key), Position: TryParseInt(entry.Value?.ToString())))
                .Where(entry => entry.LapNumber is not null && entry.Position is not null)
                .OrderByDescending(entry => entry.LapNumber)
                .FirstOrDefault();

            return latest;
        }

        if (lapPositionNode is JsonArray lapPositionArray)
        {
            var position = lapPositionArray
                .Select(node => TryParseInt(node?.ToString()))
                .LastOrDefault(value => value is not null);

            return (null, position);
        }

        return (null, null);
    }

    private static string? NormalizeTimingValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith('{'))
        {
            return null;
        }

        return normalized;
    }

    private static int? ParseLapTimeToMilliseconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Contains(':', StringComparison.Ordinal))
        {
            var parts = normalized.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return null;
            }

            return (int)Math.Round((minutes * 60d + seconds) * 1000d, MidpointRounding.AwayFromZero);
        }

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var onlySeconds)
            ? (int)Math.Round(onlySeconds * 1000d, MidpointRounding.AwayFromZero)
            : null;
    }

    private static int? TryParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static bool? TryParseBool(string? value)
        => bool.TryParse(value, out var parsed) ? parsed : null;

    private static string? BuildFullName(string? firstName, string? lastName)
    {
        var fullName = string.Join(" ", new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    private static string? NormalizeColor(string? rawColor)
    {
        if (string.IsNullOrWhiteSpace(rawColor))
        {
            return null;
        }

        var value = rawColor.Trim();
        return value.StartsWith('#') ? value : $"#{value}";
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static int? FirstNonNull(int? left, int? right)
        => left ?? right;
}
