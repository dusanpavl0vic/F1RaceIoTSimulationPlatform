"use client";

import { Provider } from "react-redux";
import { AppOverlays } from "@/components/providers/app-overlays";
import { AppThemeProvider } from "@/components/providers/app-theme-provider";
import { store } from "@/store";

export function AppProvider({ children }: { children: React.ReactNode }) {
  return (
    <Provider store={store}>
      <AppThemeProvider>
        {children}
        <AppOverlays />
      </AppThemeProvider>
    </Provider>
  );
}
