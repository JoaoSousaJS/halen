import axios from 'axios';

// No absolute URL needed — Vite proxies /api/* to the backend in dev,
// and in production the same server serves both.
const client = axios.create({
  baseURL: '/',
});

function isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return typeof payload.exp === 'number' && payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
}

// client-localstorage-schema: wrap localStorage in try-catch — throws in Safari incognito,
// disabled storage, or quota exceeded.
function storageGet(key: string): string | null {
  try { return localStorage.getItem(key); } catch { return null; }
}
function storageRemove(key: string): void {
  try { localStorage.removeItem(key); } catch { /* storage unavailable */ }
}

client.interceptors.request.use((config) => {
  const token = storageGet('token');
  if (!token) return config;

  if (isTokenExpired(token)) {
    storageRemove('token');
    window.location.href = '/login';
    return Promise.reject(new Error('Session expired'));
  }

  config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Fallback for tokens the client-side expiry check didn't catch (clock skew, revocation, etc.)
client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      storageRemove('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default client;
