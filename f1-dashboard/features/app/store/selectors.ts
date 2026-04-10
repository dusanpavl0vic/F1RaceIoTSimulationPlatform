import type { RootState } from "@/store";

export const selectColorMode = (state: RootState) => state.appUi.colorMode;
