export type RaceStateSessionView = {
  sessionId: string | null;
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
  tla: string;
  driverName: string;
  teamName: string;
  teamColor: string | null;
  position: number | null;
  line: number | null;
  gridPosition: number | null;
  gapToLeader: string;
  intervalToPositionAhead: string;
  lastLapTime: string;
  bestLapTime: string;
  tyreCompound: string;
  tyreIsNew: boolean | null;
  currentStintLapCount: number | null;
  pitFlag: string;
  status: string;
  speed: number | null;
  gear: number | null;
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
  speedReadings: Record<string, string>;
  drs: number | null;
  trackPosition: RaceMapPosition | null;
};

export type RaceDashboard = {
  session: RaceStateSessionView;
  leaderboard: RaceLeaderboardEntry[];
  mapPositions: RaceMapPosition[];
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
  session: RaceStateSessionView;
  change:
    | {
        Section: "session";
        Session: RaceStateSessionView;
      }
    | {
        Section: "driver";
        LeaderboardEntry: RaceLeaderboardEntry;
        MapPosition: RaceMapPosition | null;
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
