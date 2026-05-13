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

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (!token) return config;

  if (isTokenExpired(token)) {
    localStorage.removeItem('token');
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
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default client;
