import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import type { RaceDashboardDriverRow } from "@/features/race-state/types/race-state";
import { LeaderboardDriverRow } from "@/features/race-state/components/leaderboard/leaderboard-driver-row";

type LeaderboardTableProps = {
  rows: RaceDashboardDriverRow[];
  isMobile: boolean;
  isTablet: boolean;
};

export function LeaderboardTable({
  rows,
  isMobile,
  isTablet,
}: LeaderboardTableProps) {
  return (
    <Paper
      sx={{
        p: 0,
        borderRadius: 3,
        overflow: "hidden",
        backgroundColor: "#050505",
        border: "1px solid rgba(255,255,255,0.08)",
        boxShadow: "0 24px 80px rgba(0,0,0,0.45)",
      }}
    >
      <Box
        sx={{
          px: 2.5,
          py: 1.75,
          borderBottom: "1px solid rgba(255,255,255,0.08)",
          background:
            "linear-gradient(90deg, rgba(225,6,0,0.18) 0%, rgba(225,6,0,0.03) 35%, rgba(0,0,0,0) 100%)",
        }}
      >
        <Typography variant="h5" sx={{ color: "#fff", fontWeight: 900, letterSpacing: "0.04em" }}>
          LEADERBOARD
        </Typography>
        <Typography variant="body2" sx={{ color: "rgba(255,255,255,0.6)" }}>
          Live timing tabela: redosled, gap, gume i status u trci.
        </Typography>
      </Box>

      <TableContainer>
        <Table
          size={isMobile ? "small" : "medium"}
          sx={{
            "& .MuiTableCell-root": {
              borderColor: "rgba(255,255,255,0.08)",
            },
          }}
        >
          <TableHead>
            <TableRow
              sx={{
                backgroundColor: "#0f1117",
                "& th": {
                  color: "rgba(255,255,255,0.72)",
                  fontWeight: 700,
                  letterSpacing: "0.06em",
                  textTransform: "uppercase",
                  fontSize: "0.72rem",
                },
              }}
            >
              <TableCell>Pos</TableCell>
              <TableCell>Driver</TableCell>
              {!isMobile && <TableCell>Team</TableCell>}
              <TableCell>Gap</TableCell>
              {!isTablet && <TableCell>Interval</TableCell>}
              {!isMobile && <TableCell>Tyre</TableCell>}
              <TableCell>Last Lap</TableCell>
              {!isTablet && <TableCell>Best Lap</TableCell>}
              <TableCell>Status</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <LeaderboardDriverRow
                key={row.driverNumber}
                row={row}
                isMobile={isMobile}
                isTablet={isTablet}
              />
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  );
}
