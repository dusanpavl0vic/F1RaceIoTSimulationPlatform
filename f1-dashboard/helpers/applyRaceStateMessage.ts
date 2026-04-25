import type {
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/store/race-state/raceStateTypes";

export const applyRaceStateMessage = (
  currentDashboard: RaceDashboard | null,
  message: RaceStateWsMessage
): RaceDashboard | null => {
  return message.type === "race.state.updated"
    ? message.dashboard
    : currentDashboard;
};
