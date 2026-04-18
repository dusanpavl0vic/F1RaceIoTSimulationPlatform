using F1.TelemetryAnalytics.Service.Domain.Models;

namespace F1.TelemetryAnalytics.Service.Infrastructure.Persistence.Formatting;

internal static class InfluxLineProtocolFormatter
{
    public static string Format(TelemetrySampleRecord sample)
    {
        var fields = new List<string>();
        if (sample.Speed is int speed)
        {
            fields.Add($"speed={speed}i");
        }

        if (sample.Rpm is int rpm)
        {
            fields.Add($"rpm={rpm}i");
        }

        if (sample.Gear is int gear)
        {
            fields.Add($"gear={gear}i");
        }

        if (sample.ThrottlePct is int throttlePct)
        {
            fields.Add($"throttle_pct={throttlePct}i");
        }

        if (sample.RawThrottle is int rawThrottle)
        {
            fields.Add($"raw_throttle={rawThrottle}i");
        }

        if (sample.RawBrake is int rawBrake)
        {
            fields.Add($"brake_pct={rawBrake}i");
            fields.Add($"raw_brake={rawBrake}i");
        }
        else if (sample.BrakeApplied is bool brakeApplied)
        {
            fields.Add($"brake_pct={(brakeApplied ? 100 : 0)}i");
        }

        if (sample.DrsEnabled is bool drsEnabled)
        {
            fields.Add($"drs_enabled={(drsEnabled ? "true" : "false")}");
        }

        fields.Add($"sample_index={sample.SampleIndex}i");

        var tags = string.Join(',',
        [
            $"session_id={EscapeTag(sample.SessionId)}",
            $"driver_number={sample.DriverNumber}",
            $"lap_number={sample.LapNumber}",
            $"stint_number={sample.StintNumber}"
        ]);

        var timestamp = sample.Timestamp.ToUnixTimeMilliseconds() * 1_000_000L;
        return $"telemetry_samples,{tags} {string.Join(',', fields)} {timestamp}";
    }

    private static string EscapeTag(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(" ", "\\ ", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("=", "\\=", StringComparison.Ordinal);
}
