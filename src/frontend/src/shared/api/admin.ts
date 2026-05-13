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
