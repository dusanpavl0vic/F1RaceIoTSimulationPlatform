namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record DriverLapSeriesDto(
    int DriverNumber,
    string DriverName,
    int LapNumber,
    IReadOnlyList<TelemetryPointDto> Telemetry);
