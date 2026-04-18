"use client";

import { startTransition, useEffect, useMemo, useState } from "react";
import {
  useGetCurrentQuery,
  useGetDashboardQuery,
} from "@/features/race-state/api/race-state-api";
import { useSignalR } from "@/features/race-state/hooks/use-signalr";
import {
  buildRaceDashboardRows,
  buildSessionCards,
} from "@/features/race-state/utils/dashboard-normalizers";
import { selectRaceStateUi } from "@/features/race-state/store/selectors";
import {
  setLastWsPayloadPreview,
  setWsStatus,
} from "@/features/race-state/store/race-state-ui-slice";
import type {
  RaceCurrentState,
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/race-state/types/race-state";
import { useAppDispatch, useAppSelector } from "@/store/hooks";

const defaultSignalRUrl =
  process.env.NEXT_PUBLIC_SIGNALR_URL ??
  resolveLegacySignalRUrl(process.env.NEXT_PUBLIC_WS_URL) ??
  "http://localhost:8082/hubs/race-state";

export function useRaceDashboardLive() {
  const dispatch = useAppDispatch();
  const wsUi = useAppSelector(selectRaceStateUi);
  const dashboardQuery = useGetDashboardQuery();
  const currentStateQuery = useGetCurrentQuery();
  const [liveDashboard, setLiveDashboard] = useState<RaceDashboard | null>(null);
  const [liveCurrentState, setLiveCurrentState] = useState<RaceCurrentState | null>(null);
  const [hasLiveWsState, setHasLiveWsState] = useState(false);

  useEffect(() => {
    const nextDashboard = dashboardQuery.data;
    if (nextDashboard && !hasLiveWsState) {
      setLiveDashboard(nextDashboard);
    }
  }, [dashboardQuery.data, hasLiveWsState]);

  useEffect(() => {
    const nextCurrentState = currentStateQuery.data;
    if (nextCurrentState && !hasLiveWsState) {
      setLiveCurrentState(nextCurrentState);
    }
  }, [currentStateQuery.data, hasLiveWsState]);

  useSignalR(defaultSignalRUrl, {
    onStatusChange: (status) => {
      dispatch(setWsStatus(status));
    },
    handlers: {
      "race.state.updated": (payload) => {
        try {
          const message = payload as RaceStateWsMessage;
          dispatch(
            setLastWsPayloadPreview(JSON.stringify(message).slice(0, 280))
          );

          if (message.type === "race.state.updated") {
            startTransition(() => {
              setHasLiveWsState(true);
              setLiveDashboard(message.dashboard);
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
    () => buildSessionCards(liveDashboard?.session, wsUi.wsStatus),
    [liveDashboard?.session, wsUi.wsStatus]
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

  return {
    ...dashboardQuery,
    data: liveDashboard,
    currentState: liveCurrentState,
    wsUi,
    sessionCards,
    leaderboardRows,
  };
}

function resolveLegacySignalRUrl(url: string | undefined) {
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
}
