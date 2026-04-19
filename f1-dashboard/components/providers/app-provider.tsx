"use client";

import { RaceStateBootstrap } from "@/components/providers/race-state-bootstrap";
import { AppThemeProvider } from "@/components/providers/app-theme-provider";
import { store } from "@/features/store/store";
import { Provider } from "react-redux";

export function AppProvider({ children }: { children: React.ReactNode }) {
  return (
    <Provider store={store}>
      <RaceStateBootstrap />
      <AppThemeProvider>{children}</AppThemeProvider>
    </Provider>
  );
}
