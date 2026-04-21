"use client";

import LeaderboardTable from "@/components/race-state/leaderboard/LeaderboardTable/leaderboard-table";
import { useDevice } from "@/hooks/use-device";
import { useRaceDashboardLive } from "@/hooks/useRaceDashboardLive";
import { CircularProgress, Typography } from "@mui/material";
import { DashboardHero } from "../DashboardHero/dashboard-hero";
import { DashboardSessionCards } from "../DashboardSessionCards/dashboard-session-cards";
import { LiveTransportPanel } from "../LiveTransportPanel/live-transport-panel";
import {
  StyledDashboardShell,
  StyledMainGrid,
  StyledSideStack,
} from "./race-dashboard-screen.styles";

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
        <LeaderboardTable
          rows={leaderboardRows}
          isMobile={isMobile}
          isTablet={isTablet}
        />

        <StyledSideStack>
          <LiveTransportPanel wsUi={wsUi} isFetching={isFetching} />
        </StyledSideStack>
      </StyledMainGrid>
    </StyledDashboardShell>
  );
}
