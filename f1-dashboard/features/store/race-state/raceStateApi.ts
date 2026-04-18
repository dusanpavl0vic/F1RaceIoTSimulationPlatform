import type {
  RaceCurrentState,
  RaceDashboard,
} from "@/features/store/race-state/raceStateTypes";
import { baseApi } from "../baseApi";

export const raceStateApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getDashboard: builder.query<RaceDashboard, void>({
      query: () => "/api/race-state/dashboard",
      providesTags: ["RaceState"],
    }),
    getCurrent: builder.query<RaceCurrentState, void>({
      query: () => "/api/race-state/current",
      providesTags: ["RaceState"],
    }),
    getLeaderboard: builder.query<RaceDashboard["leaderboard"], void>({
      query: () => "/api/race-state/leaderboard",
      transformResponse: (response: { drivers?: RaceDashboard["leaderboard"]; Drivers?: RaceDashboard["leaderboard"] }) =>
        response.drivers ?? response.Drivers ?? [],
      providesTags: ["RaceState"],
    }),
  }),
});

export const {
  useGetCurrentQuery,
  useGetDashboardQuery,
  useGetLeaderboardQuery,
} = raceStateApi;
