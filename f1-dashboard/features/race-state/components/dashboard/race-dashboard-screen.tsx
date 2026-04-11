"use client";

import {
  CircularProgress,
  Grid,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useRaceDashboardLive } from "@/features/race-state/hooks/use-race-dashboard-live";
import { useDevice } from "@/hooks/use-device";
import { DashboardHero } from "@/features/race-state/components/dashboard/dashboard-hero";
import { DashboardSessionCards } from "@/features/race-state/components/dashboard/dashboard-session-cards";
import { LeaderboardTable } from "@/features/race-state/components/leaderboard/leaderboard-table";
import { LiveTransportPanel } from "@/features/race-state/components/dashboard/live-transport-panel";

export function RaceDashboardScreen() {
  const { isMobile, isTablet } = useDevice();
  const {
    data,
    isLoading,
    isError,
    isFetching,
    wsUi,
    sessionCards,
    leaderboardRows,
  } = useRaceDashboardLive();

  if (isLoading) {
    return (
      <Stack className="page-shell" alignItems="center" spacing={2}>
        <CircularProgress />
        <Typography>Loading dashboard...</Typography>
      </Stack>
    );
  }

  if (isError || !data) {
    return (
      <div className="page-shell">
        <Paper className="hero-panel">
          <Typography variant="h4" gutterBottom>
            Dashboard unavailable
          </Typography>
          <Typography color="text.secondary">
            Race state service trenutno ne vraca podatke.
          </Typography>
        </Paper>
      </div>
    );
  }

  return (
    <div className="page-shell">
      <DashboardHero wsStatus={wsUi.wsStatus} session={data.session} />

      <DashboardSessionCards cards={sessionCards} />

      <Grid container spacing={2.5} sx={{ mt: 0.5 }}>
        <Grid size={{ xs: 12, xl: 9 }}>
          <LeaderboardTable
            rows={leaderboardRows}
            isMobile={isMobile}
            isTablet={isTablet}
          />
        </Grid>

        <Grid size={{ xs: 12, xl: 3 }}>
          <Stack spacing={2.5}>
            <LiveTransportPanel wsUi={wsUi} isFetching={isFetching} />
          </Stack>
        </Grid>
      </Grid>
    </div>
  );
}
