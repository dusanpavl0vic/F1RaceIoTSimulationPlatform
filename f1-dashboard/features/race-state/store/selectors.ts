import type { RootState } from "@/store";

export const selectRaceStateUi = (state: RootState) => state.raceStateUi;
export const selectWsStatus = (state: RootState) => state.raceStateUi.wsStatus;
