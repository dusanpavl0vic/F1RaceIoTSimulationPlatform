import type { RootState } from "@/features/store/store";
import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

export type ColorMode = "light" | "dark";

type AppUiState = {
  colorMode: ColorMode;
};

const initialState: AppUiState = {
  colorMode: "light",
};

const appUiSlice = createSlice({
  name: "appUi",
  initialState,
  reducers: {
    setColorMode(state, action: PayloadAction<ColorMode>) {
      state.colorMode = action.payload;
    },
    toggleColorMode(state) {
      state.colorMode = state.colorMode === "light" ? "dark" : "light";
    },
  },
});

export const { setColorMode, toggleColorMode } = appUiSlice.actions;
export const appUiReducer = appUiSlice.reducer;


export const selectColorMode = (state: RootState) => state.appUi.colorMode;
