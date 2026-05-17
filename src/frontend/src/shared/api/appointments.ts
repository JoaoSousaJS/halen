import client from './client';

export interface DoctorDto {
  id: string;
  name: string;
  specialty: string;
  consultationFee: number;
  yearsOfExperience: number;
}

export type AppointmentStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';

export interface AppointmentDto {
  id: string;
  scheduledAt: string;
  durationMinutes: number;
  reason: string;
  status: AppointmentStatus;
  notes: string | null;
  doctorName: string;
  specialty: string;
  consultationFee: number;
  patientName: string;
  patientId: string;
}

export interface BookAppointmentPayload {
  doctorId: string;
  scheduledAt: string;
  reason: string;
}

export async function listDoctors(): Promise<DoctorDto[]> {
  const { data } = await client.get<{ doctors: DoctorDto[]; totalCount: number }>('/api/v1/appointments/doctors');
  return data.doctors;
}

export async function getMyAppointments(): Promise<AppointmentDto[]> {
  const { data } = await client.get<{ appointments: AppointmentDto[]; totalCount: number }>('/api/v1/appointments');
  return data.appointments;
}

export async function bookAppointment(payload: BookAppointmentPayload): Promise<{ appointmentId: string }> {
  const { data } = await client.post<{ appointmentId: string }>('/api/v1/appointments', payload);
  return data;
}

export async function cancelAppointment(id: string): Promise<void> {
  await client.post(`/api/v1/appointments/${id}/cancel`);
}

export async function completeAppointment(id: string, notes?: string): Promise<void> {
  await client.post(`/api/v1/appointments/${id}/complete`, { notes });
}
