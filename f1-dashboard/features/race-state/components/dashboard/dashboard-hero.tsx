import type { RaceStateWebSocketStatus } from "@/features/race-state/store/race-state-ui-slice";
import type { RaceStateSessionView } from "@/features/race-state/types/race-state";
import {
  StyledHeroPaper,
  StyledHeroStatus,
  StyledHeroSubtitle,
  StyledHeroTitle,
  StyledLapBadge,
  StyledWsBadge,
  StyledWsDot,
  StyledWsLabel,
} from "./dashboard-hero.styles";

type DashboardHeroProps = {
  wsStatus: RaceStateWebSocketStatus;
  session: RaceStateSessionView | null | undefined;
};

export function DashboardHero({ wsStatus, session }: DashboardHeroProps) {
  const lapLabel =
    session?.currentLap && session?.totalLaps
      ? `LAP ${session.currentLap} / ${session.totalLaps}`
      : "LAP — / —";

  const isLive = wsStatus === "open";

  return (
    <StyledHeroPaper>
      <div>
        <StyledHeroTitle>F1 RACE CONTROL</StyledHeroTitle>
        <StyledHeroSubtitle>LIVE TIMING MONITOR</StyledHeroSubtitle>
      </div>

      <StyledHeroStatus>
        <StyledLapBadge>{lapLabel}</StyledLapBadge>
        <StyledWsBadge $live={isLive}>
          <StyledWsDot $live={isLive} />
          <StyledWsLabel $live={isLive}>{wsStatus.toUpperCase()}</StyledWsLabel>
        </StyledWsBadge>
      </StyledHeroStatus>
    </StyledHeroPaper>
  );
}
