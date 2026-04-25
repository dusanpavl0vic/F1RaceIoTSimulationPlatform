using System.Net.Http.Headers;
using System.Text;
using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using F1.TelemetryAnalytics.Service.Infrastructure.Persistence.Parsing;
using Microsoft.Extensions.Options;

namespace F1.TelemetryAnalytics.Service.Infrastructure.Persistence;

public sealed class InfluxTelemetryClient(
    HttpClient httpClient,
    IOptions<InfluxDbOptions> influxDbOptions,
    ILogger<InfluxTelemetryClient> logger) : IInfluxTelemetryClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly InfluxDbOptions _options = influxDbOptions.Value;
    private readonly ILogger<InfluxTelemetryClient> _logger = logger;

    public async Task<IReadOnlyList<TelemetrySampleDto>> QueryDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string> requestedMetrics,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return [];
        }

        var selectedFields = TelemetryMetricCatalog.ResolveInfluxFields(requestedMetrics);
        var flux = BuildTelemetrySamplesFlux(
            sessionId,
            $"|> filter(fn: (r) => r.driver_number == \"{driverNumber}\")\n  |> filter(fn: (r) => r.lap_number == \"{lapNumber}\")",
            selectedFields);

        var payload = await QueryTelemetryCsvAsync(flux, cancellationToken);
        return InfluxTelemetryCsvParser.ParseTelemetrySamples(payload);
    }

    private static string EscapeFluxString(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private string BuildTelemetrySamplesFlux(
        string sessionId,
        string driverFilter,
        IReadOnlyCollection<string> selectedFields)
    {
        var keepColumns = new[]
        {
            "session_id",
            "driver_number",
            "lap_number",
            "stint_number",
            "_time",
            "sample_index"
        }
        .Concat(selectedFields)
        .Distinct(StringComparer.Ordinal)
        .Select(field => $"\"{field}\"");

        return $$"""
            from(bucket: "{{_options.Bucket}}")
              |> range(start: 1970-01-01T00:00:00Z)
              |> filter(fn: (r) => r._measurement == "telemetry_samples")
              |> filter(fn: (r) => r.session_id == "{{EscapeFluxString(sessionId)}}")
              {{driverFilter}}
              |> pivot(rowKey: ["_time", "session_id", "driver_number", "lap_number", "stint_number"], columnKey: ["_field"], valueColumn: "_value")
              |> keep(columns: [{{string.Join(", ", keepColumns)}}])
              |> sort(columns: ["driver_number", "sample_index", "_time"])
            """;
    }

    private async Task<string> QueryTelemetryCsvAsync(string flux, CancellationToken cancellationToken)
    {
        using var response = await SendWithOptionalAuthRetryAsync(
            includeAuthorization =>
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_options.BaseUrl.TrimEnd('/')}/api/v2/query?org={Uri.EscapeDataString(_options.Organization)}")
                {
                    Content = new StringContent($"{{\"query\":{System.Text.Json.JsonSerializer.Serialize(flux)}}}", Encoding.UTF8, "application/json")
                };

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));
                ApplyAuthorization(request, includeAuthorization);
                return request;
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithOptionalAuthRetryAsync(
        Func<bool, HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var includeAuthorization = !string.IsNullOrWhiteSpace(_options.Token);
        var response = await SendAsync(requestFactory(includeAuthorization), cancellationToken);

        if (includeAuthorization && response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            response.Dispose();
            _logger.LogWarning("InfluxDB request rejected the configured token. Retrying without authorization header for local/dev compatibility.");
            response = await SendAsync(requestFactory(false), cancellationToken);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => await _httpClient.SendAsync(request, cancellationToken);

    private void ApplyAuthorization(HttpRequestMessage request, bool includeAuthorization)
    {
        if (includeAuthorization)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.Token);
        }
    }
}
