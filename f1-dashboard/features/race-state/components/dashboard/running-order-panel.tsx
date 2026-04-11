"use client";

import { Paper, Stack, Typography } from "@mui/material";
import type { RaceDashboardDriverRow } from "@/features/race-state/types/race-state";

type RunningOrderPanelProps = {
  rows: RaceDashboardDriverRow[];
};

export function RunningOrderPanel({ rows }: RunningOrderPanelProps) {
  return (
    <Paper
      sx={{
        p: 2.25,
        borderRadius: 3,
        backgroundColor: "#050505",
        border: "1px solid rgba(255,255,255,0.08)",
      }}
    >
      <Typography variant="h6" sx={{ color: "#fff", fontWeight: 900, mb: 0.5 }}>
        Running Order
      </Typography>
      <Typography variant="body2" sx={{ color: "rgba(255,255,255,0.58)", mb: 2 }}>
        Trenutni poredak vozaca kako ga servis trenutno vidi.
      </Typography>

      <Stack spacing={0.75}>
        {rows.map((row) => {
          const teamColor = row.teamColor ?? "#f7f7f7";

          return (
            <Stack
              key={row.driverNumber}
              direction="row"
              alignItems="center"
              justifyContent="space-between"
              spacing={1.25}
              sx={{
                px: 1.25,
                py: 0.9,
                borderRadius: 2,
                backgroundColor: "rgba(255,255,255,0.03)",
              }}
            >
              <Stack direction="row" spacing={1.1} alignItems="center">
                <Typography
                  sx={{
                    minWidth: 28,
                    color: row.line || row.position || row.gridPosition ? "#fff" : "rgba(255,255,255,0.42)",
                    fontWeight: 900,
                  }}
                >
                  {row.line ?? row.position ?? row.gridPosition ?? "-"}
                </Typography>
                <Stack spacing={0.1}>
                  <Typography sx={{ color: teamColor, fontWeight: 900 }}>
                    {row.driverLabel}
                  </Typography>
                  <Typography sx={{ color: "rgba(255,255,255,0.54)", fontSize: 12 }}>
                    #{row.driverNumber} {row.displayTeamName}
                  </Typography>
                </Stack>
              </Stack>

              <Stack spacing={0.1} alignItems="flex-end">
                <Typography sx={{ color: "#fff", fontWeight: 700, fontSize: 13 }}>
                  {row.gapToLeader ?? "-"}
                </Typography>
                <Typography sx={{ color: "rgba(255,255,255,0.54)", fontSize: 12 }}>
                  {row.statusLabel}
                </Typography>
              </Stack>
            </Stack>
          );
        })}
      </Stack>
    </Paper>
  );
}
