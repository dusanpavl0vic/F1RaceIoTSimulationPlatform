import { Chip } from "@mui/material";
import type { RaceDashboardDriverRow } from "@/features/race-state/types/race-state";
import { StyledTyreLapCount, StyledTyreWrapper } from "./leaderboard-tyre-cell.styles";

type LeaderboardTyreCellProps = {
  row: RaceDashboardDriverRow;
};

const resolveTyreColor = (compound: string) => {
  switch (compound.toLowerCase()) {
    case "soft": case "c5": case "c4": return "error" as const;
    case "medium": case "c3": return "warning" as const;
    case "hard": case "c2": case "c1": return "default" as const;
    case "intermediate": case "inter": return "success" as const;
    case "wet": return "info" as const;
    default: return "default" as const;
  }
};

export function LeaderboardTyreCell({ row }: LeaderboardTyreCellProps) {
  const compound = row.tyreCompound ?? "-";

  return (
    <StyledTyreWrapper>
      <Chip
        size="small"
        label={compound === "-" ? "?" : compound.substring(0, 1).toUpperCase()}
        color={resolveTyreColor(compound)}
        variant={row.tyreIsNew ? "filled" : "outlined"}
        title={compound}
      />
      {row.currentStintLapCount && (
        <StyledTyreLapCount>{row.currentStintLapCount}L</StyledTyreLapCount>
      )}
    </StyledTyreWrapper>
  );
}
