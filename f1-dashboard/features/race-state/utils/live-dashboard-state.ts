import type {
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/race-state/types/race-state";

export const applyRaceStateMessage = (
  _currentDashboard: RaceDashboard | null,
  message: RaceStateWsMessage
): RaceDashboard | null => {
  return message.dashboard;
};
