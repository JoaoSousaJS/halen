import client from './client';

export interface RegisterPayload {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role: 0; // self-registration is always Patient; doctors are created by Admin
}

export interface AuthResponse {
  token: string;
}

export async function register(payload: RegisterPayload): Promise<AuthResponse> {
  const { data } = await client.post<AuthResponse>('/api/v1/auth/register', payload);
  return data;
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  const { data } = await client.post<AuthResponse>('/api/v1/auth/login', { email, password });
  return data;
}
