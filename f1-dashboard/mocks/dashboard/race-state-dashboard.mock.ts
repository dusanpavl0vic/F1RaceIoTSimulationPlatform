import type {
  RaceCurrentDriverState,
  RaceCurrentState,
  RaceDashboard,
  RaceLeaderboardEntry,
  RaceStateBroadcastMessage,
} from "@/features/store/race-state/raceStateTypes";

type MockDriverSeed = {
  driverNumber: number;
  broadcastName: string;
  fullName: string;
  tla: string;
  teamName: string;
  teamColor: string;
  position: number;
  gapToLeader: string | null;
  intervalToPositionAhead: string | null;
  bestLapTime: string;
  lastLapTime: string;
  tyreCompound: string;
  tyreIsNew: boolean;
  currentStintLapCount: number;
  inPit?: boolean;
  pitOut?: boolean;
  retired?: boolean;
  stopped?: boolean;
  didNotStart?: boolean;
  status?: number;
};

type MockSnapshot = {
  dashboard: RaceDashboard;
  currentState: RaceCurrentState;
  message: RaceStateBroadcastMessage;
};

const sessionId = "italian-gp-2021-race";
const totalLaps = 53;
const baseLap = 18;
const baseSequence = 124_500;
const baseTimestamp = Date.parse("2021-09-12T13:24:12Z");

const driverSeeds: MockDriverSeed[] = [
  {
    driverNumber: 33,
    broadcastName: "VERSTAPPEN",
    fullName: "Max Verstappen",
    tla: "VER",
    teamName: "Red Bull Racing",
    teamColor: "1E5BC6",
    position: 1,
    gapToLeader: null,
    intervalToPositionAhead: null,
    bestLapTime: "1:24.812",
    lastLapTime: "1:25.114",
    tyreCompound: "MED",
    tyreIsNew: false,
    currentStintLapCount: 12,
    status: 64,
  },
  {
    driverNumber: 44,
    broadcastName: "HAMILTON",
    fullName: "Lewis Hamilton",
    tla: "HAM",
    teamName: "Mercedes",
    teamColor: "6CD3BF",
    position: 2,
    gapToLeader: "+1.284",
    intervalToPositionAhead: "+1.284",
    bestLapTime: "1:24.921",
    lastLapTime: "1:25.238",
    tyreCompound: "MED",
    tyreIsNew: false,
    currentStintLapCount: 12,
    status: 64,
  },
  {
    driverNumber: 4,
    broadcastName: "NORRIS",
    fullName: "Lando Norris",
    tla: "NOR",
    teamName: "McLaren",
    teamColor: "F58020",
    position: 3,
    gapToLeader: "+3.941",
    intervalToPositionAhead: "+2.657",
    bestLapTime: "1:25.103",
    lastLapTime: "1:25.447",
    tyreCompound: "HARD",
    tyreIsNew: false,
    currentStintLapCount: 8,
    status: 64,
  },
  {
    driverNumber: 16,
    broadcastName: "LECLERC",
    fullName: "Charles Leclerc",
    tla: "LEC",
    teamName: "Ferrari",
    teamColor: "DC0000",
    position: 4,
    gapToLeader: "+5.228",
    intervalToPositionAhead: "+1.287",
    bestLapTime: "1:25.172",
    lastLapTime: "1:25.503",
    tyreCompound: "HARD",
    tyreIsNew: false,
    currentStintLapCount: 8,
    status: 64,
  },
  {
    driverNumber: 55,
    broadcastName: "SAINZ",
    fullName: "Carlos Sainz",
    tla: "SAI",
    teamName: "Ferrari",
    teamColor: "DC0000",
    position: 5,
    gapToLeader: "+6.012",
    intervalToPositionAhead: "+0.784",
    bestLapTime: "1:25.241",
    lastLapTime: "1:25.590",
    tyreCompound: "HARD",
    tyreIsNew: false,
    currentStintLapCount: 8,
    status: 64,
  },
  {
    driverNumber: 11,
    broadcastName: "PEREZ",
    fullName: "Sergio Perez",
    tla: "PER",
    teamName: "Red Bull Racing",
    teamColor: "1E5BC6",
    position: 6,
    gapToLeader: "+8.441",
    intervalToPositionAhead: "+2.429",
    bestLapTime: "1:25.314",
    lastLapTime: "1:25.721",
    tyreCompound: "MED",
    tyreIsNew: false,
    currentStintLapCount: 14,
    status: 64,
  },
  {
    driverNumber: 3,
    broadcastName: "RICCIARDO",
    fullName: "Daniel Ricciardo",
    tla: "RIC",
    teamName: "McLaren",
    teamColor: "F58020",
    position: 7,
    gapToLeader: "+10.193",
    intervalToPositionAhead: "+1.752",
    bestLapTime: "1:25.382",
    lastLapTime: "1:25.854",
    tyreCompound: "MED",
    tyreIsNew: false,
    currentStintLapCount: 14,
    status: 64,
  },
  {
    driverNumber: 14,
    broadcastName: "ALONSO",
    fullName: "Fernando Alonso",
    tla: "ALO",
    teamName: "Alpine",
    teamColor: "2293D1",
    position: 8,
    gapToLeader: "+12.773",
    intervalToPositionAhead: "+2.580",
    bestLapTime: "1:25.511",
    lastLapTime: "1:26.008",
    tyreCompound: "MED",
    tyreIsNew: false,
    currentStintLapCount: 14,
    status: 64,
  },
  {
    driverNumber: 63,
    broadcastName: "RUSSELL",
    fullName: "George Russell",
    tla: "RUS",
    teamName: "Williams",
    teamColor: "37BEDD",
    position: 9,
    gapToLeader: "+16.212",
    intervalToPositionAhead: "+3.439",
    bestLapTime: "1:25.628",
    lastLapTime: "1:26.181",
    tyreCompound: "SOFT",
    tyreIsNew: false,
    currentStintLapCount: 6,
    status: 64,
  },
  {
    driverNumber: 5,
    broadcastName: "VETTEL",
    fullName: "Sebastian Vettel",
    tla: "VET",
    teamName: "Aston Martin",
    teamColor: "2D826D",
    position: 10,
    gapToLeader: "+19.807",
    intervalToPositionAhead: "+3.595",
    bestLapTime: "1:25.733",
    lastLapTime: "1:26.245",
    tyreCompound: "SOFT",
    tyreIsNew: false,
    currentStintLapCount: 6,
    status: 64,
  },
];

