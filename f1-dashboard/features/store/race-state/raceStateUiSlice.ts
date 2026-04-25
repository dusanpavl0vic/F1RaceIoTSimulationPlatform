import type { RootState } from "@/features/store/store";
import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

export type RaceStateWebSocketStatus =
  | "idle"
  | "connecting"
  | "open"
  | "closed"
  | "error";

export type RaceBattleAlert = {
  id: string;
  message: string;
  sentAt: string;
  sessionId: string;
  driverNumber: number;
  aheadDriverNumber: number;
  battleForPosition: number;
  gapLabel: string;
};

type RaceStateUiState = {
  wsStatus: RaceStateWebSocketStatus;
  lastWsPayloadPreview: string;
  lastWsMessageReceivedAt: string | null;
  battleAlerts: RaceBattleAlert[];
};

const initialState: RaceStateUiState = {
  wsStatus: "idle",
  lastWsPayloadPreview: "No WS messages yet",
  lastWsMessageReceivedAt: null,
  battleAlerts: [],
};

const raceStateUiSlice = createSlice({
  name: "raceStateUi",
  initialState,
  reducers: {
    setWsStatus(state, action: PayloadAction<RaceStateWebSocketStatus>) {
      state.wsStatus = action.payload;
    },
    setLastWsPayloadPreview(state, action: PayloadAction<string>) {
      state.lastWsPayloadPreview = action.payload;
      state.lastWsMessageReceivedAt = new Date().toISOString();
    },
    enqueueBattleAlert(state, action: PayloadAction<RaceBattleAlert>) {
      const nextAlerts = state.battleAlerts.filter(
        (alert) => alert.id !== action.payload.id
      );
      nextAlerts.push(action.payload);
      state.battleAlerts = nextAlerts.slice(-5);
      state.lastWsMessageReceivedAt = new Date().toISOString();
    },
    dismissBattleAlert(state, action: PayloadAction<string>) {
      state.battleAlerts = state.battleAlerts.filter(
        (alert) => alert.id !== action.payload
      );
    },
    resetWsPreview(state) {
      state.lastWsPayloadPreview = initialState.lastWsPayloadPreview;
      state.lastWsMessageReceivedAt = initialState.lastWsMessageReceivedAt;
      state.battleAlerts = initialState.battleAlerts;
    },
  },
});

export const {
  dismissBattleAlert,
  enqueueBattleAlert,
  resetWsPreview,
  setLastWsPayloadPreview,
  setWsStatus,
} = raceStateUiSlice.actions;

export const raceStateUiReducer = raceStateUiSlice.reducer;


export const selectRaceStateUi = (state: RootState) => state.raceStateUi;
export const selectWsStatus = (state: RootState) => state.raceStateUi.wsStatus;
export const selectBattleAlerts = (state: RootState) =>
  state.raceStateUi.battleAlerts;
