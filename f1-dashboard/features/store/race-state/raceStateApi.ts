import type {
  DriverPredictionFeaturesResponse,
  NextLapPredictionFeatures,
  NextLapPredictionRequest,
  NextLapPredictionResponse,
  NextLapPredictionBatchRequest,
  NextLapPredictionBatchResponse,
  RaceDashboard,
  RaceTyreStintStrategy,
} from "@/features/store/race-state/raceStateTypes";
import { buildMockRaceDashboardSnapshot } from "@/mocks/dashboard/race-state-dashboard.mock";
import { baseApi } from "../baseApi";

const useDashboardMocks =
  process.env.NEXT_PUBLIC_USE_DASHBOARD_MOCKS === "true";
const predictionApiBaseUrl =
  process.env.NEXT_PUBLIC_PREDICTION_API_BASE_URL ?? "http://localhost:8087";
const replayApiBaseUrl =
  process.env.NEXT_PUBLIC_REPLAY_API_BASE_URL ?? "http://localhost:8080";

const tyreCompounds = ["MEDIUM", "HARD", "SOFT"] as const;

const normalizePredictionFeatures = (
  payload: Record<string, unknown> | null | undefined,
): NextLapPredictionFeatures | null => {
  if (!payload) {
    return null;
  }

  return {
    driver_number: Number(payload.driver_number ?? payload.driverNumber),
    lap_number: Number(payload.lap_number ?? payload.lapNumber),
    position: (payload.position as number | null | undefined) ?? null,
    lap_time_last: (payload.lap_time_last as number | null | undefined) ?? (payload.lapTimeLast as number | null | undefined) ?? null,
    lap_time_best: (payload.lap_time_best as number | null | undefined) ?? (payload.lapTimeBest as number | null | undefined) ?? null,
    lap_time_avg_last_3: (payload.lap_time_avg_last_3 as number | null | undefined) ?? (payload.lapTimeAvgLast3 as number | null | undefined) ?? null,
    lap_time_avg_last_5: (payload.lap_time_avg_last_5 as number | null | undefined) ?? (payload.lapTimeAvgLast5 as number | null | undefined) ?? null,
    gap_to_leader: (payload.gap_to_leader as number | null | undefined) ?? (payload.gapToLeader as number | null | undefined) ?? null,
    gap_to_ahead: (payload.gap_to_ahead as number | null | undefined) ?? (payload.gapToAhead as number | null | undefined) ?? null,
    stint_number: (payload.stint_number as number | null | undefined) ?? (payload.stintNumber as number | null | undefined) ?? null,
    tyre_compound: (payload.tyre_compound as string | null | undefined) ?? (payload.tyreCompound as string | null | undefined) ?? null,
    tyre_is_new: (payload.tyre_is_new as boolean | null | undefined) ?? (payload.tyreIsNew as boolean | null | undefined) ?? null,
    tyre_laps_on_set: (payload.tyre_laps_on_set as number | null | undefined) ?? (payload.tyreLapsOnSet as number | null | undefined) ?? null,
    in_pit: (payload.in_pit as boolean | null | undefined) ?? (payload.inPit as boolean | null | undefined) ?? null,
    avg_speed_last_lap: (payload.avg_speed_last_lap as number | null | undefined) ?? (payload.avgSpeedLastLap as number | null | undefined) ?? null,
    max_speed_last_lap: (payload.max_speed_last_lap as number | null | undefined) ?? (payload.maxSpeedLastLap as number | null | undefined) ?? null,
    avg_rpm_last_lap: (payload.avg_rpm_last_lap as number | null | undefined) ?? (payload.avgRpmLastLap as number | null | undefined) ?? null,
    avg_throttle_pct_last_lap: (payload.avg_throttle_pct_last_lap as number | null | undefined) ?? (payload.avgThrottlePctLastLap as number | null | undefined) ?? null,
    avg_raw_brake_last_lap: (payload.avg_raw_brake_last_lap as number | null | undefined) ?? (payload.avgRawBrakeLastLap as number | null | undefined) ?? null,
    drs_open_ratio_last_lap: (payload.drs_open_ratio_last_lap as number | null | undefined) ?? (payload.drsOpenRatioLastLap as number | null | undefined) ?? null,
    gear_changes_last_lap: (payload.gear_changes_last_lap as number | null | undefined) ?? (payload.gearChangesLastLap as number | null | undefined) ?? null,
  };
};

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
    getDriverPredictionFeatures: builder.mutation<
      DriverPredictionFeaturesResponse,
      number
    >({
      async queryFn(driverNumber, _api, _extraOptions, fetchWithBQ) {
        const result = await fetchWithBQ({
          url: `/api/race-state/prediction/drivers/${driverNumber}/next-lap-features`,
          method: "GET",
        });

        if (result.error) {
          return { error: result.error };
        }

        const response = result.data as DriverPredictionFeaturesResponse & {
          features?: Record<string, unknown> | null;
        };

        return {
          data: {
            ...response,
            features: normalizePredictionFeatures(response.features),
          },
        };
      },
    }),
    predictNextLapBatch: builder.mutation<
      NextLapPredictionBatchResponse,
      NextLapPredictionBatchRequest
    >({
      async queryFn(arg, _api, _extraOptions, fetchWithBQ) {
        if (useDashboardMocks) {
          return {
            data: {
              predictions: arg.items.map((item, index) => ({
                predicted_next_lap_time:
                  (item.lap_time_last ?? item.lap_time_best ?? 82) -
                  0.05 +
                  index * 0.012,
                model_version: "mock-model",
              })),
              model_version: "mock-model",
            },
          };
        }

        const result = await fetchWithBQ({
          url: `${predictionApiBaseUrl}/v1/predictions/next-lap/batch`,
          method: "POST",
          body: arg,
        });

        if (result.error) {
          return { error: result.error };
        }

        return { data: result.data as NextLapPredictionBatchResponse };
      },
    }),
    predictNextLap: builder.mutation<
      NextLapPredictionResponse,
      NextLapPredictionRequest
    >({
      async queryFn(arg, _api, _extraOptions, fetchWithBQ) {
        if (useDashboardMocks) {
          return {
            data: {
              predicted_next_lap_time:
                (arg.features.lap_time_last ?? arg.features.lap_time_best ?? 82) -
                0.05,
              model_version: "mock-model",
            },
          };
        }

        const result = await fetchWithBQ({
          url: `${predictionApiBaseUrl}/v1/predictions/next-lap`,
          method: "POST",
          body: arg,
        });

        if (result.error) {
          return { error: result.error };
        }

        return { data: result.data as NextLapPredictionResponse };
      },
    }),
    bootstrapReplay: builder.mutation<unknown, void>({
      async queryFn(_arg, _api, _extraOptions, fetchWithBQ) {
        const result = await fetchWithBQ({
          url: `${replayApiBaseUrl}/api/replay/bootstrap`,
          method: "POST",
          body: {},
        });

        if (result.error) {
          return { error: result.error };
        }

        return { data: result.data as unknown };
      },
    }),
    startReplay: builder.mutation<unknown, void>({
      async queryFn(_arg, _api, _extraOptions, fetchWithBQ) {
        const result = await fetchWithBQ({
          url: `${replayApiBaseUrl}/api/replay/start`,
          method: "POST",
        });

        if (result.error) {
          return { error: result.error };
        }

        return { data: result.data as unknown };
      },
    }),
  }),
});

export const {
  useGetDashboardQuery,
  useGetLeaderboardQuery,
  usePredictNextLapMutation,
  useGetTyreStintStrategyQuery,
  usePredictNextLapBatchMutation,
  useBootstrapReplayMutation,
  useStartReplayMutation,
  useGetDriverPredictionFeaturesMutation,
} = raceStateApi;
