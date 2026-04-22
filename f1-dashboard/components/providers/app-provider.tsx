"use client";

import { AppThemeProvider } from "@/components/providers/app-theme-provider";
import { store } from "@/features/store/store";
import { Provider } from "react-redux";

export function AppProvider({ children }: { children: React.ReactNode }) {
  return (
    <Provider store={store}>
      <AppThemeProvider>{children}</AppThemeProvider>
    </Provider>
  );
}
