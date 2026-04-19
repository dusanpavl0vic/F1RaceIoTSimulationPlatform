import type { RaceDashboardDriverRow } from "@/features/store/race-state/raceStateTypes";
import {
  StyledTyreBadge,
  StyledTyreBadgeLabel,
  StyledTyreLapCount,
  StyledTyreWrapper,
} from "./leaderboard-tyre-cell.styles";

type LeaderboardTyreCellProps = {
  row: RaceDashboardDriverRow;
};

const resolveTyreColor = (compound: string) => {
  switch (compound.toLowerCase()) {
    case "soft":
    case "c5":
    case "c4":
      return "error" as const;
    case "medium":
    case "c3":
      return "warning" as const;
    case "hard":
    case "c2":
    case "c1":
      return "default" as const;
    case "intermediate":
    case "inter":
      return "success" as const;
    case "wet":
      return "info" as const;
    default:
      return "default" as const;
  }
};

export function LeaderboardTyreCell({ row }: LeaderboardTyreCellProps) {
  const compound = row.tyreCompound ?? "-";
  const shortLabel =
    compound === "-" ? "?" : compound.substring(0, 1).toUpperCase();

  return (
    <StyledTyreWrapper>
      <StyledTyreBadge
        $compound={compound}
        $filled={Boolean(row.tyreIsNew)}
        title={compound}
      >
        <StyledTyreBadgeLabel>{shortLabel}</StyledTyreBadgeLabel>
      </StyledTyreBadge>
      {row.currentStintLapCount && (
        <StyledTyreLapCount>{row.currentStintLapCount}L</StyledTyreLapCount>
      )}
    </StyledTyreWrapper>
  );
}
