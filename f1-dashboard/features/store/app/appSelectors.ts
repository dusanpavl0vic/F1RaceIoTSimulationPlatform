import type { RootState } from "@/features/store/store";

export const selectColorMode = (state: RootState) => state.appUi.colorMode;
