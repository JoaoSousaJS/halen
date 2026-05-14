import client from './client';

export interface CreateDoctorPayload {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  specialty: string;
  licenseNumber: string;
  consultationFee: number;
  yearsOfExperience: number;
}

export async function createDoctor(payload: CreateDoctorPayload): Promise<{ doctorId: string }> {
  const { data } = await client.post<{ doctorId: string }>('/api/v1/admin/doctors', payload);
  return data;
}

export interface AdminUserDto {
  id: string;
  name: string;
  role: string;
  status: string;
  plan: string | null;
  lastLoginAt: string | null;
  isFlagged: boolean;
}

export interface ListUsersParams {
  role?: string;
  search?: string;
  flaggedOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export interface ListUsersResponse {
  users: AdminUserDto[];
  totalCount: number;
}

export async function listUsers(params: ListUsersParams = {}): Promise<ListUsersResponse> {
  const { data } = await client.get<ListUsersResponse>('/api/v1/admin/users', { params });
  return data;
}
