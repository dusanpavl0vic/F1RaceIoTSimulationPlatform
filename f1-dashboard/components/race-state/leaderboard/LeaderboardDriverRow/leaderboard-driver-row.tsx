import LeaderboardTyreCell from "@/components/race-state/leaderboard/LeaderboardTyreCell/leaderboard-tyre-cell";
import type { RaceDashboardDriverRow } from "@/features/store/race-state/raceStateTypes";
import { resolveLeaderboardStatusBadgeState } from "@/helpers/leaderboardStatus";
import { Chip, TableCell } from "@mui/material";
import {
  StyledDataCell,
  StyledDriverCell,
  StyledDriverNum,
  StyledDriverTableRow,
  StyledDriverTla,
  StyledLastCell,
  StyledLeaderLabel,
  StyledMutedCell,
  StyledPosArrow,
  StyledPosCell,
  StyledStatusBadge,
  StyledStatusBadgeLabel,
  StyledStatusStack,
} from "./leaderboard-driver-row.styles";

type LeaderboardDriverRowProps = {
  row: RaceDashboardDriverRow;
  isMobile: boolean;
  isTablet: boolean;
  positionChange: "gained" | "lost" | null;
};

function LeaderboardDriverRow({
  row,
  isMobile,
  isTablet,
  positionChange,
}: LeaderboardDriverRowProps) {
  const teamColor = row.teamColor
    ? `#${row.teamColor.replace(/^#/, "")}`
    : "#888";
  const pos = row.position ?? row.gridPosition ?? row.line;
  const isDimmed = row.retired || row.didNotStart || row.stopped;
  const statusBadge = resolveLeaderboardStatusBadgeState(row.statusLabel);

  return (
    <StyledDriverTableRow hover $dimmed={isDimmed} $flash={positionChange}>
      <StyledPosCell $isLeader={pos === 1}>
        {pos ?? "—"}
        {positionChange && (
          <StyledPosArrow $direction={positionChange}>
            {positionChange === "gained" ? "▲" : "▼"}
          </StyledPosArrow>
        )}
      </StyledPosCell>

      <StyledDriverCell $color={teamColor}>
        <StyledDriverTla $color={teamColor}>
          {row.tla ?? row.driverLabel}
        </StyledDriverTla>
        {!isMobile && <StyledDriverNum>#{row.driverNumber}</StyledDriverNum>}
      </StyledDriverCell>

      {!isMobile && <StyledMutedCell>{row.displayTeamName}</StyledMutedCell>}

      <StyledDataCell>
        {row.gapToLeader === "leader" ? (
          <StyledLeaderLabel>LEADER</StyledLeaderLabel>
        ) : (
          (row.gapToLeader ?? "—")
        )}
      </StyledDataCell>

      {!isTablet && (
        <StyledMutedCell>{row.intervalToPositionAhead ?? "—"}</StyledMutedCell>
      )}

      {!isMobile && (
        <TableCell>
          <LeaderboardTyreCell row={row} />
        </TableCell>
      )}

      <StyledDataCell>{row.lastLapTime ?? "—"}</StyledDataCell>

      {!isTablet && <StyledMutedCell>{row.bestLapTime ?? "—"}</StyledMutedCell>}

      <StyledLastCell>
        <StyledStatusStack>
          {row.pitFlag !== "-" && (
            <Chip
              size="small"
              label={row.pitFlag}
              color="warning"
              variant="filled"
            />
          )}
          <StyledStatusBadge
            $tone={statusBadge.tone}
            $filled={statusBadge.filled}
          >
            <StyledStatusBadgeLabel>{statusBadge.label}</StyledStatusBadgeLabel>
          </StyledStatusBadge>
        </StyledStatusStack>
      </StyledLastCell>
    </StyledDriverTableRow>
  );
}

export default LeaderboardDriverRow;
