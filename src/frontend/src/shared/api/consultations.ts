import client from './client';

export interface ConsultationRoomDto {
  id: string;
  appointmentId: string;
  roomCode: string;
  status: 'Waiting' | 'Active' | 'Ended';
  startedAt: string | null;
  endedAt: string | null;
  doctorJoinedAt: string | null;
  patientJoinedAt: string | null;
  notes: string | null;
  doctorName: string;
  patientName: string;
  reason: string;
  durationMinutes: number;
}

export async function getConsultationRoom(appointmentId: string): Promise<ConsultationRoomDto> {
  const { data } = await client.get<ConsultationRoomDto>(`/api/v1/consultations/${appointmentId}`);
  return data;
}
