using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
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

        var line = BuildLineProtocol(sample);
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
              |> keep(columns: ["_time", "sample_index", "speed", "throttle_pct", "brake_pct", "gear", "drs_enabled"])
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
        return ParseTelemetryCsv(payload);
    }

    private static string BuildLineProtocol(TelemetrySampleRecord sample)
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

        if (sample.BrakeApplied is bool brakeApplied)
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

    private static string EscapeFluxString(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

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

    private static IReadOnlyList<TelemetryPointDto> ParseTelemetryCsv(string csv)
    {
        var lines = csv
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#'))
            .ToArray();

        if (lines.Length <= 1)
        {
            return [];
        }

        var headers = lines[0].Split(',');
        var timeIndex = Array.IndexOf(headers, "_time");
        var sampleIndex = Array.IndexOf(headers, "sample_index");
        var speedIndex = Array.IndexOf(headers, "speed");
        var throttleIndex = Array.IndexOf(headers, "throttle_pct");
        var brakeIndex = Array.IndexOf(headers, "brake_pct");
        var gearIndex = Array.IndexOf(headers, "gear");
        var drsIndex = Array.IndexOf(headers, "drs_enabled");

        var items = new List<(int SampleIndex, TelemetryPointDto Point)>();
        for (var index = 1; index < lines.Length; index++)
        {
            var columns = lines[index].Split(',');
            items.Add((
                ParseInt(columns, sampleIndex),
                new TelemetryPointDto(
                    0d,
                    timeIndex >= 0 && timeIndex < columns.Length ? columns[timeIndex] : string.Empty,
                    ParseInt(columns, speedIndex),
                    ParseInt(columns, throttleIndex),
                    ParseDouble(columns, brakeIndex),
                    ParseInt(columns, gearIndex),
                    ParseBool(columns, drsIndex))));
        }

        items = items
            .OrderBy(item => item.SampleIndex)
            .ThenBy(item => item.Point.Timestamp, StringComparer.Ordinal)
            .ToList();

        if (items.Count == 1)
        {
            return [items[0].Point with { ProgressPct = 100d }];
        }

        return items.Select((item, index) => item.Point with
        {
            ProgressPct = index / (double)Math.Max(1, items.Count - 1) * 100d
        }).ToArray();
    }

    private static int ParseInt(string[] columns, int index)
        => index >= 0 && index < columns.Length && int.TryParse(columns[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static double ParseDouble(string[] columns, int index)
        => index >= 0 && index < columns.Length && double.TryParse(columns[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0d;

    private static bool ParseBool(string[] columns, int index)
        => index >= 0 && index < columns.Length && bool.TryParse(columns[index], out var parsed) && parsed;
}
