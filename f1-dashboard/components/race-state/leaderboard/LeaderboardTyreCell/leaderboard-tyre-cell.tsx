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

function LeaderboardTyreCell({ row }: LeaderboardTyreCellProps) {
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

export default LeaderboardTyreCell;
