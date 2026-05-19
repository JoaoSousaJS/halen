import { useCallback, useEffect, useRef, useState } from 'react';
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr';

export function useChat(threadId: string, token: string | null) {
  const [connected, setConnected] = useState(false);
  const [typingUser, setTypingUser] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);
  const typingTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!token || !threadId) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on('UserTyping', (_threadId: string, userName: string) => {
      setTypingUser(userName);
      if (typingTimerRef.current) clearTimeout(typingTimerRef.current);
      typingTimerRef.current = setTimeout(() => setTypingUser(null), 3000);
    });

    connection.onreconnecting(() => setConnected(false));
    connection.onreconnected(() => {
      setConnected(true);
      connection.invoke('JoinThread', threadId).catch((e) => console.warn('JoinThread failed on reconnect', e));
    });

    connection
      .start()
      .then(() => {
        setConnected(true);
        return connection.invoke('JoinThread', threadId);
      })
      .catch((e) => {
        console.warn('Chat connection failed', e);
        setConnected(false);
      });

    return () => {
      if (connection.state === HubConnectionState.Connected) {
        connection.invoke('LeaveThread', threadId).catch((e) => console.warn('LeaveThread failed', e));
      }
      connection.stop();
      connectionRef.current = null;
      if (typingTimerRef.current) clearTimeout(typingTimerRef.current);
    };
  }, [token, threadId]);

  const sendTyping = useCallback(() => {
    const conn = connectionRef.current;
    if (conn && conn.state === HubConnectionState.Connected) {
      conn.invoke('SendTyping', threadId).catch((e) => console.warn('SendTyping failed', e));
    }
  }, [threadId]);

  return { connected, typingUser, sendTyping };
}
