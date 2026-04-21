"use client";

import { useGetTelemetryMetadataQuery } from "@/features/store/race-state/raceStateApi";
import {
  setRaceStateTelemetryError,
  setRaceStateTelemetryLoading,
  setRaceStateTelemetryMetadata,
} from "@/features/store/race-state/raceStateTelemetrySlice";
import { useEffect } from "react";
import { useDispatch } from "react-redux";

export function RaceStateBootstrap() {
  const dispatch = useDispatch();
  const { data, error, isError, isLoading, isSuccess } =
    useGetTelemetryMetadataQuery();

  useEffect(() => {
    if (isLoading) {
      dispatch(setRaceStateTelemetryLoading());
      return;
    }

    if (isSuccess && data) {
      dispatch(setRaceStateTelemetryMetadata(data));
      return;
    }

    if (isError) {
      dispatch(setRaceStateTelemetryError(resolveTelemetryMetadataError(error)));
    }
  }, [data, dispatch, error, isError, isLoading, isSuccess]);

  return null;
}

const resolveTelemetryMetadataError = (error: unknown) => {
  if (typeof error === "object" && error !== null && "status" in error) {
    return `Telemetry metadata API returned ${String(error.status)}.`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Race-state bootstrap request failed.";
};
