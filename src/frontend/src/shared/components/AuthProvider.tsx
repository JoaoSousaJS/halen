import { createContext, ReactNode, useContext, useState } from 'react';

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

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'));

  const user = token ? parseToken(token) : null;

  function saveToken(newToken: string) {
    localStorage.setItem('token', newToken);
    setToken(newToken);
  }

  function logout() {
    localStorage.removeItem('token');
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
