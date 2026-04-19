"use client";

import type { RaceStateTelemetrySample } from "@/features/store/race-state/raceStateTypes";

export type TelemetryMetricKey =
  | "speed"
  | "throttlePct"
  | "brakePct"
  | "rpm"
  | "gear"
  | "drsEnabled";

export type TelemetryChartPoint = RaceStateTelemetrySample & {
  drsEnabledNumeric: number;
  timestampMs: number;
};

export const telemetryMetricOptions: Array<{
  key: TelemetryMetricKey;
  label: string;
  unit: string;
  color: string;
  dataKey: keyof TelemetryChartPoint;
}> = [
  { key: "speed", label: "SPEED", unit: "km/h", color: "#DC0000", dataKey: "speed" },
  { key: "throttlePct", label: "THROTTLE", unit: "%", color: "#007A36", dataKey: "throttlePct" },
  { key: "brakePct", label: "BRAKE", unit: "%", color: "#8F6B00", dataKey: "brakePct" },
  { key: "rpm", label: "RPM", unit: "rpm", color: "#213357", dataKey: "rpm" },
  { key: "gear", label: "GEAR", unit: "", color: "#4A5F91", dataKey: "gear" },
  { key: "drsEnabled", label: "DRS", unit: "", color: "#005CC8", dataKey: "drsEnabledNumeric" },
];

export const buildTelemetryChartPoints = (
  samples: RaceStateTelemetrySample[]
): TelemetryChartPoint[] => {
  return samples.map((sample) => ({
    ...sample,
    drsEnabledNumeric: sample.drsEnabled ? 1 : 0,
    timestampMs: new Date(sample.timestamp).getTime(),
  }));
};

export const formatTelemetryMetricValue = (
  metric: TelemetryMetricKey,
  value: number | boolean | null | undefined
) => {
  if (value === null || value === undefined) {
    return "—";
  }

  if (metric === "drsEnabled") {
    return value ? "ON" : "OFF";
  }

  if (metric === "rpm") {
    return `${Math.round(Number(value)).toLocaleString()}`;
  }

  if (metric === "speed") {
    return `${Math.round(Number(value))}`;
  }

  if (metric === "gear") {
    return `${Math.round(Number(value))}`;
  }

  return `${Math.round(Number(value))}`;
};

export const formatTelemetryTimestamp = (timestamp: string) => {
  return new Date(timestamp).toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  });
};

export const buildMockTelemetrySample = (
  sessionId: string,
  driverNumber: number,
  sampleIndex: number,
  lapNumber = 53
): RaceStateTelemetrySample => {
  const phase = sampleIndex / 8 + driverNumber;
  const secondaryPhase = sampleIndex / 14 + driverNumber / 2;
  const speed = 245 + Math.round(Math.sin(phase) * 38 + Math.cos(secondaryPhase) * 16);
  const throttlePct = Math.max(14, Math.min(100, Math.round(78 + Math.sin(phase) * 22)));
  const brakePct = Math.max(0, Math.min(100, Math.round(18 + Math.cos(phase * 1.4) * 28)));
  const rpm = 9600 + Math.round(Math.sin(phase * 1.2) * 950 + throttlePct * 38);
  const gear = Math.max(1, Math.min(8, Math.round(5 + Math.sin(secondaryPhase) * 2)));

  return {
    sessionId,
    driverNumber,
    lapNumber,
    stintNumber: 2,
    sampleIndex,
    timestamp: new Date(Date.now() + sampleIndex * 850).toISOString(),
    speed,
    rpm,
    throttlePct,
    rawThrottle: throttlePct,
    brakePct,
    rawBrake: Math.round(brakePct),
    gear,
    drsEnabled: speed > 285 || throttlePct > 92,
  };
};
