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
  doctorProfileId: string | null;
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

// KYC review types and functions

export interface KycReviewDto {
  id: string;
  decision: string;
  rejectionReason: string | null;
  reviewerName: string;
  reviewedAt: string;
}

export interface KycDocumentDto {
  id: string;
  documentType: string;
  fileName: string;
  uploadedAt: string;
}

export interface DoctorKycDetailsResponse {
  doctorProfileId: string;
  doctorName: string;
  specialty: string;
  licenseNumber: string;
  status: string;
  submittedAt: string | null;
  documents: KycDocumentDto[];
  reviewHistory: KycReviewDto[];
}

export interface ReviewKycPayload {
  decision: 'Approved' | 'Rejected';
  rejectionReason?: string;
}

export async function getDoctorKycDetails(doctorProfileId: string): Promise<DoctorKycDetailsResponse> {
  const { data } = await client.get<DoctorKycDetailsResponse>(`/api/v1/admin/doctors/${doctorProfileId}/kyc`);
  return data;
}

export async function reviewKyc(doctorProfileId: string, payload: ReviewKycPayload): Promise<{ message: string }> {
  const { data } = await client.post<{ message: string }>(`/api/v1/admin/doctors/${doctorProfileId}/kyc/review`, payload);
  return data;
}

export function getKycDocumentUrl(documentId: string): string {
  return `/api/v1/admin/kyc/documents/${documentId}`;
}

export async function downloadKycDocument(documentId: string): Promise<Blob> {
  const { data } = await client.get<Blob>(`/api/v1/admin/kyc/documents/${documentId}`, {
    responseType: 'blob',
  });
  return data;
}
