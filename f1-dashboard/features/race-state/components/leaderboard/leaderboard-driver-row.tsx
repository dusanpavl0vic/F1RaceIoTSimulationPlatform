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
  const secondarySpeedReading =
    (row.speeds?.ST as { Value?: string } | undefined)?.Value ??
    (row.speeds?.FL as { Value?: string } | undefined)?.Value ??
    (row.speeds?.I1 as { Value?: string } | undefined)?.Value ??
    (row.speeds?.I2 as { Value?: string } | undefined)?.Value ??
    null;

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
      <TableCell sx={{ width: 60, color: row.line || row.position ? "#fff" : "rgba(255,255,255,0.45)" }}>
        {row.line ?? row.position ?? row.gridPosition ?? "-"}
      </TableCell>
      <TableCell sx={{ minWidth: 150 }}>
        <Stack spacing={0.25}>
          <Typography fontWeight={800} sx={{ color: teamColor }}>
            {row.driverLabel}
          </Typography>
          <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.55)" }}>
            #{row.driverNumber} {row.trackPosition?.isEstimated ? "estimated" : ""}
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
        <Stack spacing={0.5}>
          <Typography fontWeight={700}>{row.speed ?? "-"}</Typography>
          {!isTablet && secondarySpeedReading ? (
            <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.55)" }}>
              {secondarySpeedReading}
            </Typography>
          ) : null}
          {!isTablet && row.gear !== null ? (
            <Stack direction="row" spacing={0.5} flexWrap="wrap">
              <Chip size="small" label={`G${row.gear}`} variant="outlined" />
              {row.drs !== null ? (
                <Chip
                  size="small"
                  label={`DRS ${row.drs}`}
                  color={row.drs ? "success" : "default"}
                  variant="outlined"
                />
              ) : null}
            </Stack>
          ) : null}
        </Stack>
      </TableCell>
    </TableRow>
  );
}
