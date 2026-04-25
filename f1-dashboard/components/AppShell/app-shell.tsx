import { RaceStateLiveBridge } from "@/components/providers/race-state-live-bridge";
import { BattleAlertsOverlay } from "@/components/race-state/dashboard/BattleAlertsOverlay/battle-alerts-overlay";
import { AppContainer } from "@/components/AppContainer/app-container";
import { Box } from "@mui/material";
import { AppFooter } from "../layout/footer/app-footer";
import { AppHeader } from "../layout/header/app-header";

export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <Box>
      <RaceStateLiveBridge />
      <AppHeader />
      <Box
        sx={{
          width: "100%",
          maxWidth: 1420,
          mx: "auto",
          px: { xs: 1.5, sm: 2, lg: 3 },
          py: { xs: 1.5, lg: 2.5 },
          display: "grid",
          gridTemplateColumns: { xs: "1fr", lg: "minmax(0, 1fr) 340px" },
          gap: { xs: 1.5, lg: 2.5 },
          alignItems: "start",
        }}
      >
        <AppContainer>{children}</AppContainer>
        <BattleAlertsOverlay />
      </Box>
      <AppFooter />
    </Box>
  );
}
