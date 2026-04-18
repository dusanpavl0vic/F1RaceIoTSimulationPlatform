import { RaceStateSessionView } from "@/features/store/race-state/raceStateTypes";
import { RaceStateWebSocketStatus } from "@/features/store/race-state/raceStateUiSlice";
import { Box } from "@mui/material";
import {
  StyledHeroPaper,
  StyledHeroSubtitle,
  StyledHeroTitle,
} from "./dashboard-hero.styles";

type DashboardHeroProps = {
  wsStatus: RaceStateWebSocketStatus;
  session: RaceStateSessionView | null | undefined;
};

export function DashboardHero({ wsStatus, session }: DashboardHeroProps) {
  return (
    <StyledHeroPaper>
      <Box>
        <StyledHeroTitle>F1 RACE CONTROL</StyledHeroTitle>
        <StyledHeroSubtitle>LIVE TIMING MONITOR</StyledHeroSubtitle>
      </Box>
    </StyledHeroPaper>
  );
}
