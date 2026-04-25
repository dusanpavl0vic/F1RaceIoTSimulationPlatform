const { Pool } = require("pg");

class PostgresAnalyticsRepository {
  constructor(connectionString) {
    this.pool = new Pool(buildPgConfig(connectionString));
  }

  async initialize() {
    const sql = `
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
    `;

    await this.pool.query(sql);
  }

  async upsertSession(session) {
    const sql = `
      insert into analytics_sessions (
          session_id, meeting_name, session_name, session_status, track_status_code, track_status_label, current_lap, total_laps, updated_at
      ) values (
          $1, $2, $3, $4, $5, $6, $7, $8, $9
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
    `;

    await this.pool.query(sql, [
      session.sessionId,
      session.meetingName,
      session.sessionName,
      session.sessionStatus,
      session.trackStatusCode,
      session.trackStatusLabel,
      session.currentLap,
      session.totalLaps,
      session.updatedAt
    ]);
  }

  async upsertDriver(sessionId, driver) {
    const sql = `
      insert into analytics_drivers (
          session_id, driver_number, abbreviation, broadcast_name, full_name, team_name, team_color, grid_position, updated_at
      ) values (
          $1, $2, $3, $4, $5, $6, $7, $8, $9
      )
      on conflict (session_id, driver_number) do update set
          abbreviation = excluded.abbreviation,
          broadcast_name = excluded.broadcast_name,
          full_name = excluded.full_name,
          team_name = excluded.team_name,
          team_color = excluded.team_color,
          grid_position = excluded.grid_position,
          updated_at = excluded.updated_at;
    `;

    await this.pool.query(sql, [
      sessionId,
      driver.driverNumber,
      driver.tla,
      driver.broadcastName,
      driver.fullName,
      driver.teamName,
      driver.teamColor,
      driver.gridPosition,
      driver.lastUpdateTimestamp ?? new Date()
    ]);
  }

  async upsertStintSummary(stintSummary) {
    const sql = `
      insert into analytics_stint_summaries (
          session_id, driver_number, stint_number, compound, tyre_is_new, start_lap, end_lap, lap_count, updated_at
      ) values (
          $1, $2, $3, $4, $5, $6, $7, $8, $9
      )
      on conflict (session_id, driver_number, stint_number) do update set
          compound = excluded.compound,
          tyre_is_new = excluded.tyre_is_new,
          start_lap = excluded.start_lap,
          end_lap = excluded.end_lap,
          lap_count = excluded.lap_count,
          updated_at = excluded.updated_at;
    `;

    await this.pool.query(sql, [
      stintSummary.sessionId,
      stintSummary.driverNumber,
      stintSummary.stintNumber,
      stintSummary.compound,
      stintSummary.tyreIsNew,
      stintSummary.startLap,
      stintSummary.endLap,
      stintSummary.lapCount,
      stintSummary.updatedAt
    ]);
  }

  async close() {
    await this.pool.end();
  }
}

const buildPgConfig = (connectionString) => {
  const values = parseSemicolonConnectionString(connectionString);
  return {
    host: values.host || "localhost",
    port: Number.parseInt(values.port || "5432", 10),
    database: values.database || "f1_telemetry",
    user: values.username || values.user || "f1",
    password: values.password || ""
  };
};

const parseSemicolonConnectionString = (connectionString) => {
  const result = {};

  for (const part of String(connectionString || "").split(";")) {
    const separatorIndex = part.indexOf("=");
    if (separatorIndex <= 0) {
      continue;
    }

    const key = part.slice(0, separatorIndex).trim().toLowerCase();
    const value = part.slice(separatorIndex + 1).trim();
    if (key) {
      result[key] = value;
    }
  }

  return result;
};

module.exports = {
  PostgresAnalyticsRepository
};
