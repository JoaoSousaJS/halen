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

export interface DoctorProfileDto {
  id: string;
  name: string;
  specialty: string;
  consultationFee: number;
  yearsOfExperience: number;
  languages: string[];
  averageRating: number | null;
  reviewCount: number;
}

export interface TimeWindowDto {
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

export interface AvailabilityDayDto {
  dayOfWeek: string;
  windows: TimeWindowDto[];
}

export interface RatingBreakdownDto {
  stars: number;
  count: number;
}

export interface TagCountDto {
  tag: string;
  count: number;
}

export interface ReviewsSummaryDto {
  averageRating: number | null;
  reviewCount: number;
  ratingBreakdown: RatingBreakdownDto[];
  topTags: TagCountDto[];
}

export interface ProfileReviewDto {
  id: string;
  rating: number;
  title: string;
  body: string | null;
  tags: string[];
  postedAs: string;
  helpfulCount: number;
  doctorResponse: string | null;
  doctorRespondedAt: string | null;
  createdAt: string;
}

export interface DoctorProfileResponse {
  doctor: DoctorProfileDto;
  availability: AvailabilityDayDto[];
  reviewsSummary: ReviewsSummaryDto | null;
  reviews: ProfileReviewDto[];
  reviewTotalCount: number;
}

export interface GetDoctorProfileParams {
  reviewPage?: number;
  reviewPageSize?: number;
  reviewSortBy?: string;
}

export async function getDoctorProfile(
  doctorId: string,
  params: GetDoctorProfileParams = {},
): Promise<DoctorProfileResponse> {
  const { data } = await client.get<DoctorProfileResponse>(
    `/api/v1/doctors/${doctorId}/profile`,
    { params },
  );
  return data;
}
