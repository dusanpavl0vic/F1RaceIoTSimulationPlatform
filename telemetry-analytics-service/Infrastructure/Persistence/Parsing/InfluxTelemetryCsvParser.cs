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

    private static int ParseInt(string[] columns, int index)
        => index >= 0 && index < columns.Length && int.TryParse(columns[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static double ParseDouble(string[] columns, int index)
        => index >= 0 && index < columns.Length && double.TryParse(columns[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0d;

    private static bool ParseBool(string[] columns, int index)
        => index >= 0 && index < columns.Length && bool.TryParse(columns[index], out var parsed) && parsed;
}
