import { useEffect, useRef, useState, useCallback } from 'react';
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../components/AuthProvider';
import type { NotificationDto } from '../api/notifications';

export interface Toast {
  id: string;
  message: string;
  type: string;
  timestamp: number;
}

const TOAST_DURATION_MS = 6000;
const MAX_TOASTS = 5;

export function useNotifications() {
  const { token } = useAuth();
  const queryClient = useQueryClient();
  const [toasts, setToasts] = useState<Toast[]>([]);
  const timersRef = useRef<Set<ReturnType<typeof setTimeout>>>(new Set());

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  useEffect(() => {
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (notification: NotificationDto) => {
      const toast: Toast = {
        id: crypto.randomUUID(),
        message: notification.message,
        type: notification.type,
        timestamp: Date.now(),
      };

      setToasts((prev) => [...prev, toast].slice(-MAX_TOASTS));

      const timer = setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== toast.id));
        timersRef.current.delete(timer);
      }, TOAST_DURATION_MS);
      timersRef.current.add(timer);

      queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
    });

    connection.start().catch(() => {
      // SignalR unavailable — app works fine without real-time updates.
    });

    return () => {
      for (const timer of timersRef.current) clearTimeout(timer);
      timersRef.current.clear();
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop();
      }
    };
  }, [token, queryClient]);

  return { toasts, dismissToast };
}
