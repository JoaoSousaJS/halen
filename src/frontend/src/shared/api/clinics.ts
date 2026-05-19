import client from './client';

export interface FeatureFlagDto {
  featureKey: string;
  isEnabled: boolean;
}

export interface ClinicDto {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAt: string;
}

export interface ClinicDetailsDto {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  userCount: number;
  featureFlags: FeatureFlagDto[];
  createdAt: string;
}

export interface CreateClinicPayload {
  name: string;
  slug: string;
}

export interface UpdateClinicPayload {
  name: string;
  isActive: boolean;
}

export interface ListClinicsResponse {
  clinics: ClinicDto[];
  totalCount: number;
}

export interface ClinicUserDto {
  id: string;
  name: string;
  email: string;
  role: string;
  status: string;
  createdAt: string;
}

export interface ListClinicUsersResponse {
  users: ClinicUserDto[];
  totalCount: number;
}

export interface CreateUserInClinicPayload {
  email: string;
  firstName: string;
  lastName: string;
  temporaryPassword: string;
  role: number;
}

export async function getMyFeatures(): Promise<FeatureFlagDto[]> {
  const { data } = await client.get<FeatureFlagDto[]>('/api/v1/me/features');
  return data;
}

export async function listClinics(params: {
  search?: string;
  page?: number;
  pageSize?: number;
}): Promise<ListClinicsResponse> {
  const { data } = await client.get<ListClinicsResponse>('/api/v1/clinics', { params });
  return data;
}

export async function getClinic(id: string): Promise<ClinicDetailsDto> {
  const { data } = await client.get<ClinicDetailsDto>(`/api/v1/clinics/${id}`);
  return data;
}

export async function createClinic(payload: CreateClinicPayload): Promise<{ clinicId: string }> {
  const { data } = await client.post<{ clinicId: string }>('/api/v1/clinics', payload);
  return data;
}

export async function updateClinic(id: string, payload: UpdateClinicPayload): Promise<void> {
  await client.put(`/api/v1/clinics/${id}`, payload);
}

export async function setFeatureFlag(
  clinicId: string,
  featureKey: string,
  isEnabled: boolean,
): Promise<void> {
  await client.put(`/api/v1/clinics/${clinicId}/features/${encodeURIComponent(featureKey)}`, { isEnabled });
}

export async function getClinicFeatures(clinicId: string): Promise<FeatureFlagDto[]> {
  const { data } = await client.get<FeatureFlagDto[]>(`/api/v1/clinics/${clinicId}/features`);
  return data;
}

export async function listClinicUsers(params: {
  search?: string;
  role?: string;
  page?: number;
  pageSize?: number;
}): Promise<ListClinicUsersResponse> {
  const { data } = await client.get<ListClinicUsersResponse>('/api/v1/clinic/users', { params });
  return data;
}

export interface CreateClinicAdminPayload {
  email: string;
  firstName: string;
  lastName: string;
  temporaryPassword: string;
}

export async function createClinicAdmin(
  clinicId: string,
  payload: CreateClinicAdminPayload,
): Promise<{ userId: string }> {
  const { data } = await client.post<{ userId: string }>(
    `/api/v1/clinics/${clinicId}/admins`,
    payload,
  );
  return data;
}

export async function createUserInClinic(
  payload: CreateUserInClinicPayload,
): Promise<{ userId: string }> {
  const { data } = await client.post<{ userId: string }>('/api/v1/clinic/users', payload);
  return data;
}
