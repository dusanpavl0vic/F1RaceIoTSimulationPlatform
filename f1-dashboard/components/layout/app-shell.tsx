import { AppNavigation } from "@/components/layout/app-navigation";

export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="app-shell">
      <AppNavigation />
      <div className="app-shell__content">{children}</div>
    </div>
  );
}
