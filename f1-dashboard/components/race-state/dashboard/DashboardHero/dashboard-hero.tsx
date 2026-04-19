"use client";

import type { RaceStateSessionView } from "@/features/store/race-state/raceStateTypes";
import type { RaceStateWebSocketStatus } from "@/features/store/race-state/raceStateUiSlice";
import { usePathname, useRouter } from "next/navigation";
import {
  StyledHeroActionButton,
  StyledHeroActions,
  StyledHeroPaper,
  StyledHeroSubtitle,
  StyledHeroTextBlock,
  StyledHeroTitle,
} from "./dashboard-hero.styles";

type DashboardHeroProps = {
  wsStatus?: RaceStateWebSocketStatus;
  session: RaceStateSessionView | null | undefined;
};

export const DashboardHero = ({ wsStatus, session }: DashboardHeroProps) => {
  const router = useRouter();
  const pathname = usePathname();

  return (
    <StyledHeroPaper>
      <StyledHeroTextBlock>
        <StyledHeroTitle>F1 RACE CONTROL</StyledHeroTitle>
        <StyledHeroSubtitle>
          {session?.sessionId ?? "LIVE TIMING MONITOR"}
          {wsStatus ? ` · ${wsStatus.toUpperCase()}` : ""}
        </StyledHeroSubtitle>
      </StyledHeroTextBlock>

      <StyledHeroActions>
        <StyledHeroActionButton
          $active={pathname === "/"}
          variant={pathname === "/" ? "contained" : "outlined"}
          onClick={() => router.push("/")}
        >
          Dashboard
        </StyledHeroActionButton>
        <StyledHeroActionButton
          $active={pathname === "/telemetry"}
          variant={pathname === "/telemetry" ? "contained" : "outlined"}
          onClick={() => router.push("/telemetry")}
        >
          Telemetry
        </StyledHeroActionButton>
      </StyledHeroActions>
    </StyledHeroPaper>
  );
};
