import type { RaceDashboardDriverRow } from "@/features/store/race-state/raceStateTypes";
import { Chip, Stack, TableCell } from "@mui/material";
import { useEffect, useRef, useState } from "react";
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
} from "./leaderboard-driver-row.styles";
import { LeaderboardTyreCell } from "./leaderboard-tyre-cell";

type LeaderboardDriverRowProps = {
  row: RaceDashboardDriverRow;
  isMobile: boolean;
  isTablet: boolean;
  positionChange: "gained" | "lost" | null;
};

const resolveStatusChipProps = (statusLabel: string) => {
  switch (statusLabel) {
    case "RET":
      return {
        label: "RET",
        color: "error" as const,
        variant: "filled" as const,
      };
    case "STOP":
      return {
        label: "STOP",
        color: "error" as const,
        variant: "outlined" as const,
      };
    case "PIT":
      return {
        label: "PIT",
        color: "warning" as const,
        variant: "filled" as const,
      };
    case "OUT":
      return {
        label: "OUT",
        color: "warning" as const,
        variant: "outlined" as const,
      };
    case "RUN":
      return {
        label: "RUN",
        color: "success" as const,
        variant: "outlined" as const,
      };
    default:
      return {
        label: statusLabel === "-" ? "—" : statusLabel,
        color: "default" as const,
        variant: "outlined" as const,
      };
  }
};

export function LeaderboardDriverRow({
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
  const statusProps = resolveStatusChipProps(row.statusLabel);

  const [flash, setFlash] = useState<"gained" | "lost" | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!positionChange) return;
    setFlash(positionChange);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => setFlash(null), 1600);
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [positionChange]);

  return (
    <StyledDriverTableRow hover $dimmed={isDimmed} $flash={flash}>
      <StyledPosCell $isLeader={pos === 1}>
        {pos ?? "—"}
        {flash && (
          <StyledPosArrow $direction={flash}>
            {flash === "gained" ? "▲" : "▼"}
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
        <Stack direction="row" spacing={0.5}>
          {row.pitFlag !== "-" && (
            <Chip
              size="small"
              label={row.pitFlag}
              color="warning"
              variant="filled"
            />
          )}
          <Chip size="small" {...statusProps} />
        </Stack>
      </StyledLastCell>
    </StyledDriverTableRow>
  );
}
