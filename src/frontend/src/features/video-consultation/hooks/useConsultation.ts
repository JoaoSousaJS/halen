import { useCallback, useEffect, useRef, useState } from 'react';
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr';

export interface ConsultationState {
  phase: 'idle' | 'connecting' | 'lobby' | 'active' | 'reconnecting' | 'ended';
  roomCode: string | null;
  participants: { name: string; role: string }[];
  chatMessages: { from: string; role: string; text: string; sentAt: string }[];
  notes: string;
  elapsedSeconds: number;
  error: string | null;
  localControls: {
    mic: boolean;
    cam: boolean;
    chatOpen: boolean;
    sidebarOpen: boolean;
  };
}

const initialState: ConsultationState = {
  phase: 'idle',
  roomCode: null,
  participants: [],
  chatMessages: [],
  notes: '',
  elapsedSeconds: 0,
  error: null,
  localControls: {
    mic: true,
    cam: true,
    chatOpen: false,
    sidebarOpen: false,
  },
};

export function useConsultation(appointmentId: string, token: string | null) {
  const [state, setState] = useState<ConsultationState>(initialState);
  const connectionRef = useRef<HubConnection | null>(null);

  const setError = (msg: string) => {
    setState((prev) => ({ ...prev, error: msg }));
  };

  useEffect(() => {
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/consultation', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on('ParticipantJoined', (participant: { name: string; role: string }) => {
      setState((prev) => ({
        ...prev,
        participants: [...prev.participants, participant],
      }));
    });

    connection.on('ParticipantLeft', (participant: { name: string }) => {
      setState((prev) => ({
        ...prev,
        participants: prev.participants.filter((p) => p.name !== participant.name),
      }));
    });

    connection.on('ConsultationStarted', (data: { roomCode: string }) => {
      setState((prev) => ({
        ...prev,
        phase: 'active',
        roomCode: data.roomCode,
      }));
    });

    connection.on(
      'ReceiveChat',
      (message: { from: string; role: string; text: string; sentAt: string }) => {
        setState((prev) => ({
          ...prev,
          chatMessages: [...prev.chatMessages, message],
        }));
      },
    );

    connection.on('NotesUpdated', (data: { notes: string }) => {
      setState((prev) => ({
        ...prev,
        notes: data.notes,
      }));
    });

    connection.on('ConsultationEnded', () => {
      setState((prev) => ({
        ...prev,
        phase: 'ended',
      }));
    });

    connection.onreconnecting(() => {
      setState((prev) => ({ ...prev, phase: 'reconnecting' }));
    });

    connection.onreconnected(() => {
      setState((prev) => ({
        ...prev,
        phase: prev.phase === 'reconnecting' ? 'lobby' : prev.phase,
      }));
    });

    setState((prev) => ({ ...prev, phase: 'connecting' }));

    connection
      .start()
      .then(() => {
        setState((prev) => ({ ...prev, phase: 'lobby', error: null }));
      })
      .catch(() => {
        setState((prev) => ({
          ...prev,
          phase: 'idle',
          error: 'Failed to connect to consultation. Please try again.',
        }));
      });

    return () => {
      connection.stop();
      connectionRef.current = null;
    };
  }, [token]);

  useEffect(() => {
    if (state.phase !== 'active') return;

    const interval = setInterval(() => {
      setState((prev) => ({ ...prev, elapsedSeconds: prev.elapsedSeconds + 1 }));
    }, 1000);

    return () => clearInterval(interval);
  }, [state.phase]);

  const joinRoom = useCallback(() => {
    const conn = connectionRef.current;
    if (conn && conn.state === HubConnectionState.Connected) {
      conn.invoke('JoinRoom', appointmentId).catch(() => setError('Failed to join room.'));
    }
  }, [appointmentId]);

  const sendChat = useCallback((text: string) => {
    const conn = connectionRef.current;
    if (conn && conn.state === HubConnectionState.Connected) {
      conn.invoke('SendChat', appointmentId, text).catch(() => setError('Failed to send message.'));
    }
  }, [appointmentId]);

  const updateNotes = useCallback((notes: string) => {
    const conn = connectionRef.current;
    if (conn && conn.state === HubConnectionState.Connected) {
      conn.invoke('UpdateNotes', appointmentId, notes).catch(() => setError('Failed to update notes.'));
    }
  }, [appointmentId]);

  const endConsultation = useCallback(() => {
    const conn = connectionRef.current;
    if (conn && conn.state === HubConnectionState.Connected) {
      conn
        .invoke('EndConsultation', appointmentId, null)
        .catch(() => setError('Failed to end consultation.'));
    }
  }, [appointmentId]);

  const toggleMic = useCallback(() => {
    setState((prev) => ({
      ...prev,
      localControls: { ...prev.localControls, mic: !prev.localControls.mic },
    }));
  }, []);

  const toggleCam = useCallback(() => {
    setState((prev) => ({
      ...prev,
      localControls: { ...prev.localControls, cam: !prev.localControls.cam },
    }));
  }, []);

  const toggleChat = useCallback(() => {
    setState((prev) => ({
      ...prev,
      localControls: { ...prev.localControls, chatOpen: !prev.localControls.chatOpen },
    }));
  }, []);

  const toggleSidebar = useCallback(() => {
    setState((prev) => ({
      ...prev,
      localControls: { ...prev.localControls, sidebarOpen: !prev.localControls.sidebarOpen },
    }));
  }, []);

  return {
    state,
    joinRoom,
    sendChat,
    updateNotes,
    endConsultation,
    toggleMic,
    toggleCam,
    toggleChat,
    toggleSidebar,
  };
}
