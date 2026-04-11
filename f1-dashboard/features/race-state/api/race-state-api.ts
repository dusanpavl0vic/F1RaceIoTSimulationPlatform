import { baseApi } from "@/features/race-state/api/base-api";
import type {
  RaceCurrentState,
  RaceDashboard,
  RaceMapPosition,
} from "@/features/race-state/types/race-state";

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
    getMap: builder.query<RaceMapPosition[], void>({
      query: () => "/api/race-state/map",
      transformResponse: (response: { positions?: RaceMapPosition[]; Positions?: RaceMapPosition[] }) =>
        response.positions ?? response.Positions ?? [],
      providesTags: ["RaceState"],
    }),
  }),
});

export const {
  useGetCurrentQuery,
  useGetDashboardQuery,
  useGetLeaderboardQuery,
  useGetMapQuery,
} = raceStateApi;
