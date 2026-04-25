import type {
  RaceCurrentState,
  RaceDashboard,
} from "@/features/store/race-state/raceStateTypes";
import type { RootState } from "@/features/store/store";
import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

type RaceStateLiveState = {
  dashboard: RaceDashboard | null;
  currentState: RaceCurrentState | null;
};

const initialState: RaceStateLiveState = {
  dashboard: null,
  currentState: null,
};

const raceStateLiveSlice = createSlice({
  name: "raceStateLive",
  initialState,
  reducers: {
    setRaceStateLiveSnapshot(
      state,
      action: PayloadAction<{
        dashboard: RaceDashboard;
        currentState: RaceCurrentState | null;
      }>
    ) {
      state.dashboard = action.payload.dashboard;
      state.currentState = action.payload.currentState;
    },
    clearRaceStateLiveSnapshot(state) {
      state.dashboard = initialState.dashboard;
      state.currentState = initialState.currentState;
    },
  },
});

export const { clearRaceStateLiveSnapshot, setRaceStateLiveSnapshot } =
  raceStateLiveSlice.actions;

export const raceStateLiveReducer = raceStateLiveSlice.reducer;

export const selectRaceStateLiveDashboard = (state: RootState) =>
  state.raceStateLive.dashboard;
export const selectRaceStateLiveCurrentState = (state: RootState) =>
  state.raceStateLive.currentState;
