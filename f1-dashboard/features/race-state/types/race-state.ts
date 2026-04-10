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

export type RaceDriverTrackPosition = {
  timestamp: string | null;
  status: string | null;
  x: number | null;
  y: number | null;
  z: number | null;
  rawX?: number | null;
  rawY?: number | null;
  rawZ?: number | null;
  hasRawCoordinates?: boolean | null;
  isEstimated?: boolean | null;
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
  status: number | null;
  bestLapTime: string | null;
  lastLapTime: string | null;
  sectors: Record<string, unknown> | null;
  speeds: Record<string, unknown> | null;
  tyreCompound: string | null;
  tyreIsNew: boolean | null;
  tyreStints: Record<string, unknown> | null;
  currentStintLapCount: number | null;
  pitStops: unknown[];
  currentTrackPosition: RaceDriverTrackPosition | null;
  currentTrackPositionTimestamp: string | null;
  lastPositionPacket: Record<string, unknown> | null;
  rpm: number | null;
  speed: number | null;
  gear: number | null;
  throttle: number | null;
  brake: number | null;
  drs: number | null;
  lastTelemetryPacket: Record<string, unknown> | null;
};

export type RaceMapPosition = {
  driverNumber: number;
  driverName: string;
  position: number | null;
  status: string;
  x: number;
  y: number;
  z: number;
  timestamp: string | null;
  isEstimated: boolean;
};

export type RaceDashboardDriverRow = RaceLeaderboardEntry & {
  trackPosition: RaceMapPosition | null;
  driverLabel: string;
  displayTeamName: string;
  pitFlag: string;
  statusLabel: string;
};

export type RaceDashboard = {
  session: RaceStateSessionView;
  leaderboard: RaceLeaderboardEntry[];
};

export type SessionCard = {
  label: string;
  value: string;
};

export type RaceStateSnapshotMessage = {
  type: "race.state.snapshot";
  sentAt: string;
  dashboard: RaceDashboard;
};

export type RaceStateChangeMessage = {
  type: "race.state.change";
  sentAt: string;
  stateKey: string | null;
  eventType: string;
  sessionId: string;
  driverNumber: number | null;
  eventTime: string;
  sequence: number;
  dashboard: RaceDashboard;
  session: RaceStateSessionView;
  change:
    | {
        Section: "session";
        Session: RaceStateSessionView;
      }
    | {
        Section: "state";
        DriverNumber: number | null;
        EventType: string;
      }
    | {
        Section: "snapshot";
        Snapshot: unknown;
      };
};

export type RaceStateWsMessage =
  | RaceStateSnapshotMessage
  | RaceStateChangeMessage;

export type RaceStateUiView = {
  wsStatus: string;
  lastWsPayloadPreview: string;
  lastWsMessageReceivedAt: string | null;
};
