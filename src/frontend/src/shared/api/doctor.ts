import client from './client';

export type KycStatus = 'NotSubmitted' | 'Submitted' | 'Approved' | 'Rejected';

export interface KycDocumentDto {
  id: string;
  documentType: string;
  fileName: string;
  uploadedAt: string;
}

export interface KycStatusResponse {
  status: KycStatus;
  submittedAt: string | null;
  lastRejectionReason: string | null;
  documents: KycDocumentDto[];
}

export interface SubmitKycFiles {
  licensePhoto: File;
  medicalCertificate: File;
  identityProof: File;
}

export interface DoctorPatientDto {
  patientId: string;
  name: string;
}

export async function getMyPatients(): Promise<DoctorPatientDto[]> {
  const { data } = await client.get<{ patients: DoctorPatientDto[] }>('/api/v1/doctor/patients');
  return data.patients;
}

export async function getKycStatus(): Promise<KycStatusResponse> {
  const { data } = await client.get<KycStatusResponse>('/api/v1/doctor/kyc/status');
  return data;
}

export async function submitKycDocuments(files: SubmitKycFiles): Promise<{ message: string }> {
  const formData = new FormData();
  formData.append('licensePhoto', files.licensePhoto);
  formData.append('medicalCertificate', files.medicalCertificate);
  formData.append('identityProof', files.identityProof);

  const { data } = await client.post<{ message: string }>('/api/v1/doctor/kyc/documents', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}
