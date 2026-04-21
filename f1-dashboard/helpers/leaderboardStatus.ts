export type LeaderboardStatusTone = "neutral" | "success" | "warning" | "error";

export type LeaderboardStatusBadgeState = {
  label: string;
  tone: LeaderboardStatusTone;
  filled: boolean;
};

export const resolveLeaderboardStatusBadgeState = (
  statusLabel: string,
): LeaderboardStatusBadgeState => {
  switch (statusLabel) {
    case "RET":
      return {
        label: "RET",
        tone: "error",
        filled: true,
      };
    case "STOP":
      return {
        label: "STOP",
        tone: "error",
        filled: false,
      };
    case "PIT":
      return {
        label: "PIT",
        tone: "warning",
        filled: true,
      };
    case "OUT":
      return {
        label: "OUT",
        tone: "warning",
        filled: false,
      };
    case "RUN":
      return {
        label: "RUN",
        tone: "success",
        filled: false,
      };
    default:
      return {
        label: statusLabel === "-" ? "—" : statusLabel,
        tone: "neutral",
        filled: false,
      };
  }
};
