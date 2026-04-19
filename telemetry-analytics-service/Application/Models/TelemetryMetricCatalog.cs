namespace F1.TelemetryAnalytics.Service.Application.Models;

internal static class TelemetryMetricCatalog
{
    private static readonly string[] SupportedMetrics =
    [
        "speed",
        "rpm",
        "throttlePct",
        "rawThrottle",
        "brakePct",
        "rawBrake",
        "gear",
        "drsEnabled"
    ];

    private static readonly Dictionary<string, string> InfluxFieldByMetric = new(StringComparer.OrdinalIgnoreCase)
    {
        ["speed"] = "speed",
        ["rpm"] = "rpm",
        ["throttlePct"] = "throttle_pct",
        ["rawThrottle"] = "raw_throttle",
        ["brakePct"] = "brake_pct",
        ["rawBrake"] = "raw_brake",
        ["gear"] = "gear",
        ["drsEnabled"] = "drs_enabled"
    };

    public static IReadOnlyList<string> NormalizeRequestedMetrics(IEnumerable<string>? requestedMetrics)
    {
        var normalized = (requestedMetrics ?? [])
            .Where(metric => !string.IsNullOrWhiteSpace(metric))
            .Select(metric => metric.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            return SupportedMetrics;
        }

        var invalid = normalized
            .Where(metric => !InfluxFieldByMetric.ContainsKey(metric))
            .OrderBy(metric => metric, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (invalid.Length > 0)
        {
            throw new ArgumentException(
                $"Unsupported telemetry metrics: {string.Join(", ", invalid)}. Supported metrics: {string.Join(", ", SupportedMetrics)}.",
                nameof(requestedMetrics));
        }

        return SupportedMetrics
            .Where(metric => normalized.Contains(metric, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    public static IReadOnlyList<string> ResolveInfluxFields(IEnumerable<string> metrics)
        => metrics
            .Where(metric => InfluxFieldByMetric.ContainsKey(metric))
            .Select(metric => InfluxFieldByMetric[metric])
            .ToArray();
}
