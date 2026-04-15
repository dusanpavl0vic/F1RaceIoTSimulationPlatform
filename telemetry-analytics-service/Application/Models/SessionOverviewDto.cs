namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record SessionOverviewDto(
    string SessionId,
    string? MeetingName,
    string? SessionName,
    string? SessionStatus,
    string? TrackStatusCode,
    string? TrackStatusLabel,
    int? CurrentLap,
    int? TotalLaps,
    int DriverCount,
    int CompletedLapCount,
    DateTimeOffset UpdatedAt);
