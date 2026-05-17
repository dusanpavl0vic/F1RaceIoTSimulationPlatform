"use client";

import type {
  DriverPredictionFeaturesResponse,
  NextLapPredictionCycle,
  NextLapPredictionFeatures,
  NextLapPredictionRequest,
  RaceCurrentDriverState,
  RaceCurrentState,
} from "@/features/store/race-state/raceStateTypes";

const normalizeText = (value: string | null | undefined) => {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
};

export const parseLapTimeSeconds = (value: string | null | undefined) => {
  const normalized = normalizeText(value);
  if (!normalized) {
    return null;
  }

  const upper = normalized.toUpperCase();
  if (upper === "-" || upper === "PIT" || upper === "PIT IN" || upper === "PIT OUT" || upper === "STOP") {
    return null;
  }

  if (upper.includes(":")) {
    const [minutesPart, secondsPart] = upper.split(":");
    const minutes = Number(minutesPart);
    const seconds = Number(secondsPart);
    if (Number.isFinite(minutes) && Number.isFinite(seconds)) {
      return minutes * 60 + seconds;
    }
  }

  const parsed = Number(upper);
  return Number.isFinite(parsed) ? parsed : null;
};

export const parseGapSeconds = (value: string | null | undefined) => {
  const normalized = normalizeText(value);
  if (!normalized) {
    return null;
  }

  const upper = normalized.toUpperCase();
  if (upper === "-" || upper === "LEADER") {
    return null;
  }

  const parsed = Number(upper.replace(/^\+/, ""));
  return Number.isFinite(parsed) ? parsed : null;
};

const resolveCompletedLaps = (
  driver: RaceCurrentDriverState,
  triggerLap: number | null,
) => {
  if (
    typeof driver.prediction.lastCompletedLapNumber === "number" &&
    Number.isFinite(driver.prediction.lastCompletedLapNumber) &&
    driver.prediction.lastCompletedLapNumber > 0
  ) {
    return driver.prediction.lastCompletedLapNumber;
  }

  if (triggerLap && triggerLap > 1) {
    return triggerLap - 1;
  }

  return null;
};

export const calculatePredictionAccuracyPercentage = (
  predictedLapTime: number,
  actualLapTime: number,
) => {
  if (actualLapTime <= 0) {
    return 0;
  }

  const accuracy = 100 * (1 - Math.abs(predictedLapTime - actualLapTime) / actualLapTime);
  return Math.max(0, Math.min(100, accuracy));
};

export const buildNextLapPredictionFeature = (
  driver: RaceCurrentDriverState,
  triggerLap: number | null,
): NextLapPredictionFeatures | null => {
  if (driver.race.retired || driver.race.stopped || driver.race.didNotStart) {
    return null;
  }

  const lapNumber = resolveCompletedLaps(driver, triggerLap);
  const lapTimeLast =
    driver.prediction.lastCompletedLapTimeSeconds ??
    parseLapTimeSeconds(driver.leaderboard.lastLapTime);
  if (!lapNumber || !lapTimeLast) {
    return null;
  }

  return {
    driver_number: driver.driverNumber,
    lap_number: lapNumber,
    position: driver.leaderboard.position,
    lap_time_last: lapTimeLast,
    lap_time_best: parseLapTimeSeconds(driver.leaderboard.bestLapTime),
    lap_time_avg_last_3: driver.prediction.lapTimeAvgLast3 ?? lapTimeLast,
    lap_time_avg_last_5: driver.prediction.lapTimeAvgLast5 ?? lapTimeLast,
    gap_to_leader: parseGapSeconds(driver.leaderboard.gapToLeader),
    gap_to_ahead: parseGapSeconds(driver.leaderboard.intervalToPositionAhead),
    tyre_compound: driver.tyres.compound,
    tyre_is_new: driver.tyres.isNew,
    tyre_laps_on_set: driver.tyres.currentStintLapCount,
    in_pit: driver.race.inPit,
  };
};

export const buildNextLapPredictionRequest = (
  currentState: RaceCurrentState | null,
  driverNumber: number | null,
  triggerLap: number | null,
) : NextLapPredictionRequest | null => {
  if (!currentState || !driverNumber) {
    return null;
  }

  const driver = currentState.drivers[String(driverNumber)];
  if (!driver) {
    return null;
  }

  const features = buildNextLapPredictionFeature(driver, triggerLap);
  return features ? { features } : null;
};

export const resolvePredictionDriverName = (driver: RaceCurrentDriverState) =>
  driver.tla ??
  driver.broadcastName ??
  driver.fullName ??
  `#${driver.driverNumber}`;

export const buildPredictionCycle = (
  currentState: RaceCurrentState | null,
  basisSnapshot: DriverPredictionFeaturesResponse,
  features: NextLapPredictionFeatures,
  predictedLapTime: number,
  modelVersion: string | null,
): NextLapPredictionCycle | null => {
  if (!currentState || basisSnapshot.lastCompletedLapNumber === null) {
    return null;
  }

  const driver = currentState.drivers[String(features.driver_number)];

  const basisLapNumber = basisSnapshot.lastCompletedLapNumber;
  const generatedAt = new Date().toISOString();

  return {
    driverNumber: features.driver_number,
    driverName: driver ? resolvePredictionDriverName(driver) : basisSnapshot.driverName,
    teamName: driver?.team.name ?? null,
    teamColor: driver?.team.color ?? null,
    position: features.position,
    basisLapNumber,
    predictedForLap: basisLapNumber + 1,
    basisLapTime: basisSnapshot.lastCompletedLapTimeSeconds ?? features.lap_time_last,
    bestLapTime: features.lap_time_best,
    averageLast3: basisSnapshot.lapTimeAvgLast3 ?? features.lap_time_avg_last_3,
    averageLast5: basisSnapshot.lapTimeAvgLast5 ?? features.lap_time_avg_last_5,
    predictedNextLapTime: predictedLapTime,
    modelVersion,
    generatedAt,
    actualNextLapTime: null,
    deltaToActual: null,
    accuracyPercentage: null,
  };
};
