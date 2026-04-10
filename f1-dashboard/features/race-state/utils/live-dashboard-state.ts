import type {
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/race-state/types/race-state";

export const applyRaceStateMessage = (
  currentDashboard: RaceDashboard | null,
  message: RaceStateWsMessage
): RaceDashboard | null => {
  if (message.type === "race.state.snapshot") {
    return message.dashboard;
  }

  if (!currentDashboard) {
    return null;
  }

  return message.dashboard;
};
