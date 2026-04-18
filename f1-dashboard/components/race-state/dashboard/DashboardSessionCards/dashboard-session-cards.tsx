import type { SessionCard } from "@/features/store/race-state/raceStateTypes";
import { Box } from "@mui/material";
import {
  StyledCardEyebrow,
  StyledCardLabel,
  StyledCardsShell,
  StyledCardValue,
  StyledHeroCard,
  StyledHeroHeader,
  StyledHeroValue,
  StyledHeroValueContainer,
  StyledMetricCard,
  StyledSessionCard,
} from "./dashboard-session-cards.styles";

type DashboardSessionCardsProps = {
  cards: SessionCard[];
};

export const DashboardSessionCards = ({
  cards,
}: DashboardSessionCardsProps) => {
  const primaryCard =
    cards.find((card) => card.label.toLowerCase() === "session") ?? cards[0];
  const secondaryCards = cards.filter(
    (card) => card.label !== primaryCard?.label,
  );

  return (
    <StyledCardsShell>
      <StyledHeroCard>
        <StyledHeroHeader>
          <Box>
            <StyledCardEyebrow>LIVE SESSION</StyledCardEyebrow>
            <StyledCardLabel>
              {primaryCard?.label.toUpperCase() ?? "SESSION"}
            </StyledCardLabel>
          </Box>
          <StyledHeroValueContainer>
            <StyledHeroValue>{primaryCard?.value ?? "—"}</StyledHeroValue>
            {secondaryCards.map((card) => (
              <StyledMetricCard key={card.label}>
                <StyledSessionCard>
                  <StyledCardLabel>{card.label.toUpperCase()}</StyledCardLabel>
                  <StyledCardValue>{card.value}</StyledCardValue>
                </StyledSessionCard>
              </StyledMetricCard>
            ))}
          </StyledHeroValueContainer>
        </StyledHeroHeader>
      </StyledHeroCard>
    </StyledCardsShell>
  );
};
