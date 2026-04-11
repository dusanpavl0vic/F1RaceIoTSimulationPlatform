"use client";

import { useEffect, useMemo, useState } from "react";
import { useGetDashboardQuery } from "@/features/race-state/api/race-state-api";
import { useWebSocket } from "@/features/race-state/hooks/use-websocket";
import {
  buildRaceDashboardRows,
  buildTrackMapPositions,
  buildSessionCards,
} from "@/features/race-state/utils/dashboard-normalizers";
import { selectRaceStateUi } from "@/features/race-state/store/selectors";
import {
  setLastWsPayloadPreview,
  setWsStatus,
} from "@/features/race-state/store/race-state-ui-slice";
import type {
  RaceDashboard,
  RaceStateWsMessage,
} from "@/features/race-state/types/race-state";
import { useAppDispatch, useAppSelector } from "@/store/hooks";

const defaultWsUrl =
  process.env.NEXT_PUBLIC_WS_URL ?? "ws://localhost:8082/ws/race-state";

export function useRaceDashboardLive() {
  const dispatch = useAppDispatch();
  const wsUi = useAppSelector(selectRaceStateUi);
  const dashboardQuery = useGetDashboardQuery();
  const [liveDashboard, setLiveDashboard] = useState<RaceDashboard | null>(null);

  useEffect(() => {
    if (dashboardQuery.data) {
      setLiveDashboard(dashboardQuery.data);
    }
  }, [dashboardQuery.data]);

  useWebSocket(defaultWsUrl, {
    onStatusChange: (status) => {
      dispatch(setWsStatus(status));
    },
    onMessage: (event) => {
      dispatch(setLastWsPayloadPreview(event.data.slice(0, 280)));

      try {
        const message = JSON.parse(event.data) as RaceStateWsMessage;
        if (message.type === "race.state.updated") {
          setLiveDashboard(message.dashboard);
        }
      } catch {
        void dashboardQuery.refetch();
      }
    },
  });

  const sessionCards = useMemo(
    () => buildSessionCards(liveDashboard?.session, wsUi.wsStatus),
    [liveDashboard?.session, wsUi.wsStatus]
  );

  const leaderboardRows = useMemo(
    () =>
      liveDashboard
        ? buildRaceDashboardRows(liveDashboard)
        : [],
    [liveDashboard]
  );

  const mapPositions = useMemo(
    () =>
      liveDashboard
        ? buildTrackMapPositions(liveDashboard)
        : [],
    [liveDashboard]
  );

  return {
    ...dashboardQuery,
    data: liveDashboard,
    wsUi,
    sessionCards,
    leaderboardRows,
    mapPositions,
  };
}
