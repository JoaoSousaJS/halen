import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import DoctorMyReviews from './DoctorMyReviews';
import type { MyReviewsResponse, DoctorReviewItemDto } from '../../shared/api/reviews';

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

const allReviewItems: DoctorReviewItemDto[] = [
  {
    id: 'rev-dr-1',
    rating: 5,
    title: 'Outstanding care and attention',
    body: 'Dr. House took the time to explain everything clearly. Would absolutely recommend.',
    tags: ['clear explanations', 'thorough', 'listens'],
    postedAs: 'Maya C.',
    helpfulCount: 12,
    moderationStatus: 'Approved',
    doctorResponse: 'Thank you for the kind words, Maya!',
    doctorRespondedAt: '2026-05-11T09:00:00Z',
    createdAt: '2026-05-10T14:30:00Z',
  },
  {
    id: 'rev-dr-2',
    rating: 4,
    title: 'Very professional experience',
    body: 'Quick appointment booking, minimal wait time. The follow-up notes were helpful.',
    tags: ['on time', 'sends follow-up notes'],
    postedAs: 'Carlos R.',
    helpfulCount: 5,
    moderationStatus: 'Approved',
    doctorResponse: null,
    doctorRespondedAt: null,
    createdAt: '2026-05-08T09:15:00Z',
  },
  {
    id: 'rev-dr-3',
    rating: 2,
    title: 'Long wait and rushed consultation',
    body: 'Waited 30 minutes past my appointment time and the consultation felt rushed.',
    tags: ['wait times'],
    postedAs: 'Elena M.',
    helpfulCount: 8,
    moderationStatus: 'Approved',
    doctorResponse: null,
    doctorRespondedAt: null,
    createdAt: '2026-04-28T16:00:00Z',
  },
  {
    id: 'rev-dr-4',
    rating: 1,
    title: 'Very disappointing',
    body: 'Could not hear the doctor properly during the video call and no follow-up was provided.',
    tags: [],
    postedAs: 'James W.',
    helpfulCount: 2,
    moderationStatus: 'Pending',
    doctorResponse: null,
    doctorRespondedAt: null,
    createdAt: '2026-04-20T12:00:00Z',
  },
];

const allReviewsResponse: MyReviewsResponse = {
  reviews: allReviewItems,
  totalCount: 4,
  averageRating: 3.0,
  reviewCount: 4,
};

const awaitingReplyResponse: MyReviewsResponse = {
  reviews: allReviewItems.filter((r) => r.doctorResponse === null),
  totalCount: 3,
  averageRating: 3.0,
  reviewCount: 4,
};

const lowStarResponse: MyReviewsResponse = {
  reviews: allReviewItems.filter((r) => r.rating <= 2),
  totalCount: 2,
  averageRating: 3.0,
  reviewCount: 4,
};

const emptyResponse: MyReviewsResponse = {
  reviews: [],
  totalCount: 0,
  averageRating: null,
  reviewCount: 0,
};

const meta: Meta<typeof DoctorMyReviews> = {
  title: 'Reviews/DoctorMyReviews',
  component: DoctorMyReviews,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={makeQueryClient()}>
        <Story />
      </QueryClientProvider>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof DoctorMyReviews>;

export const AllReviews: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/reviews', () =>
          HttpResponse.json(allReviewsResponse),
        ),
        http.post('*/api/v1/reviews/:reviewId/respond', () =>
          new HttpResponse(null, { status: 204 }),
        ),
      ],
    },
  },
};

export const AwaitingReply: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/reviews', ({ request }) => {
          const url = new URL(request.url);
          const filter = url.searchParams.get('filter');
          if (filter === 'awaiting-reply') {
            return HttpResponse.json(awaitingReplyResponse);
          }
          return HttpResponse.json(allReviewsResponse);
        }),
        http.post('*/api/v1/reviews/:reviewId/respond', () =>
          new HttpResponse(null, { status: 204 }),
        ),
      ],
    },
  },
};

export const LowStar: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/reviews', ({ request }) => {
          const url = new URL(request.url);
          const filter = url.searchParams.get('filter');
          if (filter === 'low-star') {
            return HttpResponse.json(lowStarResponse);
          }
          return HttpResponse.json(allReviewsResponse);
        }),
        http.post('*/api/v1/reviews/:reviewId/respond', () =>
          new HttpResponse(null, { status: 204 }),
        ),
      ],
    },
  },
};

export const EmptyState: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/reviews', () =>
          HttpResponse.json(emptyResponse),
        ),
      ],
    },
  },
};
