"use client";

import { Provider } from "react-redux";
import { AppOverlays } from "@/components/providers/app-overlays";
import { store } from "@/store";

export function AppProvider({ children }: { children: React.ReactNode }) {
  return (
    <Provider store={store}>
      {children}
      <AppOverlays />
    </Provider>
  );
}
