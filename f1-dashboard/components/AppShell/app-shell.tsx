import { AppContainer } from "@/components/AppContainer/app-container";
import { Box } from "@mui/material";
import { AppFooter } from "../layout/footer/app-footer";
import { AppHeader } from "../layout/header/app-header";

export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <Box>
      <AppHeader />
      <AppContainer>{children}</AppContainer>
      <AppFooter />
    </Box>
  );
}
