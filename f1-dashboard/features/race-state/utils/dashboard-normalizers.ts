import type {
  RaceDashboard,
  RaceDashboardDriverRow,
  RaceStateSessionView,
  SessionCard,
} from "@/features/race-state/types/race-state";
import type { RaceStateWebSocketStatus } from "@/features/race-state/store/race-state-ui-slice";

const resolveDriverLabel = (broadcastName: string | null, fullName: string | null, tla: string | null, driverNumber: number) =>
  tla || broadcastName || fullName || `#${driverNumber}`;

const resolveDisplayOrder = (position: number | null, line: number | null, gridPosition: number | null) =>
  position ?? gridPosition ?? line;

const normalizeTimingLabel = (value: string | null) => {
  if (
    !value ||
    value.trim().length === 0 ||
    value.trim().startsWith("{") ||
    value.trim().toUpperCase().startsWith("LAP ")
  ) {
    return "-";
  }

  return value;
};

const resolveGapToLeader = (position: number | null, line: number | null, gridPosition: number | null, gapToLeader: string | null) => {
  return resolveDisplayOrder(position, line, gridPosition) === 1 ? "leader" : normalizeTimingLabel(gapToLeader);
};

const resolvePitFlag = (inPit: boolean, pitOut: boolean) => {
  if (inPit) {
    return "IN";
  }

  if (pitOut) {
    return "OUT";
  }

  return "-";
};

const resolveStatusLabel = (
  status: number | null,
  inPit: boolean,
  pitOut: boolean,
  retired: boolean,
  stopped: boolean
) => {
  if (retired) {
    return "RET";
  }

  if (stopped) {
    return "STOP";
  }

  if (inPit) {
    return "PIT";
  }

  if (pitOut) {
    return "OUT";
  }

  if (status === 80) {
    return "PIT";
  }

  if (status === 96 || status === 608) {
    return "OUT";
  }

  if (status === 92) {
    return "RET";
  }

  if (status === 64) {
    return "RUN";
  }

  return "-";
};

export const buildRaceDashboardRows = (
  dashboard: RaceDashboard
): RaceDashboardDriverRow[] => {
  return dashboard.leaderboard
    .map((entry) => {
      const driverLabel = resolveDriverLabel(
        entry.broadcastName,
        entry.fullName,
        entry.tla,
        entry.driverNumber
      );

      const row: RaceDashboardDriverRow = {
        ...entry,
        driverLabel,
        displayTeamName: entry.teamName ?? "-",
        gapToLeader: resolveGapToLeader(entry.position, entry.line, entry.gridPosition, entry.gapToLeader),
        intervalToPositionAhead: entry.position === 1 ? "-" : normalizeTimingLabel(entry.intervalToPositionAhead),
        lastLapTime: normalizeTimingLabel(entry.lastLapTime),
        bestLapTime: normalizeTimingLabel(entry.bestLapTime),
        tyreCompound: entry.tyreCompound ?? "-",
        pitFlag: resolvePitFlag(entry.inPit, entry.pitOut),
        statusLabel: resolveStatusLabel(entry.status, entry.inPit, entry.pitOut, entry.retired, entry.stopped),
      };

      return row;
    });
};

export const buildSessionCards = (
  session: RaceStateSessionView | null | undefined,
  wsStatus: RaceStateWebSocketStatus
): SessionCard[] => {
  if (!session) {
    return [];
  }

  return [
    {
      label: "Session",
      value: session.sessionId ?? "-",
    },
    {
      label: "Lap",
      value:
        session.currentLap && session.totalLaps
          ? `${session.currentLap}/${session.totalLaps}`
          : "-",
    },
    {
      label: "Track",
      value: session.trackStatusCode ?? "-",
    },
    {
      label: "WS",
      value: wsStatus,
    },
  ];
};
