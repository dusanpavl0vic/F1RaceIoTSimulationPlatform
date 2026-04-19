namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record DriverTelemetryQueryResultDto(
    IReadOnlyList<string> Metrics,
    IReadOnlyList<TelemetrySampleDto> Samples);
