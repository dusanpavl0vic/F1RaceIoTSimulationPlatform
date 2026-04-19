using System.Globalization;
using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Infrastructure.Persistence.Parsing;

internal static class InfluxTelemetryCsvParser
{
    public static IReadOnlyList<TelemetryPointDto> Parse(string csv)
    {
        var lines = csv
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#'))
            .ToArray();

        if (lines.Length <= 1)
        {
            return [];
        }

        var headers = lines[0].Split(',');
        var timeIndex = Array.IndexOf(headers, "_time");
        var sampleIndex = Array.IndexOf(headers, "sample_index");
        var speedIndex = Array.IndexOf(headers, "speed");
        var rpmIndex = Array.IndexOf(headers, "rpm");
        var throttleIndex = Array.IndexOf(headers, "throttle_pct");
        var rawThrottleIndex = Array.IndexOf(headers, "raw_throttle");
        var brakeIndex = Array.IndexOf(headers, "brake_pct");
        var rawBrakeIndex = Array.IndexOf(headers, "raw_brake");
        var gearIndex = Array.IndexOf(headers, "gear");
        var drsIndex = Array.IndexOf(headers, "drs_enabled");

        var items = new List<(int SampleIndex, TelemetryPointDto Point)>();
        for (var index = 1; index < lines.Length; index++)
        {
            var columns = lines[index].Split(',');
            items.Add((
                ParseInt(columns, sampleIndex),
                new TelemetryPointDto(
                    0d,
                    timeIndex >= 0 && timeIndex < columns.Length ? columns[timeIndex] : string.Empty,
                    ParseInt(columns, speedIndex),
                    ParseInt(columns, throttleIndex),
                    ParseDouble(columns, brakeIndex),
                    ParseInt(columns, gearIndex),
                    ParseBool(columns, drsIndex),
                    ParseInt(columns, rpmIndex),
                    ParseInt(columns, sampleIndex),
                    ParseInt(columns, rawThrottleIndex),
                    ParseInt(columns, rawBrakeIndex))));
        }

        items = items
            .OrderBy(item => item.SampleIndex)
            .ThenBy(item => item.Point.Timestamp, StringComparer.Ordinal)
            .ToList();

        if (items.Count == 1)
        {
            return [items[0].Point with { ProgressPct = 100d }];
        }

        return items.Select((item, index) => item.Point with
        {
            ProgressPct = index / (double)Math.Max(1, items.Count - 1) * 100d
        }).ToArray();
    }

    public static IReadOnlyList<TelemetrySampleDto> ParseTelemetrySamples(string csv)
    {
        var lines = csv
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#'))
            .ToArray();

        if (lines.Length <= 1)
        {
            return [];
        }

        var headers = lines[0].Split(',');
        var sessionIdIndex = Array.IndexOf(headers, "session_id");
        var driverNumberIndex = Array.IndexOf(headers, "driver_number");
        var lapNumberIndex = Array.IndexOf(headers, "lap_number");
        var stintNumberIndex = Array.IndexOf(headers, "stint_number");
        var timeIndex = Array.IndexOf(headers, "_time");
        var sampleIndex = Array.IndexOf(headers, "sample_index");
        var speedIndex = Array.IndexOf(headers, "speed");
        var rpmIndex = Array.IndexOf(headers, "rpm");
        var throttleIndex = Array.IndexOf(headers, "throttle_pct");
        var rawThrottleIndex = Array.IndexOf(headers, "raw_throttle");
        var brakeIndex = Array.IndexOf(headers, "brake_pct");
        var rawBrakeIndex = Array.IndexOf(headers, "raw_brake");
        var gearIndex = Array.IndexOf(headers, "gear");
        var drsIndex = Array.IndexOf(headers, "drs_enabled");

        var items = new List<TelemetrySampleDto>(Math.Max(0, lines.Length - 1));
        for (var index = 1; index < lines.Length; index++)
        {
            var columns = lines[index].Split(',');
            items.Add(new TelemetrySampleDto(
                ParseString(columns, sessionIdIndex),
                ParseInt(columns, driverNumberIndex),
                ParseInt(columns, lapNumberIndex),
                ParseInt(columns, stintNumberIndex),
                ParseInt(columns, sampleIndex),
                ParseString(columns, timeIndex),
                ParseNullableInt(columns, speedIndex),
                ParseNullableInt(columns, rpmIndex),
                ParseNullableInt(columns, throttleIndex),
                ParseNullableInt(columns, rawThrottleIndex),
                ParseNullableDouble(columns, brakeIndex),
                ParseNullableInt(columns, rawBrakeIndex),
                ParseNullableInt(columns, gearIndex),
                ParseNullableBool(columns, drsIndex)));
        }

        return items
            .OrderBy(item => item.DriverNumber)
            .ThenBy(item => item.SampleIndex)
            .ThenBy(item => item.Timestamp, StringComparer.Ordinal)
            .ToArray();
    }

    private static int ParseInt(string[] columns, int index)
        => index >= 0 && index < columns.Length && int.TryParse(columns[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static int? ParseNullableInt(string[] columns, int index)
        => index >= 0 && index < columns.Length && int.TryParse(columns[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static double ParseDouble(string[] columns, int index)
        => index >= 0 && index < columns.Length && double.TryParse(columns[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0d;

    private static double? ParseNullableDouble(string[] columns, int index)
        => index >= 0 && index < columns.Length && double.TryParse(columns[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static bool ParseBool(string[] columns, int index)
        => index >= 0 && index < columns.Length && bool.TryParse(columns[index], out var parsed) && parsed;

    private static bool? ParseNullableBool(string[] columns, int index)
        => index >= 0 && index < columns.Length && bool.TryParse(columns[index], out var parsed)
            ? parsed
            : null;

    private static string ParseString(string[] columns, int index)
        => index >= 0 && index < columns.Length ? columns[index] : string.Empty;
}
