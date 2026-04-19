export type RaceStateSessionView = {
  sessionId: string | null;
  sessionInfo?: unknown;
  currentLap: number | null;
  totalLaps: number | null;
  trackStatusCode: string | null;
  trackStatusMessage: string | null;
  lastProcessedEventTime: string | null;
  lastProcessedSequence: number | null;
  updatedAt: string | null;
};

export type RaceLeaderboardEntry = {
  driverNumber: number;
  broadcastName: string | null;
  fullName: string | null;
  tla: string | null;
  teamName: string | null;
  teamColor: string | null;
  position: number | null;
  line: number | null;
  gridPosition: number | null;
  gapToLeader: string | null;
  intervalToPositionAhead: string | null;
  isCatchingAhead: boolean | null;
  inPit: boolean;
  pitOut: boolean;
  retired: boolean;
  stopped: boolean;
  didNotStart: boolean;
  status: number | null;
  bestLapTime: string | null;
  lastLapTime: string | null;
  tyreCompound: string | null;
  tyreIsNew: boolean | null;
  currentStintLapCount: number | null;
};

export type RaceDashboardDriverRow = RaceLeaderboardEntry & {
  driverLabel: string;
  displayTeamName: string;
  pitFlag: string;
  statusLabel: string;
};

export type RaceDashboard = {
  session: RaceStateSessionView;
  leaderboard: RaceLeaderboardEntry[];
};

export type RaceTelemetrySessionInfo = RaceStateSessionView;

export type RaceTelemetryDriverSummary = {
  driverNumber: number;
  broadcastName: string | null;
  fullName: string | null;
  tla: string | null;
  teamName: string | null;
  teamColor: string | null;
  position: number | null;
  gridPosition: number | null;
};

export type RaceTelemetryMetadata = {
  session: RaceTelemetrySessionInfo;
  drivers: RaceTelemetryDriverSummary[];
};

export type RaceCurrentDriverState = {
  driverNumber: number;
  broadcastName: string | null;
  fullName: string | null;
  tla: string | null;
  team: {
    name: string | null;
    color: string | null;
  };
  leaderboard: {
    position: number | null;
    displayPosition: number | null;
    timingPosition: number | null;
    line: number | null;
    gridPosition: number | null;
    lapSeriesPosition: number | null;
    lapsCompleted: number | null;
    gapToLeader: string | null;
    intervalToPositionAhead: string | null;
    bestLapTime: string | null;
    lastLapTime: string | null;
  };
  tyres: {
    compound: string | null;
    isNew: boolean | null;
    currentStintLapCount: number | null;
  };
  race: {
    inPit: boolean;
    pitOut: boolean;
    retired: boolean;
    stopped: boolean;
    didNotStart: boolean;
    status: number | null;
  };
};

export type RaceCurrentState = {
  sessionId: string | null;
  updatedAt: string;
  lastProcessedEventTime: string | null;
  lastProcessedSequence: number | null;
  session: Record<string, unknown>;
  drivers: Record<string, RaceCurrentDriverState>;
  leaderboard: RaceLeaderboardEntry[];
};

export type SessionCard = {
  label: string;
  value: string;
};

export type RaceStateBroadcastMessage = {
  type: "race.state.updated";
  sentAt: string;
  dashboard: RaceDashboard;
  currentState: RaceCurrentState;
};

export type RaceStateWsMessage = RaceStateBroadcastMessage;

export type RaceStateTelemetrySample = {
  sessionId: string;
  driverNumber: number;
  lapNumber: number;
  stintNumber: number;
  sampleIndex: number;
  timestamp: string;
  speed?: number;
  rpm?: number;
  throttlePct?: number;
  rawThrottle?: number;
  brakePct?: number;
  rawBrake?: number;
  gear?: number;
  drsEnabled?: boolean;
};

export type RaceStateTelemetryReadyMessage = {
  type: "telemetry.stream.ready";
  sessionId: string;
  driverNumber: number;
  recentSampleCount: number;
};

export type RaceStateTelemetrySampleMessage = {
  type: "telemetry.sample";
  data: RaceStateTelemetrySample;
};

export type RaceStateTelemetryErrorMessage = {
  type: "telemetry.stream.error";
  message: string;
};

export type RaceStateUiView = {
  wsStatus: string;
  lastWsPayloadPreview: string;
  lastWsMessageReceivedAt: string | null;
};
