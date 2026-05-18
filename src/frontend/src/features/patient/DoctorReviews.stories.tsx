import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import DoctorReviews from './DoctorReviews';
import type { DoctorReviewsResponse } from '../../shared/api/reviews';

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

const sampleReviews: DoctorReviewsResponse = {
  reviews: [
    {
      id: 'rev-1',
      rating: 5,
      title: 'Outstanding care and attention',
      body: 'Dr. House took the time to explain everything clearly. Would absolutely recommend.',
      tags: ['clear explanations', 'thorough', 'listens'],
      postedAs: 'Maya C.',
      helpfulCount: 12,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-10T14:30:00Z',
    },
    {
      id: 'rev-2',
      rating: 4,
      title: 'Very professional experience',
      body: 'Quick appointment booking, minimal wait time. The follow-up notes were helpful.',
      tags: ['on time', 'sends follow-up notes', 'booking flexibility'],
      postedAs: 'Carlos R.',
      helpfulCount: 5,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-08T09:15:00Z',
    },
    {
      id: 'rev-3',
      rating: 3,
      title: 'Good but could improve wait times',
      body: 'The consultation itself was fine, but I waited 25 minutes past my scheduled slot.',
      tags: ['wait times'],
      postedAs: 'Elena M.',
      helpfulCount: 3,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-04-28T16:00:00Z',
    },
  ],
  totalCount: 3,
  averageRating: 4.0,
  reviewCount: 3,
  ratingBreakdown: [
    { stars: 5, count: 1 },
    { stars: 4, count: 1 },
    { stars: 3, count: 1 },
    { stars: 2, count: 0 },
    { stars: 1, count: 0 },
  ],
  topTags: [
    { tag: 'clear explanations', count: 1 },
    { tag: 'thorough', count: 1 },
    { tag: 'on time', count: 1 },
  ],
};

const emptyReviews: DoctorReviewsResponse = {
  reviews: [],
  totalCount: 0,
  averageRating: null,
  reviewCount: 0,
  ratingBreakdown: [
    { stars: 5, count: 0 },
    { stars: 4, count: 0 },
    { stars: 3, count: 0 },
    { stars: 2, count: 0 },
    { stars: 1, count: 0 },
  ],
  topTags: [],
};

const reviewsWithResponses: DoctorReviewsResponse = {
  ...sampleReviews,
  reviews: [
    {
      ...sampleReviews.reviews[0],
      doctorResponse: 'Thank you for your kind words, Maya. It was a pleasure helping you with your care plan.',
      doctorRespondedAt: '2026-05-11T09:00:00Z',
    },
    {
      ...sampleReviews.reviews[1],
      doctorResponse: 'I appreciate the feedback, Carlos. Glad the follow-up notes were useful.',
      doctorRespondedAt: '2026-05-09T10:30:00Z',
    },
    sampleReviews.reviews[2],
  ],
};

const highRatedReviews: DoctorReviewsResponse = {
  reviews: [
    {
      id: 'rev-hr-1',
      rating: 5,
      title: 'Best doctor I have ever visited',
      body: 'Incredibly thorough and compassionate. Took time to answer all my questions.',
      tags: ['clear explanations', 'thorough', 'calm bedside manner', 'listens'],
      postedAs: 'Priya K.',
      helpfulCount: 24,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-14T11:00:00Z',
    },
    {
      id: 'rev-hr-2',
      rating: 5,
      title: 'Exceptional telehealth experience',
      body: 'The video call quality was great. The prescription was sent to my pharmacy within minutes.',
      tags: ['on time', 'sends follow-up notes'],
      postedAs: 'James W.',
      helpfulCount: 18,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-12T15:45:00Z',
    },
  ],
  totalCount: 2,
  averageRating: 5.0,
  reviewCount: 2,
  ratingBreakdown: [
    { stars: 5, count: 2 },
    { stars: 4, count: 0 },
    { stars: 3, count: 0 },
    { stars: 2, count: 0 },
    { stars: 1, count: 0 },
  ],
  topTags: [
    { tag: 'clear explanations', count: 1 },
    { tag: 'thorough', count: 1 },
    { tag: 'on time', count: 1 },
    { tag: 'sends follow-up notes', count: 1 },
  ],
};

const meta: Meta<typeof DoctorReviews> = {
  title: 'Reviews/DoctorReviews',
  component: DoctorReviews,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={makeQueryClient()}>
        <Story />
      </QueryClientProvider>
    ),
  ],
  args: {
    doctorProfileId: 'doctor-1',
  },
};
export default meta;

type Story = StoryObj<typeof DoctorReviews>;

export const WithReviews: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/reviews/doctor/:doctorId', () =>
          HttpResponse.json(sampleReviews),
        ),
        http.post('*/api/v1/reviews/:reviewId/helpful', () =>
          HttpResponse.json({ newCount: 1 }),
        ),
      ],
    },
  },
};

export const EmptyState: Story = {
  args: {
    doctorProfileId: 'doctor-new',
  },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/reviews/doctor/:doctorId', () =>
          HttpResponse.json(emptyReviews),
        ),
      ],
    },
  },
};

export const WithDoctorResponses: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/reviews/doctor/:doctorId', () =>
          HttpResponse.json(reviewsWithResponses),
        ),
        http.post('*/api/v1/reviews/:reviewId/helpful', () =>
          HttpResponse.json({ newCount: 1 }),
        ),
      ],
    },
  },
};

export const HighRated: Story = {
  args: {
    doctorProfileId: 'doctor-top',
  },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/reviews/doctor/:doctorId', () =>
          HttpResponse.json(highRatedReviews),
        ),
        http.post('*/api/v1/reviews/:reviewId/helpful', () =>
          HttpResponse.json({ newCount: 1 }),
        ),
      ],
    },
  },
};
