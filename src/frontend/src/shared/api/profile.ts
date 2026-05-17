import client from './client';

export interface ProfileDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  createdAt: string;
  lastLoginAt: string | null;
  specialty: string | null;
  consultationFee: number | null;
  yearsOfExperience: number | null;
  languages: string[] | null;
  dateOfBirth: string | null;
  city: string | null;
  subscriptionPlan: string | null;
}

export interface UpdateProfilePayload {
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  city?: string | null;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
}

export async function getMyProfile(): Promise<ProfileDto> {
  const { data } = await client.get<{ profile: ProfileDto }>('/api/v1/profile/me');
  return data.profile;
}

export async function updateMyProfile(payload: UpdateProfilePayload): Promise<void> {
  await client.put('/api/v1/profile/me', payload);
}

export async function changePassword(payload: ChangePasswordPayload): Promise<void> {
  await client.post('/api/v1/profile/me/change-password', payload);
}
