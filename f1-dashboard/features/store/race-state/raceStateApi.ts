import type {
  RaceDashboard,
  RaceTyreStintStrategy,
} from "@/features/store/race-state/raceStateTypes";
import { buildMockRaceDashboardSnapshot } from "@/mocks/dashboard/race-state-dashboard.mock";
import { baseApi } from "../baseApi";

const useDashboardMocks =
  process.env.NEXT_PUBLIC_USE_DASHBOARD_MOCKS === "true";

const tyreCompounds = ["MEDIUM", "HARD", "SOFT"] as const;

const buildMockTyreStintStrategy = (
  sessionId: string,
): RaceTyreStintStrategy => {
  const mockSnapshot = buildMockRaceDashboardSnapshot(0);
  const totalLaps = mockSnapshot.dashboard.session.totalLaps ?? 53;

  return {
    sessionId,
    totalLaps,
    drivers: mockSnapshot.dashboard.leaderboard.map((driver, index) => {
      const firstLength = 14 + (index % 8);
      const secondLength = 18 + (index % 6);
      const thirdStart = firstLength + secondLength + 1;
      const stints = [
        {
          stintNumber: 1,
          compound: tyreCompounds[index % tyreCompounds.length],
          tyreIsNew: true,
          startLap: 1,
          endLap: firstLength,
          lapCount: firstLength,
        },
        {
          stintNumber: 2,
          compound: tyreCompounds[(index + 1) % tyreCompounds.length],
          tyreIsNew: true,
          startLap: firstLength + 1,
          endLap: firstLength + secondLength,
          lapCount: secondLength,
        },
        {
          stintNumber: 3,
          compound: tyreCompounds[(index + 2) % tyreCompounds.length],
          tyreIsNew: false,
          startLap: thirdStart,
          endLap: totalLaps,
          lapCount: Math.max(1, totalLaps - thirdStart + 1),
        },
      ];

      return {
        driverNumber: driver.driverNumber,
        driverName: driver.tla ?? driver.broadcastName ?? `#${driver.driverNumber}`,
        teamName: driver.teamName,
        teamColor: driver.teamColor,
        gridPosition: driver.gridPosition,
        position: driver.position,
        stints,
      };
    }),
  };
};

export const raceStateApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getDashboard: builder.query<RaceDashboard, void>({
      async queryFn(_arg, _api, _extraOptions, fetchWithBQ) {
        if (useDashboardMocks) {
          return { data: buildMockRaceDashboardSnapshot(0).dashboard };
        }

        const result = await fetchWithBQ({
          url: "/api/race-state/dashboard",
          method: "GET",
        });

        if (result.error) {
          return { error: result.error };
        }

        return { data: result.data as RaceDashboard };
      },
      providesTags: ["RaceState"],
    }),
    getLeaderboard: builder.query<RaceDashboard["leaderboard"], void>({
      query: () => "/api/race-state/leaderboard",
      transformResponse: (response: { drivers?: RaceDashboard["leaderboard"]; Drivers?: RaceDashboard["leaderboard"] }) =>
        response.drivers ?? response.Drivers ?? [],
      providesTags: ["RaceState"],
    }),
    getTyreStintStrategy: builder.query<RaceTyreStintStrategy, string>({
      async queryFn(sessionId, _api, _extraOptions, fetchWithBQ) {
        if (useDashboardMocks) {
          return { data: buildMockTyreStintStrategy(sessionId) };
        }

        const searchParams = new URLSearchParams();
        searchParams.set("sessionId", sessionId);

        const result = await fetchWithBQ({
          url: `/api/race-state/analytics/tyres/stints?${searchParams.toString()}`,
          method: "GET",
        });

        if (result.error) {
          return { error: result.error };
        }

        return { data: result.data as RaceTyreStintStrategy };
      },
      providesTags: ["RaceState"],
    }),
  }),
});

export const {
  useGetDashboardQuery,
  useGetLeaderboardQuery,
  useGetTyreStintStrategyQuery,
} = raceStateApi;
