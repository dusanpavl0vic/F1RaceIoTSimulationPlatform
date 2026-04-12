import type { SessionCard } from "@/features/race-state/types/race-state";
import {
  StyledCardLabel,
  StyledCardValue,
  StyledCardsGrid,
  StyledSessionCard,
} from "./dashboard-session-cards.styles";

type DashboardSessionCardsProps = {
  cards: SessionCard[];
};

export function DashboardSessionCards({ cards }: DashboardSessionCardsProps) {
  return (
    <StyledCardsGrid>
      {cards.map((card) => (
        <StyledSessionCard key={card.label}>
          <StyledCardLabel>{card.label.toUpperCase()}</StyledCardLabel>
          <StyledCardValue $small={card.label === "Session"}>
            {card.value}
          </StyledCardValue>
        </StyledSessionCard>
      ))}
    </StyledCardsGrid>
  );
}
