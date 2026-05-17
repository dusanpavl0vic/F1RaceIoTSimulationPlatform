import type {
  RaceCurrentState,
  RaceDashboardDriverRow,
  RaceTelemetryDriverSummary,
} from "@/features/store/race-state/raceStateTypes";

export const buildTelemetryDriverRows = (
  drivers: RaceTelemetryDriverSummary[],
  currentState: RaceCurrentState | null,
): RaceDashboardDriverRow[] =>
  resolveTelemetryDriverSource(drivers, currentState)
    .sort((left, right) => left.driverNumber - right.driverNumber)
    .map((driver) => ({
      driverNumber: driver.driverNumber,
      broadcastName: driver.broadcastName,
      fullName: driver.fullName,
      tla: driver.tla,
      teamName: driver.teamName,
      teamColor: driver.teamColor,
      position: driver.position,
      line: null,
      gridPosition: driver.gridPosition,
      gapToLeader: null,
      intervalToPositionAhead: null,
      isCatchingAhead: null,
      inPit: false,
      pitOut: false,
      retired: false,
      stopped: false,
      didNotStart: false,
      status: null,
      bestLapTime: null,
      lastLapTime: null,
      tyreCompound: null,
      tyreIsNew: null,
      currentStintLapCount: null,
      driverLabel:
        driver.tla ||
        driver.broadcastName ||
        driver.fullName ||
        `#${driver.driverNumber}`,
      displayTeamName: driver.teamName ?? "-",
      pitFlag: "-",
      statusLabel: "-",
    }));

const resolveTelemetryDriverSource = (
  drivers: RaceTelemetryDriverSummary[],
  currentState: RaceCurrentState | null,
) => {
  const currentStateDrivers = currentState
    ? Object.values(currentState.drivers).map((driver) => ({
        driverNumber: driver.driverNumber,
        broadcastName: driver.broadcastName,
        fullName: driver.fullName,
        tla: driver.tla,
        teamName: driver.team.name,
        teamColor: driver.team.color,
        position: driver.leaderboard.position,
        gridPosition: driver.leaderboard.gridPosition,
      }))
    : [];

  return currentStateDrivers.length > 0 ? currentStateDrivers : drivers;
};
