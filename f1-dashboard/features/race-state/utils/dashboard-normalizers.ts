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
  position ?? line ?? gridPosition;

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

const resolveStatusLabel = (status: number | null, retired: boolean, stopped: boolean) => {
  if (retired) {
    return "RET";
  }

  if (stopped) {
    return "STOP";
  }

  if (status === 80) {
    return "PIT";
  }

  if (status === 64) {
    return "RUN";
  }

  return status?.toString() ?? "-";
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
        statusLabel: resolveStatusLabel(entry.status, entry.retired, entry.stopped),
      };

      return row;
    })
    .sort((left, right) => {
      const leftInactive = left.retired || left.stopped ? 1 : 0;
      const rightInactive = right.retired || right.stopped ? 1 : 0;

      if (leftInactive !== rightInactive) {
        return leftInactive - rightInactive;
      }

      const leftPosition = left.position ?? Number.MAX_SAFE_INTEGER;
      const rightPosition = right.position ?? Number.MAX_SAFE_INTEGER;
      const leftLine = left.line ?? Number.MAX_SAFE_INTEGER;
      const rightLine = right.line ?? Number.MAX_SAFE_INTEGER;

      if (leftPosition !== rightPosition) {
        return leftPosition - rightPosition;
      }

      if (leftLine !== rightLine) {
        return leftLine - rightLine;
      }

      return left.driverNumber - right.driverNumber;
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
