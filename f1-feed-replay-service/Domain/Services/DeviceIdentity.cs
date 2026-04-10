namespace F1.FeedReplay.Service.Domain.Services;

public static class DeviceIdentity
{
    public static string Resolve(string feedName, int? driverNumber)
        => feedName switch
        {
            "SessionInfo" or "DriverList" or "LapCount" => "session-control-unit",
            "TrackStatus" => "track-control-unit",
            "TimingData" or "TimingStats" => driverNumber is int driver ? $"timing-car-{driver}" : "timing-control-unit",
            "RaceControlMessages" => "race-control-unit",
            "CarData.z" => driverNumber is int telemetryDriver ? $"car-{telemetryDriver}" : "car-unknown",
            "Position.z" => driverNumber is int positionDriver ? $"position-car-{positionDriver}" : "position-tracker",
            "CurrentTyres" or "TyreStintSeries" => driverNumber is int tyreDriver ? $"tyre-sensor-car-{tyreDriver}" : "tyre-sensor-hub",
            "PitLaneTimeCollection" => driverNumber is int pitDriver ? $"pit-lane-sensor-{pitDriver}" : "pit-lane-sensor",
            _ => driverNumber is int genericDriver
                ? $"{Sanitize(feedName)}-device-{genericDriver}"
                : $"{Sanitize(feedName)}-device"
        };

    private static string Sanitize(string value)
        => value
            .Replace(".", "-", StringComparison.Ordinal)
            .Replace("_", "-", StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant();
}
