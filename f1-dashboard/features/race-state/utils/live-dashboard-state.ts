import type {
  RaceDashboard,
  RaceLeaderboardEntry,
  RaceMapPosition,
  RaceStateChangeMessage,
  RaceStateWsMessage,
} from "@/features/race-state/types/race-state";

const sortLeaderboard = (entries: RaceLeaderboardEntry[]) =>
  [...entries].sort((left, right) => {
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

const sortMapPositions = (entries: RaceMapPosition[]) =>
  [...entries].sort((left, right) => {
    const leftPosition = left.position ?? Number.MAX_SAFE_INTEGER;
    const rightPosition = right.position ?? Number.MAX_SAFE_INTEGER;

    if (leftPosition !== rightPosition) {
      return leftPosition - rightPosition;
    }

    return left.driverNumber - right.driverNumber;
  });

const upsertLeaderboardEntry = (
  leaderboard: RaceLeaderboardEntry[],
  nextEntry: RaceLeaderboardEntry
) => {
  const withoutCurrent = leaderboard.filter(
    (entry) => entry.driverNumber !== nextEntry.driverNumber
  );

  return sortLeaderboard([...withoutCurrent, nextEntry]);
};

const upsertMapPosition = (
  mapPositions: RaceMapPosition[],
  nextPosition: RaceMapPosition | null,
  driverNumber: number
) => {
  const filtered = mapPositions.filter(
    (entry) => entry.driverNumber !== driverNumber
  );

  if (!nextPosition) {
    return sortMapPositions(filtered);
  }

  return sortMapPositions([...filtered, nextPosition]);
};

const applyChangeMessage = (
  dashboard: RaceDashboard,
  message: RaceStateChangeMessage
): RaceDashboard => {
  const change = message.change;

  if (change.Section === "session") {
    return {
      ...dashboard,
      session: change.Session,
    };
  }

  if (change.Section === "driver") {
    return {
      ...dashboard,
      session: message.session,
      leaderboard: upsertLeaderboardEntry(
        dashboard.leaderboard,
        change.LeaderboardEntry
      ),
      mapPositions: upsertMapPosition(
        dashboard.mapPositions,
        change.MapPosition,
        change.LeaderboardEntry.driverNumber
      ),
    };
  }

  return dashboard;
};

export const applyRaceStateMessage = (
  currentDashboard: RaceDashboard | null,
  message: RaceStateWsMessage
): RaceDashboard | null => {
  if (message.type === "race.state.snapshot") {
    return message.dashboard;
  }

  if (!currentDashboard) {
    return null;
  }

  return applyChangeMessage(currentDashboard, message);
};
