using System.Data;
using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace F1.TelemetryAnalytics.Service.Infrastructure.Persistence;

public sealed class PostgresAnalyticsRepository(
    IOptions<PostgresOptions> postgresOptions,
    ILogger<PostgresAnalyticsRepository> logger) : IAnalyticsRepository
{
    private readonly string[] _connectionStrings = BuildCandidateConnectionStrings(postgresOptions.Value.ConnectionString);
    private readonly ILogger<PostgresAnalyticsRepository> _logger = logger;
    private readonly object _connectionGate = new();
    private string? _resolvedConnectionString;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        const string sql = """
            drop table if exists analytics_lap_summaries;

            create table if not exists analytics_sessions (
                session_id text primary key,
                meeting_name text null,
                session_name text null,
                session_status text null,
                track_status_code text null,
                track_status_label text null,
                current_lap integer null,
                total_laps integer null,
                updated_at timestamptz not null
            );

            create table if not exists analytics_drivers (
                session_id text not null,
                driver_number integer not null,
                abbreviation text null,
                broadcast_name text null,
                full_name text null,
                team_name text null,
                team_color text null,
                grid_position integer null,
                created_at timestamptz not null default now(),
                updated_at timestamptz not null,
                primary key (session_id, driver_number)
            );

            create table if not exists analytics_stint_summaries (
                session_id text not null,
                driver_number integer not null,
                stint_number integer not null,
                compound text null,
                tyre_is_new boolean null,
                start_lap integer null,
                end_lap integer null,
                lap_count integer null,
                created_at timestamptz not null default now(),
                updated_at timestamptz not null,
                primary key (session_id, driver_number, stint_number)
            );

            alter table analytics_drivers add column if not exists grid_position integer null;
            alter table analytics_drivers add column if not exists created_at timestamptz not null default now();
            alter table analytics_drivers add column if not exists updated_at timestamptz not null default now();
            alter table analytics_stint_summaries add column if not exists created_at timestamptz not null default now();

            delete from analytics_drivers d
            where not exists (
                select 1
                from analytics_sessions s
                where s.session_id = d.session_id
            );

            delete from analytics_stint_summaries st
            where not exists (
                select 1
                from analytics_drivers d
                where d.session_id = st.session_id
                  and d.driver_number = st.driver_number
            );

            do $$
            begin
              if not exists (
                  select 1
                  from pg_constraint
                  where conname = 'fk_analytics_drivers_session'
              ) then
                alter table analytics_drivers
                  add constraint fk_analytics_drivers_session
                  foreign key (session_id)
                  references analytics_sessions(session_id)
                  on delete cascade;
              end if;
            end
            $$;

            do $$
            begin
              if not exists (
                  select 1
                  from pg_constraint
                  where conname = 'fk_analytics_stints_driver'
              ) then
                alter table analytics_stint_summaries
                  add constraint fk_analytics_stints_driver
                  foreign key (session_id, driver_number)
                  references analytics_drivers(session_id, driver_number)
                  on delete cascade;
              end if;
            end
            $$;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Telemetry analytics PostgreSQL schema initialized.");
    }

    public async Task<TyreStintStrategyDto> GetTyreStintStrategyAsync(string sessionId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                coalesce(sess.total_laps, 0) as total_laps,
                d.driver_number,
                coalesce(d.abbreviation, d.broadcast_name, d.full_name, '#' || d.driver_number::text) as driver_name,
                d.team_name,
                d.team_color,
                d.grid_position,
                null::integer as position,
                s.stint_number,
                s.compound,
                s.tyre_is_new,
                s.start_lap,
                s.end_lap,
                s.lap_count
            from analytics_drivers d
            left join analytics_sessions sess
                on sess.session_id = d.session_id
            left join analytics_stint_summaries s
                on s.session_id = d.session_id
               and s.driver_number = d.driver_number
            where d.session_id = @session_id
            order by coalesce(d.grid_position, 999), d.driver_number, coalesce(s.stint_number, 999);
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);

        var drivers = new Dictionary<int, TyreStrategyDriverBuilder>();
        var totalLaps = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            totalLaps = Math.Max(totalLaps, reader.GetInt32(0));

            var driverNumber = reader.GetInt32(1);
            if (!drivers.TryGetValue(driverNumber, out var driver))
            {
                driver = new TyreStrategyDriverBuilder(
                    driverNumber,
                    reader.GetString(2),
                    ReadNullableString(reader, 3),
                    ReadNullableString(reader, 4),
                    ReadNullableInt(reader, 5),
                    ReadNullableInt(reader, 6));
                drivers[driverNumber] = driver;
            }

            if (!reader.IsDBNull(7))
            {
                driver.AppendStint(
                    reader.GetInt32(7),
                    ReadNullableString(reader, 8),
                    reader.IsDBNull(9) ? null : reader.GetBoolean(9),
                    ReadNullableInt(reader, 10),
                    ReadNullableInt(reader, 11),
                    ReadNullableInt(reader, 12));
            }
        }

        var responseDrivers = drivers.Values
            .Select(driver => driver.ToDto())
            .ToArray();

        var inferredTotalLaps = responseDrivers
            .SelectMany(driver => driver.Stints)
            .Select(stint => stint.EndLap ?? (stint.StartLap + stint.LapCount - 1) ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        return new TyreStintStrategyDto(
            sessionId,
            Math.Max(totalLaps, inferredTotalLaps),
            responseDrivers);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var resolvedConnectionString = _resolvedConnectionString;
        if (!string.IsNullOrWhiteSpace(resolvedConnectionString))
        {
            return await OpenConnectionAsync(resolvedConnectionString, cancellationToken);
        }

        var failures = new List<string>();
        foreach (var candidate in _connectionStrings)
        {
            try
            {
                var connection = await OpenConnectionAsync(candidate, cancellationToken);
                lock (_connectionGate)
                {
                    _resolvedConnectionString ??= candidate;
                }

                return connection;
            }
            catch (Exception exception)
            {
                failures.Add(exception.Message);
            }
        }

        throw new InvalidOperationException(
            $"Unable to connect to PostgreSQL using the available analytics connection settings. Tried {_connectionStrings.Length} candidate connection string(s). Last errors: {string.Join(" | ", failures.TakeLast(3))}");
    }

    private static string? ReadNullableString(IDataRecord record, int index)
        => record.IsDBNull(index) ? null : record.GetString(index);

    private static int? ReadNullableInt(IDataRecord record, int index)
        => record.IsDBNull(index) ? null : record.GetInt32(index);

    private sealed class TyreStrategyDriverBuilder(
        int driverNumber,
        string driverName,
        string? teamName,
        string? teamColor,
        int? gridPosition,
        int? position)
    {
        public int DriverNumber { get; } = driverNumber;
        public string DriverName { get; } = driverName;
        public string? TeamName { get; } = teamName;
        public string? TeamColor { get; } = teamColor;
        public int? GridPosition { get; } = gridPosition;
        public int? Position { get; } = position;
        public List<TyreStintDto> Stints { get; } = [];

        public void AppendStint(
            int stintNumber,
            string? compound,
            bool? tyreIsNew,
            int? startLap,
            int? endLap,
            int? lapCount)
        {
            if (stintNumber <= 0)
            {
                return;
            }

            var normalizedStartLap = startLap is > 0 ? startLap.Value : 1;
            var normalizedLapCount = lapCount is > 0
                ? lapCount.Value
                : endLap is > 0
                    ? Math.Max(1, endLap.Value - normalizedStartLap + 1)
                    : 1;
            var normalizedEndLap = endLap is > 0
                ? endLap.Value
                : normalizedStartLap + normalizedLapCount - 1;

            Stints.Add(new TyreStintDto(
                stintNumber,
                compound,
                tyreIsNew,
                normalizedStartLap,
                normalizedEndLap,
                normalizedLapCount));
        }

        public TyreStintDriverDto ToDto()
            => new(
                DriverNumber,
                DriverName,
                TeamName,
                TeamColor,
                GridPosition,
                Position,
                Stints
                    .OrderBy(stint => stint.StintNumber)
                    .ThenBy(stint => stint.StartLap ?? int.MaxValue)
                    .ToArray());
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string[] BuildCandidateConnectionStrings(string configuredConnectionString)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            candidates.Add(configuredConnectionString);

            var configured = new NpgsqlConnectionStringBuilder(configuredConnectionString);
            candidates.Add(new NpgsqlConnectionStringBuilder(configuredConnectionString)
            {
                Password = string.Empty
            }.ConnectionString);

            candidates.Add(new NpgsqlConnectionStringBuilder(configuredConnectionString)
            {
                Username = "postgres",
                Password = string.Empty
            }.ConnectionString);

            candidates.Add(new NpgsqlConnectionStringBuilder(configuredConnectionString)
            {
                Username = "postgres",
                Password = "postgres"
            }.ConnectionString);

            candidates.Add(new NpgsqlConnectionStringBuilder(configuredConnectionString)
            {
                Username = "f1",
                Password = "f1_password"
            }.ConnectionString);
        }

        candidates.Add("Host=postgres;Port=5432;Database=f1_telemetry;Username=f1;Password=f1_password");
        candidates.Add("Host=postgres;Port=5432;Database=f1_telemetry;Username=postgres;Password=postgres");
        candidates.Add("Host=postgres;Port=5432;Database=f1_telemetry;Username=postgres");

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
