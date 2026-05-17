import { renderHook, act } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

type EventHandler = (...args: unknown[]) => void;

const handlers = new Map<string, EventHandler>();
const mockInvoke = vi.fn().mockResolvedValue(undefined);
const mockStart = vi.fn().mockResolvedValue(undefined);
const mockStop = vi.fn().mockResolvedValue(undefined);
const mockOn = vi.fn((event: string, handler: EventHandler) => {
  handlers.set(event, handler);
});

vi.mock('@microsoft/signalr', () => {
  class MockHubConnectionBuilder {
    withUrl() { return this; }
    withAutomaticReconnect() { return this; }
    configureLogging() { return this; }
    build() {
      return {
        on: mockOn,
        start: mockStart,
        stop: mockStop,
        invoke: mockInvoke,
        onreconnecting: vi.fn(),
        onreconnected: vi.fn(),
        state: 'Connected',
      };
    }
  }
  return {
    HubConnectionBuilder: MockHubConnectionBuilder,
    HubConnectionState: { Disconnected: 'Disconnected', Connected: 'Connected' },
    LogLevel: { Warning: 3 },
  };
});

vi.mock('../../../shared/api/consultations', () => ({
  getConsultationRoom: vi.fn().mockResolvedValue({
    id: 'room-1',
    appointmentId: 'apt-1',
    roomCode: 'VC-TEST',
    status: 'Waiting',
    doctorName: 'Dr House',
    patientName: 'Pat Ient',
    reason: 'Checkup',
    durationMinutes: 20,
    notes: null,
    startedAt: null,
    endedAt: null,
    doctorJoinedAt: null,
    patientJoinedAt: null,
  }),
}));

beforeEach(() => {
  vi.useFakeTimers();
  handlers.clear();
  vi.clearAllMocks();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('useConsultation', () => {
  it('starts in idle phase when no token provided', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', null));

    expect(result.current.state.phase).toBe('idle');
  });

  it('transitions to lobby after connection starts', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    // Wait for connection to establish
    await act(async () => {
      await vi.runAllTimersAsync();
    });

    expect(result.current.state.phase).toBe('lobby');
    expect(mockStart).toHaveBeenCalled();
  });

  it('adds participant on ParticipantJoined event', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('ParticipantJoined')?.({ name: 'Dr House', role: 'Doctor' });
    });

    expect(result.current.state.participants).toHaveLength(1);
    expect(result.current.state.participants[0].name).toBe('Dr House');
  });

  it('removes participant on ParticipantLeft event', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('ParticipantJoined')?.({ name: 'Dr House', role: 'Doctor' });
    });

    expect(result.current.state.participants).toHaveLength(1);

    act(() => {
      handlers.get('ParticipantLeft')?.({ name: 'Dr House', role: 'Doctor' });
    });

    expect(result.current.state.participants).toHaveLength(0);
  });

  it('transitions to active on ConsultationStarted event', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('ConsultationStarted')?.({ roomCode: 'VC-TEST', startedAt: new Date().toISOString() });
    });

    expect(result.current.state.phase).toBe('active');
  });

  it('accumulates messages on ReceiveChat event', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('ReceiveChat')?.({ from: 'Dr House', role: 'Doctor', text: 'Hello', sentAt: new Date().toISOString() });
    });

    expect(result.current.state.chatMessages).toHaveLength(1);
    expect(result.current.state.chatMessages[0].text).toBe('Hello');
  });

  it('updates notes on NotesUpdated event', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('NotesUpdated')?.({ notes: 'Patient has a headache' });
    });

    expect(result.current.state.notes).toBe('Patient has a headache');
  });

  it('transitions to ended on ConsultationEnded event', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('ConsultationEnded')?.({ endedAt: new Date().toISOString(), appointmentId: 'apt-1' });
    });

    expect(result.current.state.phase).toBe('ended');
  });

  it('increments elapsedSeconds when phase is active', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('ConsultationStarted')?.({ roomCode: 'VC-TEST', startedAt: new Date().toISOString() });
    });

    expect(result.current.state.elapsedSeconds).toBe(0);

    act(() => { vi.advanceTimersByTime(3000); });

    expect(result.current.state.elapsedSeconds).toBe(3);
  });

  it('toggleMic updates localControls', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    expect(result.current.state.localControls.mic).toBe(true);

    act(() => { result.current.toggleMic(); });

    expect(result.current.state.localControls.mic).toBe(false);
  });

  it('toggleCam updates localControls', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    expect(result.current.state.localControls.cam).toBe(true);

    act(() => { result.current.toggleCam(); });

    expect(result.current.state.localControls.cam).toBe(false);
  });

  it('toggleChat updates localControls', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    expect(result.current.state.localControls.chatOpen).toBe(false);

    act(() => { result.current.toggleChat(); });

    expect(result.current.state.localControls.chatOpen).toBe(true);
  });

  it('toggleSidebar updates localControls', async () => {
    const { useConsultation } = await import('../hooks/useConsultation');
    const { result } = renderHook(() => useConsultation('apt-1', 'test-token'));

    expect(result.current.state.localControls.sidebarOpen).toBe(false);

    act(() => { result.current.toggleSidebar(); });

    expect(result.current.state.localControls.sidebarOpen).toBe(true);
  });
});
