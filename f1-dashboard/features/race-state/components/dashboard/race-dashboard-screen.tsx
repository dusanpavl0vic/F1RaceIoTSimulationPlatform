"use client";

import { CircularProgress, Typography } from "@mui/material";
import { useRaceDashboardLive } from "@/features/race-state/hooks/use-race-dashboard-live";
import { useDevice } from "@/hooks/use-device";
import { DashboardHero } from "./dashboard-hero";
import { DashboardSessionCards } from "./dashboard-session-cards";
import { LeaderboardTable } from "@/features/race-state/components/leaderboard/leaderboard-table";
import { LiveTransportPanel } from "./live-transport-panel";
import {
  StyledDashboardShell,
  StyledMainGrid,
  StyledSideStack,
} from "./race-dashboard-screen.styles";

export function RaceDashboardScreen() {
  const { isMobile, isTablet } = useDevice();
  const { data, isLoading, isError, isFetching, wsUi, sessionCards, leaderboardRows } =
    useRaceDashboardLive();

  if (isLoading) {
    return (
      <StyledDashboardShell>
        <CircularProgress size={24} />
        <Typography>Loading dashboard...</Typography>
      </StyledDashboardShell>
    );
  }

  if (isError || !data) {
    return (
      <StyledDashboardShell>
        <Typography variant="h4">Dashboard unavailable</Typography>
        <Typography color="text.secondary">
          Race state service trenutno ne vraca podatke.
        </Typography>
      </StyledDashboardShell>
    );
  }

  return (
    <StyledDashboardShell>
      <DashboardHero wsStatus={wsUi.wsStatus} session={data.session} />
      <DashboardSessionCards cards={sessionCards} />

      <StyledMainGrid>
        <LeaderboardTable rows={leaderboardRows} isMobile={isMobile} isTablet={isTablet} />

        <StyledSideStack>
          <LiveTransportPanel wsUi={wsUi} isFetching={isFetching} />
        </StyledSideStack>
      </StyledMainGrid>
    </StyledDashboardShell>
  );
}
