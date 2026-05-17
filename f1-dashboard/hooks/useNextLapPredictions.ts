"use client";

import {
  useGetDriverPredictionFeaturesMutation,
  usePredictNextLapMutation,
} from "@/features/store/race-state/raceStateApi";
import { selectRaceStateLiveCurrentState } from "@/features/store/race-state/raceStateLiveSlice";
import type {
  DriverPredictionFeaturesResponse,
  NextLapPredictionCycle,
  PredictionComparisonEntry,
  PredictionWorkflowStatus,
} from "@/features/store/race-state/raceStateTypes";
import {
  buildPredictionCycle,
  calculatePredictionAccuracyPercentage,
} from "@/helpers/nextLapPrediction";
import { useEffect, useMemo, useRef, useState } from "react";
import { useSelector } from "react-redux";

type UseNextLapPredictionsOptions = {
  selectedDriverNumber: number | null;
  enabled: boolean;
};

type UseNextLapPredictionsResult = {
  basisSnapshot: DriverPredictionFeaturesResponse | null;
  pendingPrediction: NextLapPredictionCycle | null;
  comparisonHistory: PredictionComparisonEntry[];
  predictionError: string | null;
  status: PredictionWorkflowStatus;
  statusMessage: string;
  isRefreshingBasis: boolean;
  isPredicting: boolean;
  loadBasisSnapshot: () => Promise<void>;
  runPrediction: () => Promise<void>;
};

