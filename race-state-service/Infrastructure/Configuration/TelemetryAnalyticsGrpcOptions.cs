namespace F1.RaceState.Service.Infrastructure.Configuration;

public sealed class TelemetryAnalyticsGrpcOptions
{
    public const string SectionName = "TelemetryAnalyticsGrpc";

    public string Endpoint { get; set; } = "http://localhost:8083";
}
