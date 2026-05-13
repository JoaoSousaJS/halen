import axios from 'axios';

// No absolute URL needed — Vite proxies /api/* to the backend in dev,
// and in production the same server serves both.
const client = axios.create({
  baseURL: '/',
});

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default client;
