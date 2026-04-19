"use client";

import {
  DASHBOARD_MOCK_REFRESH_INTERVAL_MS,
  DEFAULT_SIGNALR_URL,
  RACE_STATE_UPDATED_EVENT,
  WS_PAYLOAD_PREVIEW_MAX_LENGTH,
} from "@/constants/race-state";
import type {
  RaceCurrentState,
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/store/race-state/raceStateTypes";
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
  const [liveDashboard, setLiveDashboard] = useState<RaceDashboard | null>(null);
  const [liveCurrentState, setLiveCurrentState] = useState<RaceCurrentState | null>(null);
  const [mockTick, setMockTick] = useState(0);
  const mockSnapshot = useMemo(
    () => buildMockRaceDashboardSnapshot(mockTick),
    [mockTick]
  );

  useEffect(() => {
    if (!useDashboardMocks) {
      return;
    }

    setLiveDashboard(mockSnapshot.dashboard);
    setLiveCurrentState(mockSnapshot.currentState);
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
              setLiveDashboard(message.dashboard);
              setLiveCurrentState(message.currentState);
            });
            dispatch(
              setRaceStateTelemetryMetadata({
                session: message.dashboard.session,
                drivers: message.dashboard.leaderboard.map((driver) => ({
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
          }
        } catch {
          dispatch(setWsStatus("error"));
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

  const isLoading =
    wsUi.wsStatus === "idle" ||
    wsUi.wsStatus === "connecting" ||
    (!liveDashboard && !liveCurrentState && wsUi.wsStatus !== "error");
  const isError = wsUi.wsStatus === "error" || wsUi.wsStatus === "closed";

  return {
    data: liveDashboard,
    currentState: liveCurrentState,
    wsUi,
    sessionCards,
    leaderboardRows,
    isLoading,
    isFetching: wsUi.wsStatus === "connecting",
    isError,
    error: isError ? new Error("Race-state SignalR connection is unavailable.") : undefined,
    refetch: async () => ({ data: liveDashboard }),
  };
};
