import type {
  RaceDashboard,
  RaceDashboardDriverRow,
  RaceMapPosition,
  RaceStateSessionView,
  SessionCard,
} from "@/features/race-state/types/race-state";
import type { RaceStateWebSocketStatus } from "@/features/race-state/store/race-state-ui-slice";

const readString = (value: unknown): string | null =>
  typeof value === "string" && value.trim().length > 0 ? value : null;

const resolveMapPosition = (
  driverNumber: number,
  mapPositions: RaceMapPosition[]
): RaceMapPosition | null =>
  mapPositions.find((position) => position.driverNumber === driverNumber) ?? null;

export const buildRaceDashboardRows = (
  dashboard: RaceDashboard
): RaceDashboardDriverRow[] => {
  return dashboard.leaderboard
    .map((entry) => {
      const trackPosition = resolveMapPosition(entry.driverNumber, dashboard.mapPositions);

      return {
        ...entry,
        position: entry.position ?? trackPosition?.position ?? null,
        speedReadings: {},
        drs: null,
        trackPosition,
      };
    })
    .sort((left, right) => {
      const leftPosition = left.position ?? Number.MAX_SAFE_INTEGER;
      const rightPosition = right.position ?? Number.MAX_SAFE_INTEGER;

      if (leftPosition !== rightPosition) {
        return leftPosition - rightPosition;
      }

      const leftLine = left.line ?? left.gridPosition ?? Number.MAX_SAFE_INTEGER;
      const rightLine = right.line ?? right.gridPosition ?? Number.MAX_SAFE_INTEGER;

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
