import client from './client';

export type DayOfWeek = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday';

export interface AvailabilitySlot {
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
}

export interface AvailabilityWindow {
  id: string;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

export interface TimeSlot {
  startUtc: string;
  startLocal: string;
  isAvailable: boolean;
}

export async function getMyAvailability(): Promise<AvailabilityWindow[]> {
  const { data } = await client.get<{ windows: AvailabilityWindow[] }>('/api/v1/availability/mine');
  return data.windows;
}

export async function setMyAvailability(slots: AvailabilitySlot[]): Promise<void> {
  await client.put('/api/v1/availability/mine', { slots });
}

export async function getDoctorAvailability(doctorId: string): Promise<AvailabilityWindow[]> {
  const { data } = await client.get<{ windows: AvailabilityWindow[] }>(`/api/v1/availability/${doctorId}`);
  return data.windows;
}

export async function getAvailableSlots(doctorId: string, date: string): Promise<TimeSlot[]> {
  const { data } = await client.get<{ slots: TimeSlot[] }>(`/api/v1/availability/${doctorId}/slots`, { params: { date } });
  return data.slots;
}
