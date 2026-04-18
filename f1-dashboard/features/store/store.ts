import { appUiReducer } from "@/features/store/app/appUiSlice";
import { raceStateUiReducer } from "@/features/store/race-state/raceStateUiSlice";
import { configureStore } from "@reduxjs/toolkit";
import { setupListeners } from "@reduxjs/toolkit/query";
import { baseApi } from "./baseApi";

export const store = configureStore({
  reducer: {
    appUi: appUiReducer,
    raceStateUi: raceStateUiReducer,
    [baseApi.reducerPath]: baseApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(baseApi.middleware),
});

setupListeners(store.dispatch);

export type AppStore = typeof store;
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
