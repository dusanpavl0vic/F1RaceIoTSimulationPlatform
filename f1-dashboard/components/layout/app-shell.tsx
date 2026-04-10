import { AppContainer } from "@/components/layout/app-container";
import { AppFooter } from "./footer/app-footer";
import { AppHeader } from "./header/app-header";

export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="app-shell">
      <AppHeader />
      <AppContainer>
        <div className="app-shell__content">{children}</div>
      </AppContainer>
      <AppFooter />
    </div>
  );
}
