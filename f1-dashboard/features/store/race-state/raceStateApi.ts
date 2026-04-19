import type {
  RaceDashboard,
} from "@/features/store/race-state/raceStateTypes";
import { baseApi } from "../baseApi";

export const raceStateApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getLeaderboard: builder.query<RaceDashboard["leaderboard"], void>({
      query: () => "/api/race-state/leaderboard",
      transformResponse: (response: { drivers?: RaceDashboard["leaderboard"]; Drivers?: RaceDashboard["leaderboard"] }) =>
        response.drivers ?? response.Drivers ?? [],
      providesTags: ["RaceState"],
    }),
  }),
});

export const {
  useGetLeaderboardQuery,
} = raceStateApi;
