import { baseApi } from "@/features/race-state/api/base-api";
import type { RaceDashboard } from "@/features/race-state/types/race-state";

export const raceStateApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getDashboard: builder.query<RaceDashboard, void>({
      query: () => "/api/race-state/dashboard",
      providesTags: ["RaceState"],
    }),
    getLeaderboard: builder.query<RaceDashboard["leaderboard"], void>({
      query: () => "/api/race-state/leaderboard",
      transformResponse: (response: { drivers?: RaceDashboard["leaderboard"]; Drivers?: RaceDashboard["leaderboard"] }) =>
        response.drivers ?? response.Drivers ?? [],
      providesTags: ["RaceState"],
    }),
    getMap: builder.query<RaceDashboard["mapPositions"], void>({
      query: () => "/api/race-state/map",
      transformResponse: (response: { positions?: RaceDashboard["mapPositions"]; Positions?: RaceDashboard["mapPositions"] }) =>
        response.positions ?? response.Positions ?? [],
      providesTags: ["RaceState"],
    }),
  }),
});

export const {
  useGetDashboardQuery,
  useGetLeaderboardQuery,
  useGetMapQuery,
} = raceStateApi;
