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

beforeEach(() => {
  vi.useFakeTimers();
  handlers.clear();
  vi.clearAllMocks();
});

afterEach(() => {
  vi.useRealTimers();
  vi.restoreAllMocks();
});

describe('useChat', () => {
  it('starts disconnected when no token provided', async () => {
    const { useChat } = await import('../hooks/useChat');
    const { result } = renderHook(() => useChat('thread-1', null));

    expect(result.current.connected).toBe(false);
    expect(result.current.typingUser).toBeNull();
  });

  it('connects to hub and joins thread when token provided', async () => {
    const { useChat } = await import('../hooks/useChat');
    const { result } = renderHook(() => useChat('thread-1', 'test-token'));

    await act(async () => {
      await vi.runAllTimersAsync();
    });

    expect(mockStart).toHaveBeenCalled();
    expect(mockInvoke).toHaveBeenCalledWith('JoinThread', 'thread-1');
    expect(result.current.connected).toBe(true);
  });

  it('tracks typing user from UserTyping event', async () => {
    const { useChat } = await import('../hooks/useChat');
    const { result } = renderHook(() => useChat('thread-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => {
      handlers.get('UserTyping')?.('thread-1', 'Dr House');
    });

    expect(result.current.typingUser).toBe('Dr House');

    act(() => { vi.advanceTimersByTime(4000); });

    expect(result.current.typingUser).toBeNull();
  });

  it('sendTyping invokes hub method', async () => {
    const { useChat } = await import('../hooks/useChat');
    const { result } = renderHook(() => useChat('thread-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    act(() => { result.current.sendTyping(); });

    expect(mockInvoke).toHaveBeenCalledWith('SendTyping', 'thread-1');
  });

  it('leaves thread on cleanup', async () => {
    const { useChat } = await import('../hooks/useChat');
    const { result, unmount } = renderHook(() => useChat('thread-1', 'test-token'));

    await act(async () => { await vi.runAllTimersAsync(); });

    unmount();

    expect(mockInvoke).toHaveBeenCalledWith('LeaveThread', 'thread-1');
    expect(mockStop).toHaveBeenCalled();
  });
});
