"use client";

import {
  DASHBOARD_MOCK_REFRESH_INTERVAL_MS,
  WS_PAYLOAD_PREVIEW_MAX_LENGTH,
} from "@/constants/race-state";
import type {
  RaceCurrentState,
  RaceDashboard,
  RaceStateSessionView,
} from "@/features/store/race-state/raceStateTypes";
import {
  selectRaceStateLiveCurrentState,
  selectRaceStateLiveDashboard,
} from "@/features/store/race-state/raceStateLiveSlice";
import { setRaceStateTelemetryMetadata } from "@/features/store/race-state/raceStateTelemetrySlice";
import {
  selectRaceStateUi,
  setLastWsPayloadPreview,
  setWsStatus,
} from "@/features/store/race-state/raceStateUiSlice";
import {
  buildRaceDashboardRows,
  buildSessionCards,
} from "@/helpers/raceStateDashboard";
import { buildMockRaceDashboardSnapshot } from "@/mocks/dashboard/race-state-dashboard.mock";
import { startTransition, useEffect, useMemo, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
const useDashboardMocks = process.env.NEXT_PUBLIC_USE_DASHBOARD_MOCKS === "true";

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

const parseNullableNumber = (value: unknown) => {
  if (typeof value === "number") {
    return Number.isFinite(value) ? value : null;
  }

  if (typeof value === "string" && value.trim().length > 0) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  return null;
};

const resolveLapCountFromCurrentState = (
  currentState: RaceCurrentState | null,
) => {
  if (!currentState || !isRecord(currentState.session)) {
    return null;
  }

  const lapCount =
    currentState.session["lap.count.updated"] ?? currentState.session.lapCount;
  if (!isRecord(lapCount)) {
    return null;
  }

  return {
    currentLap: parseNullableNumber(lapCount.currentLap ?? lapCount.CurrentLap),
    totalLaps: parseNullableNumber(lapCount.totalLaps ?? lapCount.TotalLaps),
  };
};

const buildFallbackSession = (
  currentState: RaceCurrentState,
): RaceStateSessionView => ({
  sessionId: currentState.sessionId,
  currentLap: null,
  totalLaps: null,
  trackStatusCode: null,
  trackStatusMessage: null,
  lastProcessedEventTime: currentState.lastProcessedEventTime,
  lastProcessedSequence: currentState.lastProcessedSequence,
  updatedAt: currentState.updatedAt,
});

const buildEffectiveDashboard = (
  dashboard: RaceDashboard | null,
  currentState: RaceCurrentState | null,
): RaceDashboard | null => {
  if (!dashboard && !currentState) {
    return null;
  }

  const session = dashboard?.session ?? (
    currentState ? buildFallbackSession(currentState) : null
  );
  if (!session) {
    return null;
  }

  const lapCount = resolveLapCountFromCurrentState(currentState);
  const currentLap = lapCount?.currentLap ?? session.currentLap;
  const totalLaps = lapCount?.totalLaps ?? session.totalLaps;

  return {
    session: {
      ...session,
      currentLap,
      totalLaps,
      lastProcessedEventTime:
        currentState?.lastProcessedEventTime ?? session.lastProcessedEventTime,
      lastProcessedSequence:
        currentState?.lastProcessedSequence ?? session.lastProcessedSequence,
      updatedAt: currentState?.updatedAt ?? session.updatedAt,
    },
    leaderboard: currentState?.leaderboard ?? dashboard?.leaderboard ?? [],
  };
};

export const useRaceDashboardLive = () => {
  const dispatch = useDispatch();
  const wsUi = useSelector(selectRaceStateUi);
  const storeDashboard = useSelector(selectRaceStateLiveDashboard);
  const storeCurrentState = useSelector(selectRaceStateLiveCurrentState);
  const [mockDashboard, setMockDashboard] = useState<RaceDashboard | null>(null);
  const [mockCurrentState, setMockCurrentState] = useState<RaceCurrentState | null>(null);
  const [mockTick, setMockTick] = useState(0);
  const mockSnapshot = useMemo(
    () => buildMockRaceDashboardSnapshot(mockTick),
    [mockTick]
  );

  useEffect(() => {
    if (!useDashboardMocks) {
      return;
    }

    setMockDashboard(mockSnapshot.dashboard);
    setMockCurrentState(mockSnapshot.currentState);
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
    dispatch(setWsStatus("open"));
    dispatch(
      setLastWsPayloadPreview(
        JSON.stringify(mockSnapshot.message).slice(0, WS_PAYLOAD_PREVIEW_MAX_LENGTH)
      )
    );
  }, [dispatch, mockSnapshot]);

  useEffect(() => {
    if (!useDashboardMocks) {
      return;
    }

    const intervalId = setInterval(() => {
      startTransition(() => {
        setMockTick((currentTick) => currentTick + 1);
      });
    }, DASHBOARD_MOCK_REFRESH_INTERVAL_MS);

    return () => {
      clearInterval(intervalId);
    };
  }, []);

  const liveDashboard = useDashboardMocks ? mockDashboard : storeDashboard;
  const liveCurrentState = useDashboardMocks ? mockCurrentState : storeCurrentState;

  const effectiveDashboard = useMemo(
    () => buildEffectiveDashboard(liveDashboard, liveCurrentState),
    [liveCurrentState, liveDashboard]
  );

  const sessionCards = useMemo(
    () => buildSessionCards(effectiveDashboard?.session),
    [effectiveDashboard?.session]
  );

  const leaderboardRows = useMemo(
    () =>
      effectiveDashboard
        ? buildRaceDashboardRows(effectiveDashboard)
        : [],
    [effectiveDashboard]
  );

  if (useDashboardMocks) {
    return {
      data: effectiveDashboard,
      currentState: liveCurrentState,
      wsUi,
      sessionCards,
      leaderboardRows,
      isLoading: false,
      isError: false,
      isFetching: false,
      error: undefined,
      refetch: async () => ({ data: effectiveDashboard }),
    };
  }

  const isLoading =
    wsUi.wsStatus === "idle" ||
    wsUi.wsStatus === "connecting" ||
    (!effectiveDashboard && wsUi.wsStatus !== "error");
  const isError = wsUi.wsStatus === "error" || wsUi.wsStatus === "closed";

  return {
    data: effectiveDashboard,
    currentState: liveCurrentState,
    wsUi,
    sessionCards,
    leaderboardRows,
    isLoading,
    isFetching: wsUi.wsStatus === "connecting",
    isError,
    error: isError ? new Error("Race-state SignalR connection is unavailable.") : undefined,
    refetch: async () => ({ data: effectiveDashboard }),
  };
};