export function buildMockRaceDashboardSnapshot(step = 0): MockSnapshot {
  const tick = Math.max(step, 0);
  const currentLap = Math.min(baseLap + tick, totalLaps);
  const updatedAt = new Date(baseTimestamp + tick * 4_000).toISOString();
  const lastProcessedEventTime = new Date(baseTimestamp + tick * 3_000).toISOString();
  const sequence = baseSequence + tick * 12;
  const leaderboard = driverSeeds.map((seed, index) =>
    buildLeaderboardEntry(seed, index, currentLap));

  const currentState: RaceCurrentState = {
    sessionId,
    updatedAt,
    lastProcessedEventTime,
    lastProcessedSequence: sequence,
    session: {
      "lap.count.updated": {
        currentLap,
        totalLaps,
      },
    },
    drivers: Object.fromEntries(
      leaderboard.map((entry) => [String(entry.driverNumber), buildCurrentDriverState(entry)])
    ),
  };

  const dashboard: RaceDashboard = {
    session: {
      sessionId,
      currentLap,
      totalLaps,
      trackStatusCode: "1",
      trackStatusMessage: "All clear",
      lastProcessedEventTime,
      lastProcessedSequence: sequence,
      updatedAt,
    },
    leaderboard,
  };

  return {
    dashboard,
    currentState,
    message: {
      type: "race.state.updated",
      sentAt: updatedAt,
      dashboard,
      currentState,
    },
  };
}

function buildLeaderboardEntry(
  seed: MockDriverSeed,
  index: number,
  currentLap: number
): RaceLeaderboardEntry {
  const isLeader = seed.position === 1;

  return {
    driverNumber: seed.driverNumber,
    broadcastName: seed.broadcastName,
    fullName: seed.fullName,
    tla: seed.tla,
    teamName: seed.teamName,
    teamColor: seed.teamColor,
    position: seed.position,
    line: seed.position,
    gridPosition: seed.position,
    gapToLeader: isLeader ? null : seed.gapToLeader,
    intervalToPositionAhead: isLeader ? null : seed.intervalToPositionAhead,
    isCatchingAhead: index % 2 === 1,
    inPit: seed.inPit ?? false,
    pitOut: seed.pitOut ?? false,
    retired: seed.retired ?? false,
    stopped: seed.stopped ?? false,
    didNotStart: seed.didNotStart ?? false,
    status: seed.status ?? 64,
    bestLapTime: seed.bestLapTime,
    lastLapTime: seed.lastLapTime,
    tyreCompound: seed.tyreCompound,
    tyreIsNew: seed.tyreIsNew,
    currentStintLapCount: seed.currentStintLapCount + Math.max(currentLap - baseLap, 0),
  };
}

function buildCurrentDriverState(entry: RaceLeaderboardEntry): RaceCurrentDriverState {
  return {
    driverNumber: entry.driverNumber,
    broadcastName: entry.broadcastName,
    fullName: entry.fullName,
    tla: entry.tla,
    team: {
      name: entry.teamName,
      color: entry.teamColor,
    },
    leaderboard: {
      position: entry.position,
      displayPosition: entry.position,
      timingPosition: entry.position,
      line: entry.line,
      gridPosition: entry.gridPosition,
      lapSeriesPosition: entry.position,
      lapsCompleted: null,
      gapToLeader: entry.gapToLeader,
      intervalToPositionAhead: entry.intervalToPositionAhead,
      bestLapTime: entry.bestLapTime,
      lastLapTime: entry.lastLapTime,
    },
    tyres: {
      compound: entry.tyreCompound,
      isNew: entry.tyreIsNew,
      currentStintLapCount: entry.currentStintLapCount,
    },
    race: {
      inPit: entry.inPit,
      pitOut: entry.pitOut,
      retired: entry.retired,
      stopped: entry.stopped,
      didNotStart: entry.didNotStart,
      status: entry.status,
    },
  };
}
