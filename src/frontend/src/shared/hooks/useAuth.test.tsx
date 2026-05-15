import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import type { ReactNode } from 'react';
import { AuthProvider, useAuth } from '../components/AuthProvider';

function fakeJwt(payload: object): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fake`;
}

const validToken = fakeJwt({
  sub: '1',
  email: 'test@example.com',
  given_name: 'Test',
  family_name: 'User',
  role: 'Patient',
});

function wrapper({ children }: { children: ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

beforeEach(() => localStorage.clear());

describe('useAuth', () => {
  it('starts with null when no token in storage', () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.token).toBeNull();
    expect(result.current.user).toBeNull();
  });

  it('reads token from localStorage on mount', () => {
    localStorage.setItem('token', validToken);

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.token).toBe(validToken);
    expect(result.current.user?.email).toBe('test@example.com');
    expect(result.current.user?.role).toBe('Patient');
  });

  it('saveToken stores token and updates user', () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    act(() => result.current.saveToken(validToken));

    expect(result.current.token).toBe(validToken);
    expect(result.current.user?.given_name).toBe('Test');
    expect(localStorage.getItem('token')).toBe(validToken);
  });

  it('logout clears token and user', () => {
    localStorage.setItem('token', validToken);
    const { result } = renderHook(() => useAuth(), { wrapper });

    act(() => result.current.logout());

    expect(result.current.token).toBeNull();
    expect(result.current.user).toBeNull();
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('clears malformed token from storage on mount', () => {
    localStorage.setItem('token', 'not-a-valid-jwt');

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.token).toBeNull();
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('throws when used outside AuthProvider', () => {
    expect(() => {
      renderHook(() => useAuth());
    }).toThrow('useAuth must be used inside AuthProvider');
  });
});
