"use client";

import { useEffect, useRef, useState } from "react";
import type { RaceStateWebSocketStatus } from "@/features/race-state/store/race-state-ui-slice";

type WebSocketOptions = {
  enabled?: boolean;
  onMessage?: (event: MessageEvent<string>) => void;
  onStatusChange?: (status: RaceStateWebSocketStatus) => void;
};

export function useWebSocket(url: string | null, options?: WebSocketOptions) {
  const enabled = options?.enabled ?? true;
  const onMessageRef = useRef(options?.onMessage);
  const onStatusChangeRef = useRef(options?.onStatusChange);
  const socketRef = useRef<WebSocket | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [status, setStatus] = useState<RaceStateWebSocketStatus>("idle");

  const updateStatus = (nextStatus: RaceStateWebSocketStatus) => {
    setStatus(nextStatus);
    onStatusChangeRef.current?.(nextStatus);
  };

  useEffect(() => {
    onMessageRef.current = options?.onMessage;
  }, [options?.onMessage]);

  useEffect(() => {
    onStatusChangeRef.current = options?.onStatusChange;
  }, [options?.onStatusChange]);

  useEffect(() => {
    if (!enabled || !url) {
      updateStatus("idle");
      return;
    }

    let disposed = false;

    const connect = () => {
      if (disposed) {
        return;
      }

      const socket = new WebSocket(url);
      socketRef.current = socket;
      updateStatus("connecting");

      socket.onopen = () => updateStatus("open");
      socket.onerror = () => updateStatus("error");
      socket.onmessage = (event) => onMessageRef.current?.(event);
      socket.onclose = () => {
        updateStatus("closed");

        if (disposed) {
          return;
        }

        reconnectTimeoutRef.current = setTimeout(() => {
          connect();
        }, 1500);
      };
    };

    connect();

    return () => {
      disposed = true;
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
        reconnectTimeoutRef.current = null;
      }

      const socket = socketRef.current;
      socketRef.current = null;
      socket?.close();
      socketRef.current = null;
    };
  }, [enabled, url]);

  return {
    status,
    socket: socketRef.current,
    send: (message: string) => socketRef.current?.send(message),
  };
}
