"use client";

import {
  setRaceStateTelemetryError,
  setRaceStateTelemetryLoading,
  setRaceStateTelemetryMetadata,
} from "@/features/store/race-state/raceStateTelemetrySlice";
import type { RaceTelemetryMetadata } from "@/features/store/race-state/raceStateTypes";
import { buildMockRaceDashboardSnapshot } from "@/mocks/dashboard/race-state-dashboard.mock";
import { useEffect } from "react";
import { useDispatch } from "react-redux";

const raceStateApiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8082";
const useDashboardMocks = process.env.NEXT_PUBLIC_USE_DASHBOARD_MOCKS === "true";

export function RaceStateBootstrap() {
  const dispatch = useDispatch();

  useEffect(() => {
    let disposed = false;

    const load = async () => {
      dispatch(setRaceStateTelemetryLoading());

      if (useDashboardMocks) {
        const mockSnapshot = buildMockRaceDashboardSnapshot(0);
        dispatch(
          setRaceStateTelemetryMetadata({
            session: mockSnapshot.dashboard.session,
            drivers: mockSnapshot.dashboard.leaderboard.map((driver) => ({
              driverNumber: driver.driverNumber,
              broadcastName: driver.broadcastName,
              fullName: driver.fullName,
              tla: driver.tla,
              teamName: driver.teamName,
              teamColor: driver.teamColor,
              position: driver.position,
              gridPosition: driver.gridPosition,
            })),
          })
        );
        return;
      }

      try {
        const response = await fetch(`${raceStateApiBaseUrl}/api/race-state/telemetry/metadata`, {
          method: "GET",
          cache: "no-store",
        });

        if (!response.ok) {
          throw new Error(`Telemetry metadata API returned ${response.status}.`);
        }

        const metadata = (await response.json()) as RaceTelemetryMetadata;

        if (disposed) {
          return;
        }

        dispatch(setRaceStateTelemetryMetadata(metadata));
      } catch (error) {
        if (disposed) {
          return;
        }

        dispatch(
          setRaceStateTelemetryError(
            error instanceof Error
              ? error.message
              : "Race-state bootstrap request failed."
          )
        );
      }
    };

    void load();

    return () => {
      disposed = true;
    };
  }, [dispatch]);

  return null;
}
