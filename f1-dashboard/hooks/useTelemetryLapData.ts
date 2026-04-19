"use client";

import type { RaceStateTelemetrySample } from "@/features/store/race-state/raceStateTypes";
import {
  buildMockTelemetrySample,
  type TelemetryMetricKey,
} from "@/helpers/telemetryStream";
import { startTransition, useEffect, useState } from "react";

type UseTelemetryLapDataOptions = {
  sessionId: string | null | undefined;
  driverNumber: number | null;
  lapNumber: number | null;
  metric: TelemetryMetricKey;
};

type TelemetryLapResponse = {
  sessionId: string;
  driverNumber: number;
  lapNumber: number;
  metrics: TelemetryMetricKey[];
  samples: RaceStateTelemetrySample[];
};

const raceStateApiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8082";
const useDashboardMocks = process.env.NEXT_PUBLIC_USE_DASHBOARD_MOCKS === "true";

const buildMockLapSamples = (
  sessionId: string,
  driverNumber: number,
  lapNumber: number
): RaceStateTelemetrySample[] =>
  Array.from({ length: 120 }, (_, index) =>
    buildMockTelemetrySample(sessionId, driverNumber, index + 1, lapNumber)
  );

export const useTelemetryLapData = ({
  sessionId,
  driverNumber,
  lapNumber,
  metric,
}: UseTelemetryLapDataOptions) => {
  const [samples, setSamples] = useState<RaceStateTelemetrySample[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!sessionId || !driverNumber || !lapNumber) {
      setSamples([]);
      setError(null);
      return;
    }

    if (useDashboardMocks) {
      startTransition(() => {
        setSamples(buildMockLapSamples(sessionId, driverNumber, lapNumber));
        setError(null);
      });
      return;
    }

    let disposed = false;
    setError(null);

    const fetchLapTelemetry = async () => {
      try {
        const searchParams = new URLSearchParams();
        searchParams.set("sessionId", sessionId);
        searchParams.set("metrics", metric);

        const response = await fetch(
          `${raceStateApiBaseUrl}/api/race-state/analytics/drivers/${driverNumber}/laps/${lapNumber}/telemetry?${searchParams.toString()}`,
          {
            method: "GET",
            cache: "no-store",
          }
        );

        if (!response.ok) {
          throw new Error(`Telemetry API returned ${response.status}.`);
        }

        const payload = (await response.json()) as TelemetryLapResponse;
        if (disposed) {
          return;
        }

        startTransition(() => {
          setSamples(payload.samples);
          setError(null);
        });
      } catch (requestError) {
        if (disposed) {
          return;
        }

        setSamples([]);
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Telemetry lap request failed."
        );
      }
    };

    void fetchLapTelemetry();

    return () => {
      disposed = true;
    };
  }, [driverNumber, lapNumber, metric, sessionId]);

  return {
    samples,
    error,
  };
};
