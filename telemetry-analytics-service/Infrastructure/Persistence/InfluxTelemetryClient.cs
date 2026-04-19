using System.Net.Http.Headers;
using System.Text;
using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using F1.TelemetryAnalytics.Service.Infrastructure.Persistence.Formatting;
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

    public async Task WriteTelemetrySampleAsync(TelemetrySampleRecord sample, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var line = InfluxLineProtocolFormatter.Format(sample);
        using var response = await SendWithOptionalAuthRetryAsync(
            includeAuthorization =>
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_options.BaseUrl.TrimEnd('/')}/api/v2/write?org={Uri.EscapeDataString(_options.Organization)}&bucket={Uri.EscapeDataString(_options.Bucket)}&precision={Uri.EscapeDataString(_options.WritePrecision)}")
                {
                    Content = new StringContent(line, Encoding.UTF8, "text/plain")
                };

                ApplyAuthorization(request, includeAuthorization);
                return request;
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("InfluxDB write failed with status {StatusCode}. body={Body}", response.StatusCode, body);
        }
    }

    public async Task<IReadOnlyList<TelemetryPointDto>> QueryLapTelemetryAsync(string sessionId, int driverNumber, int lapNumber, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return [];
        }

        var flux = $$"""
            from(bucket: "{{_options.Bucket}}")
              |> range(start: 1970-01-01T00:00:00Z)
              |> filter(fn: (r) => r._measurement == "telemetry_samples")
              |> filter(fn: (r) => r.session_id == "{{EscapeFluxString(sessionId)}}")
              |> filter(fn: (r) => r.driver_number == "{{driverNumber}}")
              |> filter(fn: (r) => r.lap_number == "{{lapNumber}}")
              |> pivot(rowKey: ["_time"], columnKey: ["_field"], valueColumn: "_value")
              |> keep(columns: ["_time", "sample_index", "speed", "rpm", "throttle_pct", "raw_throttle", "brake_pct", "raw_brake", "gear", "drs_enabled"])
              |> sort(columns: ["sample_index", "_time"])
            """;

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
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return InfluxTelemetryCsvParser.Parse(payload);
    }

    public async Task<IReadOnlyList<TelemetrySampleDto>> QueryDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        DateTimeOffset? sinceTimestamp,
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
            $"|> filter(fn: (r) => r.driver_number == \"{driverNumber}\")",
            maxSamples,
            sinceTimestamp,
            selectedFields);

        var payload = await QueryTelemetryCsvAsync(flux, cancellationToken);
        return InfluxTelemetryCsvParser.ParseTelemetrySamples(payload);
    }

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
            maxSamples: 0,
            sinceTimestamp: null,
            selectedFields);

        var payload = await QueryTelemetryCsvAsync(flux, cancellationToken);
        return InfluxTelemetryCsvParser.ParseTelemetrySamples(payload);
    }

    private static string EscapeFluxString(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private string BuildTelemetrySamplesFlux(
        string sessionId,
        string driverFilter,
        int maxSamples,
        DateTimeOffset? sinceTimestamp,
        IReadOnlyCollection<string> selectedFields)
    {
        var rangeStart = sinceTimestamp?.ToUniversalTime().ToString("O") ?? "1970-01-01T00:00:00Z";
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
        var limitClause = maxSamples > 0
            ? $$"""
              |> sort(columns: ["sample_index", "_time"], desc: true)
              |> limit(n: {{maxSamples}})
              |> sort(columns: ["driver_number", "sample_index", "_time"])
              """
            : """
              |> sort(columns: ["driver_number", "sample_index", "_time"])
              """;

        return $$"""
            from(bucket: "{{_options.Bucket}}")
              |> range(start: {{rangeStart}})
              |> filter(fn: (r) => r._measurement == "telemetry_samples")
              |> filter(fn: (r) => r.session_id == "{{EscapeFluxString(sessionId)}}")
              {{driverFilter}}
              |> pivot(rowKey: ["_time", "session_id", "driver_number", "lap_number", "stint_number"], columnKey: ["_field"], valueColumn: "_value")
              |> keep(columns: [{{string.Join(", ", keepColumns)}}])
              {{limitClause}}
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
