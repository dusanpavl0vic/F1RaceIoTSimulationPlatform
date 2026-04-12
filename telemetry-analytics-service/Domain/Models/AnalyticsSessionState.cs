namespace F1.TelemetryAnalytics.Service.Domain.Models;

public sealed class AnalyticsSessionState
{
    public string SessionId { get; set; } = string.Empty;
    public string? MeetingName { get; set; }
    public string? SessionName { get; set; }
    public string? SessionStatus { get; set; }
    public string? TrackStatusCode { get; set; }
    public string? TrackStatusLabel { get; set; }
    public int? CurrentLap { get; set; }
    public int? TotalLaps { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Dictionary<int, AnalyticsDriverState> Drivers { get; } = [];
}
