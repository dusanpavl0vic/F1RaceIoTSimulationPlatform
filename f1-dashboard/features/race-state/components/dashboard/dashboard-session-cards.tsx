import { Grid, Paper, Typography } from "@mui/material";
import type { SessionCard } from "@/features/race-state/types/race-state";

type DashboardSessionCardsProps = {
  cards: SessionCard[];
};

export function DashboardSessionCards({
  cards,
}: DashboardSessionCardsProps) {
  return (
    <Grid container spacing={2.5} sx={{ mt: 0.5 }}>
      {cards.map((card) => (
        <Grid key={card.label} size={{ xs: 12, sm: 6, lg: 3 }}>
          <Paper
            sx={{
              p: 2.25,
              borderRadius: 3,
              backgroundColor: "#08090d",
              border: "1px solid rgba(255,255,255,0.08)",
            }}
          >
            <Typography variant="overline" sx={{ color: "rgba(255,255,255,0.54)" }}>
              {card.label}
            </Typography>
            <Typography
              variant={card.label === "Session" ? "body1" : "h5"}
              sx={{ mt: 1, fontWeight: 900, wordBreak: "break-word", color: "#fff" }}
            >
              {card.value}
            </Typography>
          </Paper>
        </Grid>
      ))}
    </Grid>
  );
}
