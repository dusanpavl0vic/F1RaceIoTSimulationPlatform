import type {
  RaceDashboard,
  RaceDashboardDriverRow,
  RaceMapPosition,
  RaceStateSessionView,
  SessionCard,
} from "@/features/race-state/types/race-state";
import type { RaceStateWebSocketStatus } from "@/features/race-state/store/race-state-ui-slice";

const resolveDriverLabel = (broadcastName: string | null, fullName: string | null, tla: string | null, driverNumber: number) =>
  tla || broadcastName || fullName || `#${driverNumber}`;

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

  return status?.toString() ?? "-";
};

const toMapPosition = (row: RaceDashboardDriverRow): RaceMapPosition | null => {
  if (!row.currentTrackPosition) {
    return null;
  }

  return {
    driverNumber: row.driverNumber,
    driverName: row.driverLabel,
    position: row.gridPosition ?? row.line ?? row.position,
    status: row.currentTrackPosition.status ?? "-",
    x: row.currentTrackPosition.x ?? 0,
    y: row.currentTrackPosition.y ?? 0,
    z: row.currentTrackPosition.z ?? 0,
    timestamp: row.currentTrackPosition.timestamp ?? row.currentTrackPositionTimestamp,
    isEstimated: row.currentTrackPosition.isEstimated ?? false,
  };
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
        trackPosition: null,
        driverLabel,
        displayTeamName: entry.teamName ?? "-",
        pitFlag: resolvePitFlag(entry.inPit, entry.pitOut),
        statusLabel: resolveStatusLabel(entry.status, entry.retired, entry.stopped),
      };

      return {
        ...row,
        trackPosition: toMapPosition(row),
      };
    })
    .sort((left, right) => {
      const leftPosition = left.gridPosition ?? left.line ?? Number.MAX_SAFE_INTEGER;
      const rightPosition = right.gridPosition ?? right.line ?? Number.MAX_SAFE_INTEGER;

      if (leftPosition !== rightPosition) {
        return leftPosition - rightPosition;
      }

      return left.driverNumber - right.driverNumber;
    });
};

export const buildTrackMapPositions = (
  dashboard: RaceDashboard
): RaceMapPosition[] =>
  buildRaceDashboardRows(dashboard)
    .map((row) => row.trackPosition)
    .filter((position): position is RaceMapPosition => position !== null);

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
