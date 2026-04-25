"use client";

import {
  BATTLE_ALERT_EVENT,
  DEFAULT_SIGNALR_URL,
  RACE_STATE_UPDATED_EVENT,
  WS_PAYLOAD_PREVIEW_MAX_LENGTH,
} from "@/constants/race-state";
import {
  setRaceStateLiveSnapshot,
} from "@/features/store/race-state/raceStateLiveSlice";
import { setRaceStateTelemetryMetadata } from "@/features/store/race-state/raceStateTelemetrySlice";
import {
  enqueueBattleAlert,
  setLastWsPayloadPreview,
  setWsStatus,
} from "@/features/store/race-state/raceStateUiSlice";
import type {
  RaceBattleAlertMessage,
  RaceStateWsMessage,
} from "@/features/store/race-state/raceStateTypes";
import { useSignalR } from "@/hooks/useSignalR";
import { useDispatch } from "react-redux";

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

export function RaceStateLiveBridge() {
  const dispatch = useDispatch();

  useSignalR(defaultSignalRUrl, {
    enabled: !useDashboardMocks,
    onStatusChange: (status) => {
      dispatch(setWsStatus(status));
    },
    handlers: {
      [RACE_STATE_UPDATED_EVENT]: (payload) => {
        try {
          const message = payload as RaceStateWsMessage;
          if (message.type !== RACE_STATE_UPDATED_EVENT) {
            return;
          }

          dispatch(
            setLastWsPayloadPreview(
              JSON.stringify(message).slice(0, WS_PAYLOAD_PREVIEW_MAX_LENGTH)
            )
          );
          dispatch(
            setRaceStateLiveSnapshot({
              dashboard: message.dashboard,
              currentState: message.currentState ?? null,
            })
          );
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
        } catch {
          dispatch(setWsStatus("error"));
        }
      },
      [BATTLE_ALERT_EVENT]: (payload) => {
        try {
          const message = payload as RaceBattleAlertMessage;
          if (message.type !== BATTLE_ALERT_EVENT) {
            return;
          }

          dispatch(
            setLastWsPayloadPreview(
              JSON.stringify(message).slice(0, WS_PAYLOAD_PREVIEW_MAX_LENGTH)
            )
          );
          dispatch(
            enqueueBattleAlert({
              id: `${message.sessionId}:${message.driverNumber}:${message.aheadDriverNumber}:${message.sentAt}`,
              message: message.message,
              sentAt: message.sentAt,
              sessionId: message.sessionId,
              driverNumber: message.driverNumber,
              aheadDriverNumber: message.aheadDriverNumber,
              battleForPosition: message.battleForPosition,
              gapLabel: message.gapLabel,
            })
          );
        } catch {
          dispatch(setWsStatus("error"));
        }
      },
    },
  });

  return null;
}
