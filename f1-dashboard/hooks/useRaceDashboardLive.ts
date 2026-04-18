"use client";

import {
  DASHBOARD_MOCK_REFRESH_INTERVAL_MS,
  DEFAULT_SIGNALR_URL,
  RACE_STATE_UPDATED_EVENT,
  WS_PAYLOAD_PREVIEW_MAX_LENGTH,
} from "@/constants/race-state";
import {
  useGetCurrentQuery,
  useGetDashboardQuery,
} from "@/features/store/race-state/raceStateApi";
import type {
  RaceCurrentState,
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/store/race-state/raceStateTypes";
import {
  selectRaceStateUi,
  setLastWsPayloadPreview,
  setWsStatus,
} from "@/features/store/race-state/raceStateUiSlice";
import { applyRaceStateMessage } from "@/helpers/applyRaceStateMessage";
import {
  buildRaceDashboardRows,
  buildSessionCards,
} from "@/helpers/raceStateDashboard";
import { useSignalR } from "@/hooks/useSignalR";
import { buildMockRaceDashboardSnapshot } from "@/mocks/dashboard/race-state-dashboard.mock";
import { startTransition, useEffect, useMemo, useState } from "react";
import { useDispatch, useSelector } from "react-redux";

const resolveLegacySignalRUrl = (url: string | undefined) => {
  if (!url) {
    return null;
  }

  try {
    const parsed = new URL(url);
    parsed.protocol = parsed.protocol === "wss:" ? "https:" : "http:";
    parsed.pathname = "/hubs/race-state";
    parsed.search = "";
    parsed.hash = "";
    return parsed.toString();
  } catch {
    return null;
  }
};

const defaultSignalRUrl =
  process.env.NEXT_PUBLIC_SIGNALR_URL ??
  resolveLegacySignalRUrl(process.env.NEXT_PUBLIC_WS_URL) ??
  DEFAULT_SIGNALR_URL;
const useDashboardMocks = process.env.NEXT_PUBLIC_USE_DASHBOARD_MOCKS === "true";

export const useRaceDashboardLive = () => {
  const dispatch = useDispatch();
  const wsUi = useSelector(selectRaceStateUi);
  const dashboardQuery = useGetDashboardQuery(undefined, {
    skip: useDashboardMocks,
  });
  const currentStateQuery = useGetCurrentQuery(undefined, {
    skip: useDashboardMocks,
  });
  const [liveDashboard, setLiveDashboard] = useState<RaceDashboard | null>(null);
  const [liveCurrentState, setLiveCurrentState] = useState<RaceCurrentState | null>(null);
  const [hasLiveWsState, setHasLiveWsState] = useState(false);
  const [mockTick, setMockTick] = useState(0);
  const mockSnapshot = useMemo(
    () => buildMockRaceDashboardSnapshot(mockTick),
    [mockTick]
  );

  useEffect(() => {
    if (!useDashboardMocks) {
      return;
    }

    setHasLiveWsState(true);
    setLiveDashboard(mockSnapshot.dashboard);
    setLiveCurrentState(mockSnapshot.currentState);
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

  useEffect(() => {
    if (useDashboardMocks) {
      return;
    }

    const nextDashboard = dashboardQuery.data;
    if (nextDashboard && !hasLiveWsState) {
      setLiveDashboard(nextDashboard);
    }
  }, [dashboardQuery.data, hasLiveWsState]);

  useEffect(() => {
    if (useDashboardMocks) {
      return;
    }

    const nextCurrentState = currentStateQuery.data;
    if (nextCurrentState && !hasLiveWsState) {
      setLiveCurrentState(nextCurrentState);
    }
  }, [currentStateQuery.data, hasLiveWsState]);

  useSignalR(defaultSignalRUrl, {
    enabled: !useDashboardMocks,
    onStatusChange: (status) => {
      dispatch(setWsStatus(status));
    },
    handlers: {
      [RACE_STATE_UPDATED_EVENT]: (payload) => {
        try {
          const message = payload as RaceStateWsMessage;
          dispatch(
            setLastWsPayloadPreview(
              JSON.stringify(message).slice(0, WS_PAYLOAD_PREVIEW_MAX_LENGTH)
            )
          );

          if (message.type === RACE_STATE_UPDATED_EVENT) {
            startTransition(() => {
              setHasLiveWsState(true);
              setLiveDashboard((currentDashboard) =>
                applyRaceStateMessage(currentDashboard, message)
              );
              setLiveCurrentState(message.currentState);
            });
          }
        } catch {
          void dashboardQuery.refetch();
          void currentStateQuery.refetch();
        }
      }
    },
  });

  const sessionCards = useMemo(
    () => buildSessionCards(liveDashboard?.session),
    [liveDashboard?.session]
  );

  const leaderboardRows = useMemo(
    () =>
      liveCurrentState
        ? buildRaceDashboardRows({
          session: liveDashboard?.session ?? {
            sessionId: liveCurrentState.sessionId,
            currentLap: null,
            totalLaps: null,
            trackStatusCode: null,
            trackStatusMessage: null,
            lastProcessedEventTime: liveCurrentState.lastProcessedEventTime,
            lastProcessedSequence: liveCurrentState.lastProcessedSequence,
            updatedAt: liveCurrentState.updatedAt,
          },
          leaderboard: liveCurrentState.leaderboard,
        })
        : liveDashboard
          ? buildRaceDashboardRows(liveDashboard)
          : [],
    [liveCurrentState, liveDashboard]
  );

  if (useDashboardMocks) {
    return {
      data: liveDashboard,
      currentState: liveCurrentState,
      wsUi,
      sessionCards,
      leaderboardRows,
      isLoading: false,
      isError: false,
      isFetching: false,
      error: undefined,
      refetch: async () => ({ data: liveDashboard }),
    };
  }

  return {
    ...dashboardQuery,
    data: liveDashboard,
    currentState: liveCurrentState,
    wsUi,
    sessionCards,
    leaderboardRows,
  };
};
