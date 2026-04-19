import type {
  RaceTelemetryDriverSummary,
  RaceTelemetryMetadata,
  RaceTelemetrySessionInfo,
} from "@/features/store/race-state/raceStateTypes";
import type { RootState } from "@/features/store/store";
import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

type RaceStateTelemetryState = {
  session: RaceTelemetrySessionInfo | null;
  drivers: RaceTelemetryDriverSummary[];
  status: "idle" | "loading" | "ready" | "error";
  error: string | null;
};

const initialState: RaceStateTelemetryState = {
  session: null,
  drivers: [],
  status: "idle",
  error: null,
};

const raceStateTelemetrySlice = createSlice({
  name: "raceStateTelemetry",
  initialState,
  reducers: {
    setRaceStateTelemetryLoading(state) {
      state.status = "loading";
      state.error = null;
    },
    setRaceStateTelemetryMetadata(state, action: PayloadAction<RaceTelemetryMetadata>) {
      state.session = action.payload.session;
      state.drivers = action.payload.drivers;
      state.status = "ready";
      state.error = null;
    },
    setRaceStateTelemetryError(state, action: PayloadAction<string>) {
      state.status = "error";
      state.error = action.payload;
    },
  },
});

export const {
  setRaceStateTelemetryError,
  setRaceStateTelemetryLoading,
  setRaceStateTelemetryMetadata,
} = raceStateTelemetrySlice.actions;

export const raceStateTelemetryReducer = raceStateTelemetrySlice.reducer;

export const selectRaceStateTelemetry = (state: RootState) => state.raceStateTelemetry;
