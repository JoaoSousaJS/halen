import { createContext, useContext, useState } from 'react';
import type { ReactNode } from 'react';

interface JwtPayload {
  sub: string;
  email: string;
  given_name: string;
  family_name: string;
  role: string;
}

interface AuthContextValue {
  token: string | null;
  user: JwtPayload | null;
  saveToken: (token: string) => void;
  logout: () => void;
}

function parseToken(token: string): JwtPayload | null {
  try {
    return JSON.parse(atob(token.split('.')[1])) as JwtPayload;
  } catch {
    return null;
  }
}

// client-localstorage-schema: wrap every localStorage call in try-catch.
// getItem/setItem throw in Safari incognito, when storage is disabled, or quota exceeded.
function storageGet(key: string): string | null {
  try { return localStorage.getItem(key); } catch { return null; }
}
function storageSet(key: string, value: string): void {
  try { localStorage.setItem(key, value); } catch { /* storage unavailable */ }
}
function storageRemove(key: string): void {
  try { localStorage.removeItem(key); } catch { /* storage unavailable */ }
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => {
    const stored = storageGet('token');
    if (stored && !parseToken(stored)) {
      // Token exists but is malformed — clear it rather than leaving a corrupt credential.
      storageRemove('token');
      return null;
    }
    return stored;
  });

  const user = token ? parseToken(token) : null;

  function saveToken(newToken: string) {
    storageSet('token', newToken);
    setToken(newToken);
  }

  function logout() {
    storageRemove('token');
    setToken(null);
  }

  return (
    <AuthContext.Provider value={{ token, user, saveToken, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
