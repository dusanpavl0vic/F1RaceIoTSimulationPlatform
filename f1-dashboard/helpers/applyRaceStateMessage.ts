import type {
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/store/race-state/raceStateTypes";

export const applyRaceStateMessage = (
  _currentDashboard: RaceDashboard | null,
  message: RaceStateWsMessage
): RaceDashboard | null => {
  return message.dashboard;
};
