import { renderHook, act } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import type { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from '../components/AuthProvider';

type NotificationHandler = (notification: { message: string; type: string }) => void;

let onHandler: NotificationHandler | null = null;
const mockStart = vi.fn().mockResolvedValue(undefined);
const mockStop = vi.fn();
const mockOn = vi.fn((event: string, handler: NotificationHandler) => {
  if (event === 'ReceiveNotification') onHandler = handler;
});

vi.mock('@microsoft/signalr', () => {
  class MockHubConnectionBuilder {
    withUrl() { return this; }
    withAutomaticReconnect() { return this; }
    configureLogging() { return this; }
    build() {
      return { on: mockOn, start: mockStart, stop: mockStop, state: 'Connected' };
    }
  }
  return {
    HubConnectionBuilder: MockHubConnectionBuilder,
    HubConnectionState: { Disconnected: 'Disconnected', Connected: 'Connected' },
    LogLevel: { Warning: 3 },
  };
});

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '1', email: 'test@test.com', given_name: 'Test', family_name: 'User', role: 'Patient', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

function createWrapper() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    );
  };
}

beforeEach(() => {
  vi.useFakeTimers();
  onHandler = null;
  localStorage.setItem('token', fakeJwt());
  vi.clearAllMocks();
});

afterEach(() => {
  vi.useRealTimers();
  localStorage.clear();
});

describe('useNotifications', () => {
  it('connects to SignalR when token exists', async () => {
    const { useNotifications } = await import('./useNotifications');
    renderHook(() => useNotifications(), { wrapper: createWrapper() });

    expect(mockStart).toHaveBeenCalled();
    expect(mockOn).toHaveBeenCalledWith('ReceiveNotification', expect.any(Function));
  });

  it('adds toast when notification received', async () => {
    const { useNotifications } = await import('./useNotifications');
    const { result } = renderHook(() => useNotifications(), { wrapper: createWrapper() });

    act(() => {
      onHandler?.({ message: 'Appointment booked', type: 'booked' });
    });

    expect(result.current.toasts).toHaveLength(1);
    expect(result.current.toasts[0].message).toBe('Appointment booked');
    expect(result.current.toasts[0].type).toBe('booked');
  });

  it('dismissToast removes a toast by id', async () => {
    const { useNotifications } = await import('./useNotifications');
    const { result } = renderHook(() => useNotifications(), { wrapper: createWrapper() });

    act(() => {
      onHandler?.({ message: 'Toast 1', type: 'info' });
    });

    const toastId = result.current.toasts[0].id;

    act(() => {
      result.current.dismissToast(toastId);
    });

    expect(result.current.toasts).toHaveLength(0);
  });

  it('auto-dismisses toast after 6 seconds', async () => {
    const { useNotifications } = await import('./useNotifications');
    const { result } = renderHook(() => useNotifications(), { wrapper: createWrapper() });

    act(() => {
      onHandler?.({ message: 'Temporary toast', type: 'info' });
    });

    expect(result.current.toasts).toHaveLength(1);

    act(() => {
      vi.advanceTimersByTime(6000);
    });

    expect(result.current.toasts).toHaveLength(0);
  });

  it('caps toasts at 5', async () => {
    const { useNotifications } = await import('./useNotifications');
    const { result } = renderHook(() => useNotifications(), { wrapper: createWrapper() });

    act(() => {
      for (let i = 0; i < 7; i++) {
        onHandler?.({ message: `Toast ${i}`, type: 'info' });
      }
    });

    expect(result.current.toasts).toHaveLength(5);
    expect(result.current.toasts[0].message).toBe('Toast 2');
    expect(result.current.toasts[4].message).toBe('Toast 6');
  });

  it('stops connection on unmount', async () => {
    const { useNotifications } = await import('./useNotifications');
    const { unmount } = renderHook(() => useNotifications(), { wrapper: createWrapper() });

    unmount();

    expect(mockStop).toHaveBeenCalled();
  });
});