export const useNextLapPredictions = ({
  selectedDriverNumber,
  enabled,
}: UseNextLapPredictionsOptions): UseNextLapPredictionsResult => {
  const currentState = useSelector(selectRaceStateLiveCurrentState);
  const sessionId = currentState?.sessionId ?? null;
  const selectedDriver = useMemo(() => {
    if (!currentState || !selectedDriverNumber) {
      return null;
    }

    return currentState.drivers[String(selectedDriverNumber)] ?? null;
  }, [currentState, selectedDriverNumber]);
  const completedLapNumber = selectedDriver?.prediction.lastCompletedLapNumber ?? null;
  const completedLapTime = selectedDriver?.prediction.lastCompletedLapTimeSeconds ?? null;

  const [fetchDriverPredictionFeatures, basisRequest] =
    useGetDriverPredictionFeaturesMutation();
  const [predictNextLap, predictionRequest] = usePredictNextLapMutation();

  const [basisSnapshot, setBasisSnapshot] =
    useState<DriverPredictionFeaturesResponse | null>(null);
  const [pendingPrediction, setPendingPrediction] =
    useState<NextLapPredictionCycle | null>(null);
  const [comparisonHistory, setComparisonHistory] = useState<
    PredictionComparisonEntry[]
  >([]);
  const [predictionError, setPredictionError] = useState<string | null>(null);
  const [status, setStatus] = useState<PredictionWorkflowStatus>("no_data");
  const [statusMessage, setStatusMessage] = useState(
    "Choose a driver, then load the latest lap snapshot.",
  );
  const lastResolvedPredictionLapRef = useRef<number | null>(null);

  useEffect(() => {
    lastResolvedPredictionLapRef.current = null;
    setBasisSnapshot(null);
    setPendingPrediction(null);
    setComparisonHistory([]);
    setPredictionError(null);
    setStatus("no_data");
    setStatusMessage("Choose a driver, then load the latest lap snapshot.");
  }, [sessionId]);

  useEffect(() => {
    lastResolvedPredictionLapRef.current = null;
    setBasisSnapshot(null);
    setPendingPrediction(null);
    setComparisonHistory([]);
    setPredictionError(null);
    setStatus("no_data");
    setStatusMessage("Driver changed. Load the latest lap snapshot for this driver.");
  }, [selectedDriverNumber]);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    if (!selectedDriverNumber || !selectedDriver) {
      setBasisSnapshot(null);
      setPendingPrediction(null);
      setStatus("no_data");
      setStatusMessage("Select a driver, then load the latest lap snapshot.");
      return;
    }

    if (
      pendingPrediction &&
      completedLapNumber === pendingPrediction.predictedForLap &&
      completedLapTime !== null &&
      lastResolvedPredictionLapRef.current !== completedLapNumber
    ) {
      const deltaToActual = pendingPrediction.predictedNextLapTime - completedLapTime;
      const accuracyPercentage = calculatePredictionAccuracyPercentage(
        pendingPrediction.predictedNextLapTime,
        completedLapTime,
      );

      lastResolvedPredictionLapRef.current = completedLapNumber;
      setComparisonHistory((currentHistory) => [
        {
          driverNumber: pendingPrediction.driverNumber,
          driverName: pendingPrediction.driverName,
          basisLapNumber: pendingPrediction.basisLapNumber,
          basisLapTime: pendingPrediction.basisLapTime,
          lapNumber: completedLapNumber,
          predictedLapTime: pendingPrediction.predictedNextLapTime,
          actualLapTime: completedLapTime,
          deltaToActual,
          accuracyPercentage,
          modelVersion: pendingPrediction.modelVersion,
          resolvedAt: new Date().toISOString(),
        },
        ...currentHistory,
      ].slice(0, 8));
      setPendingPrediction(null);
      setPredictionError(null);
      setStatus("resolved");
      setStatusMessage(
        `Lap ${completedLapNumber} is complete. The result has been added to prediction history, and Lap ${completedLapNumber + 1} can now be prepared.`,
      );
    }
  }, [
    enabled,
    completedLapNumber,
    completedLapTime,
    pendingPrediction,
    selectedDriver,
    selectedDriverNumber,
  ]);

  const loadBasisSnapshot = async () => {
    if (!enabled) {
      setPredictionError("Open the prediction tab first.");
      setStatus("no_data");
      setStatusMessage("Prediction is available only while the prediction tab is active.");
      return;
    }

    if (!selectedDriverNumber || !selectedDriver) {
      setPredictionError("Choose a driver first.");
      setStatus("no_data");
      setStatusMessage("Select a driver, then load the latest lap snapshot.");
      return;
    }

    setPredictionError(null);
    setPendingPrediction(null);
    setComparisonHistory([]);
    setStatus("loading_basis");
    setStatusMessage(`Loading the latest completed lap snapshot for Driver #${selectedDriverNumber}.`);

    try {
      const response = await fetchDriverPredictionFeatures(selectedDriverNumber).unwrap();
      setBasisSnapshot(response);

      if (response.features && response.lastCompletedLapNumber) {
        setStatus("ready");
        setStatusMessage(
          `Lap ${response.lastCompletedLapNumber} is ready. Predict Lap ${response.lastCompletedLapNumber + 1} when you want.`,
        );
        return;
      }

      setStatus("no_data");
      setStatusMessage(
        "The selected driver still does not have a complete feature snapshot for prediction.",
      );
    } catch (error) {
      setPredictionError(resolvePredictionError(error));
      setStatus("error");
      setStatusMessage("The latest lap snapshot could not be loaded from race state.");
    }
  };

  const runPrediction = async () => {
    if (!enabled) {
      setPredictionError("Open the prediction tab first.");
      setStatus("no_data");
      setStatusMessage("Prediction is available only while the prediction tab is active.");
      return;
    }

    if (!currentState || !basisSnapshot?.features || !selectedDriverNumber) {
      setPredictionError(
        "Prediction cannot run yet because the selected driver has no valid completed lap snapshot.",
      );
      setStatus("no_data");
      setStatusMessage(
        "Wait for a valid completed lap, then run the next-lap prediction again.",
      );
      return;
    }

    setPredictionError(null);
    setStatus("predicting");
    setStatusMessage(
      `Sending Lap ${basisSnapshot.lastCompletedLapNumber ?? "?"} to the model and preparing a forecast for the next lap.`,
    );

    try {
      const response = await predictNextLap({
        features: basisSnapshot.features,
      }).unwrap();

      const cycle = buildPredictionCycle(
        currentState,
        basisSnapshot,
        basisSnapshot.features,
        response.predicted_next_lap_time,
        response.model_version,
      );

      if (!cycle) {
        setPredictionError("The prediction payload was created, but the UI could not build a valid prediction cycle.");
        setStatus("error");
        setStatusMessage("Prediction response arrived, but the cycle summary could not be assembled.");
        return;
      }

      setPendingPrediction(cycle);
      setStatus("waiting_for_actual");
      setStatusMessage(
        `Prediction stored for Lap ${cycle.predictedForLap}. Waiting for the driver to complete that lap so the estimate can be compared with the real time.`,
      );
    } catch (error) {
      setPredictionError(resolvePredictionError(error));
      setStatus("error");
      setStatusMessage("The model request failed. Review the API error and retry the prediction.");
    }
  };

  return {
    basisSnapshot,
    pendingPrediction,
    comparisonHistory,
    predictionError,
    status,
    statusMessage,
    isRefreshingBasis: basisRequest.isLoading,
    isPredicting: predictionRequest.isLoading,
    loadBasisSnapshot,
    runPrediction,
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
