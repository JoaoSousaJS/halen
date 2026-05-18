import client from './client';

export type ReviewModerationStatus = 'Pending' | 'Approved' | 'Hidden' | 'Removed';

export interface ReviewDto {
  id: string;
  rating: number;
  title: string;
  body: string;
  tags: string[];
  postedAs: string;
  helpfulCount: number;
  doctorResponse: string | null;
  doctorRespondedAt: string | null;
  createdAt: string;
}

export interface RatingBreakdownDto {
  stars: number;
  count: number;
}

export interface TagCountDto {
  tag: string;
  count: number;
}

export interface DoctorReviewsResponse {
  reviews: ReviewDto[];
  totalCount: number;
  averageRating: number | null;
  reviewCount: number;
  ratingBreakdown: RatingBreakdownDto[];
  topTags: TagCountDto[];
}

export interface DoctorReviewItemDto {
  id: string;
  rating: number;
  title: string;
  body: string;
  tags: string[];
  postedAs: string;
  helpfulCount: number;
  moderationStatus: ReviewModerationStatus;
  doctorResponse: string | null;
  doctorRespondedAt: string | null;
  createdAt: string;
}

export interface MyReviewsResponse {
  reviews: DoctorReviewItemDto[];
  totalCount: number;
  averageRating: number | null;
  reviewCount: number;
}

export interface ModerationReviewDto {
  id: string;
  rating: number;
  title: string;
  body: string;
  tags: string[];
  postedAs: string;
  moderationStatus: ReviewModerationStatus;
  patientName: string;
  doctorName: string;
  createdAt: string;
}

export interface ModerationQueueResponse {
  reviews: ModerationReviewDto[];
  totalCount: number;
}

export interface SubmitReviewPayload {
  appointmentId: string;
  rating: number;
  title: string;
  body: string;
  tags: string[];
}

export async function submitReview(payload: SubmitReviewPayload): Promise<{ reviewId: string }> {
  const { data } = await client.post<{ reviewId: string }>('/api/v1/reviews', payload);
  return data;
}

export async function getDoctorReviews(
  doctorProfileId: string,
  params: { page?: number; pageSize?: number; sortBy?: string } = {},
): Promise<DoctorReviewsResponse> {
  const { data } = await client.get<DoctorReviewsResponse>(
    `/api/v1/reviews/doctor/${doctorProfileId}`,
    { params },
  );
  return data;
}

export async function respondToReview(reviewId: string, response: string): Promise<void> {
  await client.post(`/api/v1/reviews/${reviewId}/respond`, { response });
}

export async function voteHelpful(reviewId: string): Promise<{ newCount: number }> {
  const { data } = await client.post<{ newCount: number }>(`/api/v1/reviews/${reviewId}/helpful`);
  return data;
}

export async function getMyReviews(
  params: { page?: number; pageSize?: number; filter?: string } = {},
): Promise<MyReviewsResponse> {
  const { data } = await client.get<MyReviewsResponse>('/api/v1/doctor/reviews', { params });
  return data;
}

export async function getModerationQueue(
  params: { page?: number; pageSize?: number; filter?: string } = {},
): Promise<ModerationQueueResponse> {
  const { data } = await client.get<ModerationQueueResponse>(
    '/api/v1/admin/reviews/moderation',
    { params },
  );
  return data;
}

export async function moderateReview(reviewId: string, decision: string): Promise<void> {
  await client.post(`/api/v1/admin/reviews/${reviewId}/moderate`, { decision });
}
