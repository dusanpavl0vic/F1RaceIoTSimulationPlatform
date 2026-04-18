using System.Data;
using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using F1.TelemetryAnalytics.Service.Infrastructure.Persistence.Mappers;
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
                primary key (session_id, driver_number)
            );

            create table if not exists analytics_lap_summaries (
                session_id text not null,
                driver_number integer not null,
                lap_number integer not null,
                position integer null,
                gap_to_leader text null,
                interval_to_ahead text null,
                lap_time_label text null,
                lap_time_ms integer null,
                best_lap_time_label text null,
                best_lap_time_ms integer null,
                sector1_label text null,
                sector1_ms integer null,
                sector2_label text null,
                sector2_ms integer null,
                sector3_label text null,
                sector3_ms integer null,
                compound text null,
                tyre_is_new boolean null,
                tyre_laps integer null,
                stint_number integer not null,
                avg_speed double precision not null,
                max_speed integer not null,
                throttle_pct double precision not null,
                brake_pct double precision not null,
                drs_pct double precision not null,
                updated_at timestamptz not null,
                primary key (session_id, driver_number, lap_number)
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
                updated_at timestamptz not null,
                primary key (session_id, driver_number, stint_number)
            );

            alter table analytics_drivers add column if not exists grid_position integer null;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Telemetry analytics PostgreSQL schema initialized.");
    }

    public async Task UpsertSessionAsync(AnalyticsSessionState session, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into analytics_sessions (
                session_id, meeting_name, session_name, session_status, track_status_code, track_status_label, current_lap, total_laps, updated_at
            ) values (
                @session_id, @meeting_name, @session_name, @session_status, @track_status_code, @track_status_label, @current_lap, @total_laps, @updated_at
            )
            on conflict (session_id) do update set
                meeting_name = excluded.meeting_name,
                session_name = excluded.session_name,
                session_status = excluded.session_status,
                track_status_code = excluded.track_status_code,
                track_status_label = excluded.track_status_label,
                current_lap = excluded.current_lap,
                total_laps = excluded.total_laps,
                updated_at = excluded.updated_at;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", session.SessionId);
        command.Parameters.AddWithValue("meeting_name", (object?)session.MeetingName ?? DBNull.Value);
        command.Parameters.AddWithValue("session_name", (object?)session.SessionName ?? DBNull.Value);
        command.Parameters.AddWithValue("session_status", (object?)session.SessionStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("track_status_code", (object?)session.TrackStatusCode ?? DBNull.Value);
        command.Parameters.AddWithValue("track_status_label", (object?)session.TrackStatusLabel ?? DBNull.Value);
        command.Parameters.AddWithValue("current_lap", (object?)session.CurrentLap ?? DBNull.Value);
        command.Parameters.AddWithValue("total_laps", (object?)session.TotalLaps ?? DBNull.Value);
        command.Parameters.AddWithValue("updated_at", session.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertDriverAsync(string sessionId, AnalyticsDriverState driver, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into analytics_drivers (
                session_id, driver_number, abbreviation, broadcast_name, full_name, team_name, team_color, grid_position
            ) values (
                @session_id, @driver_number, @abbreviation, @broadcast_name, @full_name, @team_name, @team_color, @grid_position
            )
            on conflict (session_id, driver_number) do update set
                abbreviation = excluded.abbreviation,
                broadcast_name = excluded.broadcast_name,
                full_name = excluded.full_name,
                team_name = excluded.team_name,
                team_color = excluded.team_color,
                grid_position = excluded.grid_position;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);
        command.Parameters.AddWithValue("driver_number", driver.DriverNumber);
        command.Parameters.AddWithValue("abbreviation", (object?)driver.Tla ?? DBNull.Value);
        command.Parameters.AddWithValue("broadcast_name", (object?)driver.BroadcastName ?? DBNull.Value);
        command.Parameters.AddWithValue("full_name", (object?)driver.FullName ?? DBNull.Value);
        command.Parameters.AddWithValue("team_name", (object?)driver.TeamName ?? DBNull.Value);
        command.Parameters.AddWithValue("team_color", (object?)driver.TeamColor ?? DBNull.Value);
        command.Parameters.AddWithValue("grid_position", (object?)driver.GridPosition ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertLapSummaryAsync(LapSummaryRecord lapSummary, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into analytics_lap_summaries (
                session_id, driver_number, lap_number, position, gap_to_leader, interval_to_ahead, lap_time_label, lap_time_ms,
                best_lap_time_label, best_lap_time_ms, sector1_label, sector1_ms, sector2_label, sector2_ms, sector3_label, sector3_ms,
                compound, tyre_is_new, tyre_laps, stint_number, avg_speed, max_speed, throttle_pct, brake_pct, drs_pct, updated_at
            ) values (
                @session_id, @driver_number, @lap_number, @position, @gap_to_leader, @interval_to_ahead, @lap_time_label, @lap_time_ms,
                @best_lap_time_label, @best_lap_time_ms, @sector1_label, @sector1_ms, @sector2_label, @sector2_ms, @sector3_label, @sector3_ms,
                @compound, @tyre_is_new, @tyre_laps, @stint_number, @avg_speed, @max_speed, @throttle_pct, @brake_pct, @drs_pct, @updated_at
            )
            on conflict (session_id, driver_number, lap_number) do update set
                position = excluded.position,
                gap_to_leader = excluded.gap_to_leader,
                interval_to_ahead = excluded.interval_to_ahead,
                lap_time_label = excluded.lap_time_label,
                lap_time_ms = excluded.lap_time_ms,
                best_lap_time_label = excluded.best_lap_time_label,
                best_lap_time_ms = excluded.best_lap_time_ms,
                sector1_label = excluded.sector1_label,
                sector1_ms = excluded.sector1_ms,
                sector2_label = excluded.sector2_label,
                sector2_ms = excluded.sector2_ms,
                sector3_label = excluded.sector3_label,
                sector3_ms = excluded.sector3_ms,
                compound = excluded.compound,
                tyre_is_new = excluded.tyre_is_new,
                tyre_laps = excluded.tyre_laps,
                stint_number = excluded.stint_number,
                avg_speed = excluded.avg_speed,
                max_speed = excluded.max_speed,
                throttle_pct = excluded.throttle_pct,
                brake_pct = excluded.brake_pct,
                drs_pct = excluded.drs_pct,
                updated_at = excluded.updated_at;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", lapSummary.SessionId);
        command.Parameters.AddWithValue("driver_number", lapSummary.DriverNumber);
        command.Parameters.AddWithValue("lap_number", lapSummary.LapNumber);
        command.Parameters.AddWithValue("position", (object?)lapSummary.Position ?? DBNull.Value);
        command.Parameters.AddWithValue("gap_to_leader", (object?)lapSummary.GapToLeader ?? DBNull.Value);
        command.Parameters.AddWithValue("interval_to_ahead", (object?)lapSummary.IntervalToAhead ?? DBNull.Value);
        command.Parameters.AddWithValue("lap_time_label", (object?)lapSummary.LapTimeLabel ?? DBNull.Value);
        command.Parameters.AddWithValue("lap_time_ms", (object?)lapSummary.LapTimeMs ?? DBNull.Value);
        command.Parameters.AddWithValue("best_lap_time_label", (object?)lapSummary.BestLapTimeLabel ?? DBNull.Value);
        command.Parameters.AddWithValue("best_lap_time_ms", (object?)lapSummary.BestLapTimeMs ?? DBNull.Value);
        command.Parameters.AddWithValue("sector1_label", (object?)lapSummary.Sector1Label ?? DBNull.Value);
        command.Parameters.AddWithValue("sector1_ms", (object?)lapSummary.Sector1Ms ?? DBNull.Value);
        command.Parameters.AddWithValue("sector2_label", (object?)lapSummary.Sector2Label ?? DBNull.Value);
        command.Parameters.AddWithValue("sector2_ms", (object?)lapSummary.Sector2Ms ?? DBNull.Value);
        command.Parameters.AddWithValue("sector3_label", (object?)lapSummary.Sector3Label ?? DBNull.Value);
        command.Parameters.AddWithValue("sector3_ms", (object?)lapSummary.Sector3Ms ?? DBNull.Value);
        command.Parameters.AddWithValue("compound", (object?)lapSummary.Compound ?? DBNull.Value);
        command.Parameters.AddWithValue("tyre_is_new", (object?)lapSummary.TyreIsNew ?? DBNull.Value);
        command.Parameters.AddWithValue("tyre_laps", (object?)lapSummary.TyreLaps ?? DBNull.Value);
        command.Parameters.AddWithValue("stint_number", lapSummary.StintNumber);
        command.Parameters.AddWithValue("avg_speed", lapSummary.AverageSpeed);
        command.Parameters.AddWithValue("max_speed", lapSummary.MaxSpeed);
        command.Parameters.AddWithValue("throttle_pct", lapSummary.AverageThrottlePct);
        command.Parameters.AddWithValue("brake_pct", lapSummary.BrakeUsagePct);
        command.Parameters.AddWithValue("drs_pct", lapSummary.DrsUsagePct);
        command.Parameters.AddWithValue("updated_at", lapSummary.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertStintSummaryAsync(StintSummaryRecord stintSummary, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into analytics_stint_summaries (
                session_id, driver_number, stint_number, compound, tyre_is_new, start_lap, end_lap, lap_count, updated_at
            ) values (
                @session_id, @driver_number, @stint_number, @compound, @tyre_is_new, @start_lap, @end_lap, @lap_count, @updated_at
            )
            on conflict (session_id, driver_number, stint_number) do update set
                compound = excluded.compound,
                tyre_is_new = excluded.tyre_is_new,
                start_lap = excluded.start_lap,
                end_lap = excluded.end_lap,
                lap_count = excluded.lap_count,
                updated_at = excluded.updated_at;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", stintSummary.SessionId);
        command.Parameters.AddWithValue("driver_number", stintSummary.DriverNumber);
        command.Parameters.AddWithValue("stint_number", stintSummary.StintNumber);
        command.Parameters.AddWithValue("compound", (object?)stintSummary.Compound ?? DBNull.Value);
        command.Parameters.AddWithValue("tyre_is_new", (object?)stintSummary.TyreIsNew ?? DBNull.Value);
        command.Parameters.AddWithValue("start_lap", (object?)stintSummary.StartLap ?? DBNull.Value);
        command.Parameters.AddWithValue("end_lap", (object?)stintSummary.EndLap ?? DBNull.Value);
        command.Parameters.AddWithValue("lap_count", (object?)stintSummary.LapCount ?? DBNull.Value);
        command.Parameters.AddWithValue("updated_at", stintSummary.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SessionOverviewDto>> ListSessionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select
                s.session_id,
                s.meeting_name,
                s.session_name,
                s.session_status,
                s.track_status_code,
                s.track_status_label,
                s.current_lap,
                s.total_laps,
                coalesce(driver_counts.driver_count, 0) as driver_count,
                coalesce(lap_counts.completed_lap_count, 0) as completed_lap_count,
                s.updated_at
            from analytics_sessions s
            left join (
                select session_id, count(*) as driver_count
                from analytics_drivers
                group by session_id
            ) driver_counts on driver_counts.session_id = s.session_id
            left join (
                select session_id, count(*) as completed_lap_count
                from analytics_lap_summaries
                group by session_id
            ) lap_counts on lap_counts.session_id = s.session_id
            order by s.updated_at desc, s.session_id;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var sessions = new List<SessionOverviewDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sessions.Add(PostgresAnalyticsRecordMapper.MapSessionOverview(reader));
        }

        return sessions;
    }

    public async Task<SessionOverviewDto?> GetSessionOverviewAsync(string sessionId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                s.session_id,
                s.meeting_name,
                s.session_name,
                s.session_status,
                s.track_status_code,
                s.track_status_label,
                s.current_lap,
                s.total_laps,
                coalesce(driver_counts.driver_count, 0) as driver_count,
                coalesce(lap_counts.completed_lap_count, 0) as completed_lap_count,
                s.updated_at
            from analytics_sessions s
            left join (
                select session_id, count(*) as driver_count
                from analytics_drivers
                group by session_id
            ) driver_counts on driver_counts.session_id = s.session_id
            left join (
                select session_id, count(*) as completed_lap_count
                from analytics_lap_summaries
                group by session_id
            ) lap_counts on lap_counts.session_id = s.session_id
            where s.session_id = @session_id;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? PostgresAnalyticsRecordMapper.MapSessionOverview(reader)
            : null;
    }

    public async Task<IReadOnlyList<DriverSessionOverviewDto>> GetSessionDriversAsync(string sessionId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                d.driver_number,
                coalesce(d.abbreviation, d.broadcast_name, d.full_name, '#' || d.driver_number::text) as driver_name,
                d.team_name,
                d.team_color,
                d.grid_position,
                latest_lap.position,
                latest_lap.lap_number as completed_laps,
                latest_lap.best_lap_time_label,
                latest_lap.best_lap_time_ms,
                latest_lap.lap_time_label,
                latest_lap.lap_time_ms,
                latest_stint.compound,
                latest_stint.lap_count,
                latest_stint.stint_number,
                latest_lap.gap_to_leader,
                latest_lap.interval_to_ahead
            from analytics_drivers d
            left join lateral (
                select
                    lap_number,
                    position,
                    best_lap_time_label,
                    best_lap_time_ms,
                    lap_time_label,
                    lap_time_ms,
                    gap_to_leader,
                    interval_to_ahead
                from analytics_lap_summaries
                where session_id = d.session_id
                  and driver_number = d.driver_number
                order by lap_number desc
                limit 1
            ) latest_lap on true
            left join lateral (
                select
                    compound,
                    lap_count,
                    stint_number
                from analytics_stint_summaries
                where session_id = d.session_id
                  and driver_number = d.driver_number
                order by stint_number desc
                limit 1
            ) latest_stint on true
            where d.session_id = @session_id
            order by coalesce(latest_lap.position, 999), coalesce(d.grid_position, 999), d.driver_number;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var drivers = new List<DriverSessionOverviewDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            drivers.Add(PostgresAnalyticsRecordMapper.MapDriverSessionOverview(reader));
        }

        return drivers;
    }

    public async Task<(string DriverName, IReadOnlyList<DriverStintDto> Stints)> GetDriverStintsAsync(string sessionId, int driverNumber, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                coalesce(d.abbreviation, d.broadcast_name, d.full_name, '#' || @driver_number::text) as driver_name,
                s.stint_number,
                s.compound,
                s.tyre_is_new,
                s.start_lap,
                s.end_lap,
                s.lap_count,
                s.updated_at
            from analytics_stint_summaries s
            left join analytics_drivers d
                on d.session_id = s.session_id
               and d.driver_number = s.driver_number
            where s.session_id = @session_id
              and s.driver_number = @driver_number
            order by s.stint_number;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);
        command.Parameters.AddWithValue("driver_number", driverNumber);

        var stints = new List<DriverStintDto>();
        var driverName = $"#{driverNumber}";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            driverName = reader.GetString(0);
            stints.Add(PostgresAnalyticsRecordMapper.MapDriverStint(reader));
        }

        return (driverName, stints);
    }

    public async Task<(string DriverName, IReadOnlyList<DriverLapSummaryDto> Laps)> GetDriverLapSummariesAsync(string sessionId, int driverNumber, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                coalesce(d.abbreviation, d.broadcast_name, d.full_name, '#' || d.driver_number::text) as driver_name,
                l.lap_number,
                l.position,
                l.lap_time_label,
                l.lap_time_ms,
                l.best_lap_time_label,
                l.best_lap_time_ms,
                l.sector1_label,
                l.sector1_ms,
                l.sector2_label,
                l.sector2_ms,
                l.sector3_label,
                l.sector3_ms,
                l.compound,
                l.tyre_is_new,
                l.tyre_laps,
                l.stint_number,
                l.avg_speed,
                l.max_speed,
                l.throttle_pct,
                l.brake_pct,
                l.drs_pct
            from analytics_lap_summaries l
            left join analytics_drivers d
                on d.session_id = l.session_id
               and d.driver_number = l.driver_number
            where l.session_id = @session_id
              and l.driver_number = @driver_number
            order by l.lap_number;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("session_id", sessionId);
        command.Parameters.AddWithValue("driver_number", driverNumber);

        var laps = new List<DriverLapSummaryDto>();
        string driverName = $"#{driverNumber}";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            driverName = reader.GetString(0);
            laps.Add(PostgresAnalyticsRecordMapper.MapDriverLapSummary(reader));
        }

        return (driverName, laps);
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
        }

        candidates.Add("Host=postgres;Port=5432;Database=f1_telemetry;Username=postgres");
        candidates.Add("Host=postgres;Port=5432;Database=f1_telemetry;Username=postgres;Password=postgres");
        candidates.Add("Host=postgres;Port=5432;Database=f1_telemetry;Username=f1;Password=f1_password");

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

}
