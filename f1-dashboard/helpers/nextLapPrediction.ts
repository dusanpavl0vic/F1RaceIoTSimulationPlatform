"use client";

import type {
  NextLapPredictionFeatures,
  NextLapPredictionRequest,
  RaceCurrentDriverState,
  RaceCurrentState,
  RaceNextLapPredictionDriver,
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
    typeof driver.leaderboard.lapsCompleted === "number" &&
    Number.isFinite(driver.leaderboard.lapsCompleted) &&
    driver.leaderboard.lapsCompleted > 0
  ) {
    return driver.leaderboard.lapsCompleted;
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
  const lapTimeLast = parseLapTimeSeconds(driver.leaderboard.lastLapTime);
  if (!lapNumber || !lapTimeLast) {
    return null;
  }

  return {
    driver_number: driver.driverNumber,
    lap_number: lapNumber,
    position: driver.leaderboard.position,
    lap_time_last: lapTimeLast,
    lap_time_best: parseLapTimeSeconds(driver.leaderboard.bestLapTime),
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

const resolveDriverName = (driver: RaceCurrentDriverState) =>
  driver.tla ??
  driver.broadcastName ??
  driver.fullName ??
  `#${driver.driverNumber}`;

export const buildPredictionSnapshotDriver = (
  currentState: RaceCurrentState | null,
  features: NextLapPredictionFeatures,
  predictedLapTime: number | null,
): RaceNextLapPredictionDriver | null => {
  if (!currentState) {
    return null;
  }

  const driver = currentState.drivers[String(features.driver_number)];
  return {
    driverNumber: features.driver_number,
    driverName: driver ? resolveDriverName(driver) : `#${features.driver_number}`,
    teamName: driver?.team.name ?? null,
    teamColor: driver?.team.color ?? null,
    position: features.position,
    completedLaps: features.lap_number,
    predictedForLap: features.lap_number + 1,
    predictedNextLapTime: predictedLapTime,
    lastLapTimeActual: driver
      ? parseLapTimeSeconds(driver.leaderboard.lastLapTime)
      : features.lap_time_last,
    bestLapTimeActual: driver
      ? parseLapTimeSeconds(driver.leaderboard.bestLapTime)
      : features.lap_time_best,
  };
};
