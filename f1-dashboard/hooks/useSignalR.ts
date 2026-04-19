"use client";

import type { RaceStateWebSocketStatus } from "@/features/store/race-state/raceStateUiSlice";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useCallback, useEffect, useRef, useState } from "react";

type SignalRHandler = (payload: unknown) => void;

type SignalROptions = {
  enabled?: boolean;
  handlers?: Record<string, SignalRHandler>;
  onStatusChange?: (status: RaceStateWebSocketStatus) => void;
};

export const useSignalR = (url: string | null, options?: SignalROptions) => {
  const enabled = options?.enabled ?? true;
  const handlersRef = useRef<Record<string, SignalRHandler>>(options?.handlers ?? {});
  const onStatusChangeRef = useRef(options?.onStatusChange);
  const connectionRef = useRef<HubConnection | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const connectDelayTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const effectRunIdRef = useRef(0);
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
    const effectRunId = ++effectRunIdRef.current;

    const scheduleReconnect = () => {
      if (disposed || effectRunId !== effectRunIdRef.current) {
        return;
      }

      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }

      reconnectTimeoutRef.current = setTimeout(() => {
        void connect();
      }, 1500);
    };

    const connect = async () => {
      if (disposed || effectRunId !== effectRunIdRef.current) {
        return;
      }

      updateStatus("connecting");
      const connection = new HubConnectionBuilder()
        .withUrl(url, {
          withCredentials: false,
        })
        .withAutomaticReconnect([0, 1500, 3000, 5000])
        .configureLogging(LogLevel.Warning)
        .build();

      connectionRef.current = connection;

      connection.onreconnecting(() => {
        if (!disposed && effectRunId === effectRunIdRef.current) {
          updateStatus("connecting");
        }
      });
      connection.onreconnected(() => {
        if (!disposed && effectRunId === effectRunIdRef.current) {
          updateStatus("open");
        }
      });
      connection.onclose((error) => {
        if (disposed || effectRunId !== effectRunIdRef.current) {
          return;
        }

        connectionRef.current = null;
        updateStatus(error ? "error" : "closed");
        scheduleReconnect();
      });

      for (const eventName of Object.keys(handlersRef.current)) {
        connection.on(eventName, (payload: unknown) => {
          handlersRef.current[eventName]?.(payload);
        });
      }

      try {
        await connection.start();
        if (disposed || effectRunId !== effectRunIdRef.current) {
          await connection.stop();
          return;
        }

        if (connection.state !== HubConnectionState.Connected) {
          return;
        }

        updateStatus("open");
      } catch {
        connectionRef.current = null;
        updateStatus("error");
        scheduleReconnect();
      }
    };

    // Delay the first connect slightly so React StrictMode's dev-only
    // mount/unmount cycle does not create and immediately tear down a socket.
    connectDelayTimeoutRef.current = setTimeout(() => {
      void connect();
    }, 100);

    return () => {
      disposed = true;
      if (connectDelayTimeoutRef.current) {
        clearTimeout(connectDelayTimeoutRef.current);
        connectDelayTimeoutRef.current = null;
      }
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

  const invoke = useCallback(async (methodName: string, ...args: unknown[]) => {
    if (!connectionRef.current) {
      return;
    }

    await connectionRef.current.invoke(methodName, ...args);
  }, []);

  return {
    status,
    connection: connectionRef.current,
    invoke,
  };
};
