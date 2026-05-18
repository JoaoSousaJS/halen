import client from './client';

export interface SearchDoctorsParams {
  search?: string;
  specialty?: string;
  minFee?: number;
  maxFee?: number;
  availableOn?: string;
  sortBy?: 'name' | 'fee_asc' | 'fee_desc' | 'experience' | 'rating';
  page?: number;
  pageSize?: number;
}

export interface DoctorSearchDto {
  id: string;
  name: string;
  specialty: string;
  consultationFee: number;
  yearsOfExperience: number;
  languages: string[];
  nextAvailableSlot: { startUtc: string; dayOfWeek: string } | null;
  averageRating: number | null;
  reviewCount: number;
}

export interface SearchDoctorsResponse {
  doctors: DoctorSearchDto[];
  totalCount: number;
}

export async function searchDoctors(params: SearchDoctorsParams): Promise<SearchDoctorsResponse> {
  const { data } = await client.get<SearchDoctorsResponse>('/api/v1/doctors/search', { params });
  return data;
}

export async function listSpecialties(): Promise<string[]> {
  const { data } = await client.get<{ specialties: string[] }>('/api/v1/doctors/specialties');
  return data.specialties;
}
