import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import ReviewModeration from './ReviewModeration';
import type { ModerationQueueResponse, ModerationReviewDto } from '../../shared/api/reviews';

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

const pendingReviews: ModerationReviewDto[] = [
  {
    id: 'mod-1',
    rating: 1,
    title: 'Terrible experience, felt ignored',
    body: 'Doctor barely listened to my symptoms and ended the call in under 5 minutes.',
    tags: [],
    postedAs: 'Elena M.',
    moderationStatus: 'Pending',
    patientName: 'Elena Martinez',
    doctorName: 'Dr. Gregory House',
    createdAt: '2026-05-16T10:00:00Z',
  },
  {
    id: 'mod-2',
    rating: 4,
    title: 'Great consultation overall',
    body: 'Thorough examination and clear next steps. Only minor issue was the video quality.',
    tags: ['clear explanations', 'thorough'],
    postedAs: 'Carlos R.',
    moderationStatus: 'Pending',
    patientName: 'Carlos Rodriguez',
    doctorName: 'Dr. Lisa Cuddy',
    createdAt: '2026-05-15T14:30:00Z',
  },
  {
    id: 'mod-3',
    rating: 5,
    title: 'Best telehealth visit I have had',
    body: 'Felt like an in-person visit. Doctor was empathetic and knowledgeable.',
    tags: ['calm bedside manner', 'listens', 'sends follow-up notes'],
    postedAs: 'Priya K.',
    moderationStatus: 'Pending',
    patientName: 'Priya Kumar',
    doctorName: 'Dr. James Wilson',
    createdAt: '2026-05-14T09:45:00Z',
  },
];

const allQueueReviews: ModerationReviewDto[] = [
  ...pendingReviews,
  {
    id: 'mod-4',
    rating: 3,
    title: 'Decent but nothing special',
    body: 'The appointment went fine. Doctor was professional but did not go above and beyond.',
    tags: ['on time'],
    postedAs: 'Maya C.',
    moderationStatus: 'Approved',
    patientName: 'Maya Chen',
    doctorName: 'Dr. Gregory House',
    createdAt: '2026-05-12T11:00:00Z',
  },
  {
    id: 'mod-5',
    rating: 2,
    title: 'Not recommended',
    body: 'Prescription seemed wrong and I had to visit another doctor for a second opinion.',
    tags: [],
    postedAs: 'James W.',
    moderationStatus: 'Removed',
    patientName: 'James Ward',
    doctorName: 'Dr. Lisa Cuddy',
    createdAt: '2026-05-10T08:20:00Z',
  },
];

const pendingQueueResponse: ModerationQueueResponse = {
  reviews: pendingReviews,
  totalCount: 3,
};

const allQueueResponse: ModerationQueueResponse = {
  reviews: allQueueReviews,
  totalCount: 5,
};

const emptyQueueResponse: ModerationQueueResponse = {
  reviews: [],
  totalCount: 0,
};

const meta: Meta<typeof ReviewModeration> = {
  title: 'Reviews/ReviewModeration',
  component: ReviewModeration,
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

type Story = StoryObj<typeof ReviewModeration>;

export const PendingQueue: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/reviews/moderation', ({ request }) => {
          const url = new URL(request.url);
          const filter = url.searchParams.get('filter');
          if (filter === 'all') {
            return HttpResponse.json(allQueueResponse);
          }
          return HttpResponse.json(pendingQueueResponse);
        }),
        http.post('*/api/v1/admin/reviews/:reviewId/moderate', () =>
          new HttpResponse(null, { status: 204 }),
        ),
      ],
    },
  },
};

export const AllQueue: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/reviews/moderation', () =>
          HttpResponse.json(allQueueResponse),
        ),
        http.post('*/api/v1/admin/reviews/:reviewId/moderate', () =>
          new HttpResponse(null, { status: 204 }),
        ),
      ],
    },
  },
};

export const EmptyQueue: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/reviews/moderation', () =>
          HttpResponse.json(emptyQueueResponse),
        ),
      ],
    },
  },
};
