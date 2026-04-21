import type {
  RaceDashboardDriverRow,
  RaceTelemetryDriverSummary,
} from "@/features/store/race-state/raceStateTypes";

export const buildTelemetryDriverRows = (
  drivers: RaceTelemetryDriverSummary[],
): RaceDashboardDriverRow[] =>
  drivers.map((driver) => ({
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
