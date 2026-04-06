import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

export type RaceStateWebSocketStatus =
  | "idle"
  | "connecting"
  | "open"
  | "closed"
  | "error";

type RaceStateUiState = {
  wsStatus: RaceStateWebSocketStatus;
  lastWsPayloadPreview: string;
  lastWsMessageReceivedAt: string | null;
};

const initialState: RaceStateUiState = {
  wsStatus: "idle",
  lastWsPayloadPreview: "No WS messages yet",
  lastWsMessageReceivedAt: null,
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
    resetWsPreview(state) {
      state.lastWsPayloadPreview = initialState.lastWsPayloadPreview;
      state.lastWsMessageReceivedAt = initialState.lastWsMessageReceivedAt;
    },
  },
});

export const { resetWsPreview, setLastWsPayloadPreview, setWsStatus } =
  raceStateUiSlice.actions;

export const raceStateUiReducer = raceStateUiSlice.reducer;
