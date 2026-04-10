import { Chip, Paper, Stack, Typography } from "@mui/material";
import type { RaceStateWebSocketStatus } from "@/features/race-state/store/race-state-ui-slice";

type DashboardHeroProps = {
  wsStatus: RaceStateWebSocketStatus;
};

export function DashboardHero({ wsStatus }: DashboardHeroProps) {
  return (
    <Paper
      className="hero-panel"
      sx={{
        background:
          "linear-gradient(135deg, rgba(225,6,0,0.2) 0%, rgba(17,20,28,0.96) 45%, rgba(5,5,5,0.98) 100%)",
        border: "1px solid rgba(255,255,255,0.08)",
      }}
    >
      <Stack
        direction={{ xs: "column", md: "row" }}
        justifyContent="space-between"
        alignItems={{ xs: "flex-start", md: "center" }}
        spacing={2}
      >
        <div>
          <Typography variant="h3" sx={{ color: "#fff", fontWeight: 900 }}>
            Race Control Monitor
          </Typography>
          <Typography className="hero-copy" sx={{ color: "rgba(255,255,255,0.68)" }}>
            Live leaderboard i telemetry view preko race-state WS toka.
          </Typography>
        </div>

        <Stack direction="row" spacing={1.5} alignItems="center">
          <Chip
            color={wsStatus === "open" ? "success" : "default"}
            label={`WS: ${wsStatus}`}
            variant={wsStatus === "open" ? "filled" : "outlined"}
          />
        </Stack>
      </Stack>
    </Paper>
  );
}
