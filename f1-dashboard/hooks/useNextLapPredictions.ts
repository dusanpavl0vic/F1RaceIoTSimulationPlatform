"use client";

import { usePredictNextLapMutation } from "@/features/store/race-state/raceStateApi";
import { selectRaceStateLiveCurrentState } from "@/features/store/race-state/raceStateLiveSlice";
import { selectRaceStateTelemetry } from "@/features/store/race-state/raceStateTelemetrySlice";
import type {
  PredictionComparisonEntry,
  RaceNextLapPredictions,
} from "@/features/store/race-state/raceStateTypes";
import {
  buildNextLapPredictionRequest,
  buildPredictionSnapshotDriver,
  calculatePredictionAccuracyPercentage,
} from "@/helpers/nextLapPrediction";
import { useEffect, useMemo, useRef, useState } from "react";
import { useSelector } from "react-redux";

type UseNextLapPredictionsOptions = {
  selectedDriverNumber: number | null;
};

export const useNextLapPredictions = ({
  selectedDriverNumber,
}: UseNextLapPredictionsOptions) => {
  const currentState = useSelector(selectRaceStateLiveCurrentState);
  const { session } = useSelector(selectRaceStateTelemetry);
  const currentLap = session?.currentLap ?? null;
  const sessionId = session?.sessionId ?? currentState?.sessionId ?? null;
  const requestPayload = useMemo(
    () =>
      buildNextLapPredictionRequest(
        currentState,
        selectedDriverNumber,
        currentLap,
      ),
    [currentLap, currentState, selectedDriverNumber],
  );

  const [predictNextLap, predictionRequest] = usePredictNextLapMutation();
  const [predictionSnapshot, setPredictionSnapshot] =
    useState<RaceNextLapPredictions>();
  const [predictionError, setPredictionError] = useState<string | null>(null);
  const [comparisonHistory, setComparisonHistory] = useState<
    PredictionComparisonEntry[]
  >([]);
  const lastRequestedLapByDriverRef = useRef<Record<number, number>>({});
  const previousPredictionByDriverRef = useRef<
    Record<number, { predictedForLap: number; predictedNextLapTime: number }>
  >({});

  useEffect(() => {
    lastRequestedLapByDriverRef.current = {};
    previousPredictionByDriverRef.current = {};
    setPredictionSnapshot(undefined);
    setPredictionError(null);
    setComparisonHistory([]);
  }, [sessionId]);

  useEffect(() => {
    if (selectedDriverNumber) {
      delete lastRequestedLapByDriverRef.current[selectedDriverNumber];
    }
    setPredictionSnapshot(undefined);
    setPredictionError(null);
    setComparisonHistory([]);
  }, [selectedDriverNumber]);

  useEffect(() => {
    if (!sessionId || !selectedDriverNumber || !requestPayload) {
      return;
    }

    const completedLaps = requestPayload.features.lap_number;
    if (completedLaps <= 0) {
      return;
    }

    if (lastRequestedLapByDriverRef.current[selectedDriverNumber] === completedLaps) {
      return;
    }

    let cancelled = false;

    const runPrediction = async () => {
      lastRequestedLapByDriverRef.current[selectedDriverNumber] = completedLaps;

      try {
        const response = await predictNextLap(requestPayload).unwrap();
        if (cancelled) {
          return;
        }

        const nextDriver = buildPredictionSnapshotDriver(
          currentState,
          requestPayload.features,
          response.predicted_next_lap_time,
        );
        if (!nextDriver) {
          return;
        }

        const previousPrediction =
          previousPredictionByDriverRef.current[selectedDriverNumber];
        const actualLapTime = nextDriver.lastLapTimeActual;

        if (
          previousPrediction &&
          nextDriver.completedLaps === previousPrediction.predictedForLap &&
          actualLapTime !== null
        ) {
          setComparisonHistory((currentHistory) => {
            if (
              currentHistory.some(
                (entry) => entry.lapNumber === previousPrediction.predictedForLap,
              )
            ) {
              return currentHistory;
            }

            return [
              {
                lapNumber: previousPrediction.predictedForLap,
                predictedLapTime: previousPrediction.predictedNextLapTime,
                actualLapTime,
                deltaToActual:
                  previousPrediction.predictedNextLapTime -
                  actualLapTime,
                accuracyPercentage: calculatePredictionAccuracyPercentage(
                  previousPrediction.predictedNextLapTime,
                  actualLapTime,
                ),
              },
              ...currentHistory,
            ].slice(0, 8);
          });
        }

        previousPredictionByDriverRef.current[selectedDriverNumber] = {
          predictedForLap: nextDriver.predictedForLap ?? completedLaps + 1,
          predictedNextLapTime:
            nextDriver.predictedNextLapTime ?? response.predicted_next_lap_time,
        };

        setPredictionSnapshot({
          sessionId,
          triggerLap: currentLap,
          modelVersion: response.model_version,
          generatedAt: new Date().toISOString(),
          drivers: [nextDriver],
        });
        setPredictionError(null);
      } catch (error) {
        if (cancelled) {
          return;
        }

        delete lastRequestedLapByDriverRef.current[selectedDriverNumber];
        setPredictionError(resolvePredictionError(error));
      }
    };

    void runPrediction();

    return () => {
      cancelled = true;
    };
  }, [
    currentLap,
    currentState,
    predictNextLap,
    requestPayload,
    selectedDriverNumber,
    sessionId,
  ]);

  return {
    predictionSnapshot,
    comparisonHistory,
    predictionError,
    isLoading: predictionRequest.isLoading && !predictionSnapshot,
    isFetching: predictionRequest.isLoading,
  };
};

const resolvePredictionError = (error: unknown) => {
  if (!error) {
    return "Prediction request failed.";
  }

  if (typeof error === "object" && error !== null && "status" in error) {
    return `Prediction API returned ${String(error.status)}.`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Prediction request failed.";
};
