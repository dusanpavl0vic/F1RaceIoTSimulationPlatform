"use client";

import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";
import type { RaceStateWebSocketStatus } from "@/features/race-state/store/race-state-ui-slice";

type SignalRHandler = (payload: unknown) => void;

type SignalROptions = {
  enabled?: boolean;
  handlers?: Record<string, SignalRHandler>;
  onStatusChange?: (status: RaceStateWebSocketStatus) => void;
};

export function useSignalR(url: string | null, options?: SignalROptions) {
  const enabled = options?.enabled ?? true;
  const handlersRef = useRef<Record<string, SignalRHandler>>(options?.handlers ?? {});
  const onStatusChangeRef = useRef(options?.onStatusChange);
  const connectionRef = useRef<HubConnection | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [status, setStatus] = useState<RaceStateWebSocketStatus>("idle");

  const updateStatus = (nextStatus: RaceStateWebSocketStatus) => {
    setStatus(nextStatus);
    onStatusChangeRef.current?.(nextStatus);
  };

  useEffect(() => {
    handlersRef.current = options?.handlers ?? {};
  }, [options?.handlers]);

  useEffect(() => {
    onStatusChangeRef.current = options?.onStatusChange;
  }, [options?.onStatusChange]);

  useEffect(() => {
    if (!enabled || !url) {
      updateStatus("idle");
      return;
    }

    let disposed = false;

    const connect = async () => {
      if (disposed) {
        return;
      }

      updateStatus("connecting");

      const connection = new HubConnectionBuilder()
        .withUrl(url, {
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
          withCredentials: false,
        })
        .withAutomaticReconnect([0, 1500, 3000, 5000])
        .configureLogging(LogLevel.Warning)
        .build();

      connectionRef.current = connection;

      connection.onreconnecting(() => updateStatus("connecting"));
      connection.onreconnected(() => updateStatus("open"));
      connection.onclose(() => {
        updateStatus("closed");

        if (disposed) {
          return;
        }

        reconnectTimeoutRef.current = setTimeout(() => {
          void connect();
        }, 1500);
      });

      for (const eventName of Object.keys(handlersRef.current)) {
        connection.on(eventName, (payload: unknown) => {
          handlersRef.current[eventName]?.(payload);
        });
      }

      try {
        await connection.start();
        if (disposed || connection.state !== HubConnectionState.Connected) {
          return;
        }

        updateStatus("open");
      } catch {
        updateStatus("error");
        if (!disposed) {
          reconnectTimeoutRef.current = setTimeout(() => {
            void connect();
          }, 1500);
        }
      }
    };

    void connect();

    return () => {
      disposed = true;
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
        reconnectTimeoutRef.current = null;
      }

      const connection = connectionRef.current;
      connectionRef.current = null;
      if (connection) {
        void connection.stop();
      }
    };
  }, [enabled, url]);

  return {
    status,
    connection: connectionRef.current,
    invoke: async (methodName: string, ...args: unknown[]) => {
      if (!connectionRef.current) {
        return;
      }

      await connectionRef.current.invoke(methodName, ...args);
    },
  };
}
