import { configureStore } from "@reduxjs/toolkit";
import { setupListeners } from "@reduxjs/toolkit/query";
import { appUiReducer } from "@/features/app/store/app-ui-slice";
import { baseApi } from "@/features/race-state/api/base-api";
import { raceStateUiReducer } from "@/features/race-state/store/race-state-ui-slice";

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
