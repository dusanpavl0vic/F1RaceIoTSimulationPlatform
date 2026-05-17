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

export type RaceTyreStint = {
  stintNumber: number;
  compound: string | null;
  tyreIsNew: boolean | null;
  startLap: number;
  endLap: number;
  lapCount: number;
};

export type RaceTyreStintDriver = {
  driverNumber: number;
  driverName: string;
  teamName: string | null;
  teamColor: string | null;
  gridPosition: number | null;
  position: number | null;
  stints: RaceTyreStint[];
};

export type RaceTyreStintStrategy = {
  sessionId: string;
  totalLaps: number;
  drivers: RaceTyreStintDriver[];
};

export type NextLapPredictionFeatures = {
  driver_number: number;
  lap_number: number;
  position: number | null;
  lap_time_last: number | null;
  lap_time_best: number | null;
  lap_time_avg_last_3: number | null;
  lap_time_avg_last_5: number | null;
  gap_to_leader: number | null;
  gap_to_ahead: number | null;
  stint_number?: number | null;
  tyre_compound: string | null;
  tyre_is_new: boolean | null;
  tyre_laps_on_set: number | null;
  in_pit: boolean | null;
  avg_speed_last_lap?: number | null;
  max_speed_last_lap?: number | null;
  avg_rpm_last_lap?: number | null;
  avg_throttle_pct_last_lap?: number | null;
  avg_raw_brake_last_lap?: number | null;
  drs_open_ratio_last_lap?: number | null;
  gear_changes_last_lap?: number | null;
};

export type NextLapPredictionBatchRequest = {
  items: NextLapPredictionFeatures[];
};

export type NextLapPredictionRequest = {
  features: NextLapPredictionFeatures;
};

export type NextLapPredictionResponse = {
  predicted_next_lap_time: number;
  model_version: string | null;
};

export type DriverPredictionFeaturesResponse = {
  sessionId: string | null;
  driverNumber: number;
  driverName: string;
  position: number | null;
  lastCompletedLapNumber: number | null;
  lastCompletedLapTimeSeconds: number | null;
  lapTimeAvgLast3: number | null;
  lapTimeAvgLast5: number | null;
  stintNumber: number | null;
  tyreCompound: string | null;
  tyreIsNew: boolean | null;
  tyreLapsOnSet: number | null;
  inPit: boolean;
  gapToLeader: number | null;
  gapToAhead: number | null;
  features: NextLapPredictionFeatures | null;
};

export type NextLapPredictionBatchResponse = {
  predictions: Array<{
    predicted_next_lap_time: number;
    model_version: string | null;
  }>;
  model_version: string | null;
};

export type PredictionWorkflowStatus =
  | "loading_basis"
  | "ready"
  | "predicting"
  | "waiting_for_actual"
  | "resolved"
  | "no_data"
  | "error";

export type RaceNextLapPredictionDriver = {
  driverNumber: number;
  driverName: string;
  teamName: string | null;
  teamColor: string | null;
  position: number | null;
  completedLaps: number | null;
  predictedForLap: number | null;
  predictedNextLapTime: number | null;
  lastLapTimeActual: number | null;
  bestLapTimeActual: number | null;
};

export type RaceNextLapPredictions = {
  sessionId: string;
  triggerLap: number | null;
  modelVersion: string | null;
  generatedAt: string;
  drivers: RaceNextLapPredictionDriver[];
};

export type PredictionComparisonEntry = {
  driverNumber: number;
  driverName: string;
  basisLapNumber: number;
  basisLapTime: number | null;
  lapNumber: number;
  predictedLapTime: number;
  actualLapTime: number;
  deltaToActual: number;
  accuracyPercentage: number;
  modelVersion: string | null;
  resolvedAt: string;
};

export type NextLapPredictionCycle = {
  driverNumber: number;
  driverName: string;
  teamName: string | null;
  teamColor: string | null;
  position: number | null;
  basisLapNumber: number;
  predictedForLap: number;
  basisLapTime: number | null;
  bestLapTime: number | null;
  averageLast3: number | null;
  averageLast5: number | null;
  predictedNextLapTime: number;
  modelVersion: string | null;
  generatedAt: string;
  actualNextLapTime: number | null;
  deltaToActual: number | null;
  accuracyPercentage: number | null;
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
  prediction: {
    lapTimeHistory: number[];
    lapTimeAvgLast3: number | null;
    lapTimeAvgLast5: number | null;
    lastCompletedLapNumber: number | null;
    lastCompletedLapTimeSeconds: number | null;
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
};

export type SessionCard = {
  label: string;
  value: string;
};

export type RaceStateBroadcastMessage = {
  type: "race.state.updated";
  sentAt: string;
  dashboard: RaceDashboard;
  currentState?: RaceCurrentState;
};

export type RaceBattleAlertMessage = {
  type: "battle.alert";
  sentAt: string;
  sessionId: string;
  driverNumber: number;
  driverLabel: string;
  aheadDriverNumber: number;
  aheadDriverLabel: string;
  battleForPosition: number;
  gapSeconds: number | null;
  gapLabel: string;
  message: string;
};

export type RaceStateWsMessage = RaceStateBroadcastMessage | RaceBattleAlertMessage;

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
