import { Chip, Stack, TableCell, TableRow, Typography } from "@mui/material";
import type { RaceDashboardDriverRow } from "@/features/race-state/types/race-state";
import { LeaderboardTyreCell } from "@/features/race-state/components/leaderboard/leaderboard-tyre-cell";

type LeaderboardDriverRowProps = {
  row: RaceDashboardDriverRow;
  isMobile: boolean;
  isTablet: boolean;
};

export function LeaderboardDriverRow({
  row,
  isMobile,
  isTablet,
}: LeaderboardDriverRowProps) {
  const teamColor = row.teamColor ?? "#f5f5f5";

  return (
    <TableRow
      hover
      sx={{
        "& td": {
          borderBottom: "1px solid rgba(255,255,255,0.06)",
          color: "#f7f7f7",
          py: 1.1,
        },
        "&:hover": {
          backgroundColor: "rgba(255,255,255,0.04)",
        },
      }}
    >
      <TableCell sx={{ width: 60, color: row.position || row.gridPosition || row.line ? "#fff" : "rgba(255,255,255,0.45)" }}>
        {row.position ?? row.gridPosition ?? row.line ?? "-"}
      </TableCell>
        <TableCell sx={{ minWidth: 150 }}>
          <Stack spacing={0.25}>
            <Typography fontWeight={800} sx={{ color: teamColor }}>
              {row.driverLabel}
            </Typography>
            <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.55)" }}>
              #{row.driverNumber}
            </Typography>
          </Stack>
        </TableCell>

      {!isMobile && (
        <TableCell sx={{ color: "rgba(255,255,255,0.7)" }}>{row.displayTeamName}</TableCell>
      )}

      <TableCell>{row.gapToLeader ?? "-"}</TableCell>

      {!isTablet && <TableCell>{row.intervalToPositionAhead ?? "-"}</TableCell>}

      {!isMobile && (
        <TableCell>
          <LeaderboardTyreCell row={row} />
        </TableCell>
      )}

      <TableCell>{row.lastLapTime ?? "-"}</TableCell>

      {!isTablet && <TableCell>{row.bestLapTime ?? "-"}</TableCell>}

      <TableCell>
        <Stack direction="row" spacing={0.5} flexWrap="wrap">
          {row.pitFlag !== "-" ? (
            <Chip size="small" label={row.pitFlag} color="warning" variant="outlined" />
          ) : null}
          <Chip
            size="small"
            label={row.statusLabel}
            color={row.statusLabel === "RET" || row.statusLabel === "STOP" ? "error" : "default"}
            variant="outlined"
          />
        </Stack>
      </TableCell>
    </TableRow>
  );
}
