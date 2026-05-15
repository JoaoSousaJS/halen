import client from './client';

export type PrescriptionStatus = 'Active' | 'Completed' | 'Cancelled';

export interface PrescriptionDto {
  id: string;
  drugName: string;
  dosage: string;
  frequency: string;
  refillsRemaining: number;
  status: PrescriptionStatus;
  pharmacyName: string | null;
  doctorName: string;
  patientName: string;
  createdAt: string;
}

export interface IssuePrescriptionPayload {
  patientId: string;
  drugName: string;
  dosage: string;
  frequency: string;
  refillsRemaining: number;
  pharmacyName?: string;
}

export async function getMyPrescriptions(): Promise<PrescriptionDto[]> {
  const { data } = await client.get<PrescriptionDto[]>('/api/v1/prescriptions');
  return data;
}

export async function issuePrescription(
  payload: IssuePrescriptionPayload,
): Promise<{ prescriptionId: string }> {
  const { data } = await client.post<{ prescriptionId: string }>(
    '/api/v1/prescriptions',
    payload,
  );
  return data;
}

export async function cancelPrescription(id: string): Promise<void> {
  await client.post(`/api/v1/prescriptions/${id}/cancel`);
}
