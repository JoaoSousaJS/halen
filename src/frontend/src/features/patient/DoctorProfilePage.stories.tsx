import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { http, HttpResponse } from 'msw';
import DoctorProfilePage from './DoctorProfilePage';
import type { DoctorProfileResponse } from '../../shared/api/doctors';

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

const fullProfile: DoctorProfileResponse = {
  doctor: {
    id: 'doc-1',
    name: 'Dr. Ana Costa',
    specialty: 'Cardiology',
    consultationFee: 150,
    yearsOfExperience: 12,
    languages: ['English', 'Portuguese', 'Spanish'],
    averageRating: 4.7,
    reviewCount: 48,
  },
  availability: [
    {
      dayOfWeek: 'Monday',
      windows: [
        { startTime: '09:00', endTime: '12:00', slotDurationMinutes: 20 },
        { startTime: '14:00', endTime: '17:00', slotDurationMinutes: 20 },
      ],
    },
    {
      dayOfWeek: 'Wednesday',
      windows: [
        { startTime: '09:00', endTime: '13:00', slotDurationMinutes: 30 },
      ],
    },
    {
      dayOfWeek: 'Friday',
      windows: [
        { startTime: '10:00', endTime: '14:00', slotDurationMinutes: 20 },
      ],
    },
  ],
  reviewsSummary: {
    averageRating: 4.7,
    totalCount: 48,
    ratingBreakdown: [
      { stars: 5, count: 30 },
      { stars: 4, count: 12 },
      { stars: 3, count: 4 },
      { stars: 2, count: 1 },
      { stars: 1, count: 1 },
    ],
    topTags: [
      { tag: 'thorough', count: 22 },
      { tag: 'clear explanations', count: 18 },
      { tag: 'listens', count: 15 },
      { tag: 'calm bedside manner', count: 10 },
      { tag: 'on time', count: 8 },
    ],
  },
  reviews: [
    {
      id: 'r1',
      rating: 5,
      title: 'Outstanding care and attention',
      body: 'Dr. Costa took the time to explain everything clearly. Would absolutely recommend.',
      tags: ['clear explanations', 'thorough', 'listens'],
      postedAs: 'Maya C.',
      helpfulCount: 12,
      doctorResponse: 'Thank you for your kind words, Maya. It was a pleasure helping you.',
      doctorRespondedAt: '2026-05-11T09:00:00Z',
      createdAt: '2026-05-10T14:30:00Z',
    },
    {
      id: 'r2',
      rating: 4,
      title: 'Very professional experience',
      body: 'Quick appointment booking, minimal wait time. The follow-up notes were helpful.',
      tags: ['on time', 'sends follow-up notes'],
      postedAs: 'Carlos R.',
      helpfulCount: 5,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-08T09:15:00Z',
    },
    {
      id: 'r3',
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
  reviewTotalCount: 48,
};

const noReviewsProfile: DoctorProfileResponse = {
  ...fullProfile,
  doctor: { ...fullProfile.doctor, averageRating: null, reviewCount: 0 },
  reviewsSummary: null,
  reviews: [],
  reviewTotalCount: 0,
};

const noAvailabilityProfile: DoctorProfileResponse = {
  ...fullProfile,
  availability: [],
};

const meta: Meta<typeof DoctorProfilePage> = {
  title: 'Patient/DoctorProfilePage',
  component: DoctorProfilePage,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={makeQueryClient()}>
        <MemoryRouter initialEntries={['/doctors/doc-1/profile']}>
          <Routes>
            <Route path="/doctors/:id/profile" element={<Story />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof DoctorProfilePage>;

export const FullProfile: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/:id/profile', () =>
          HttpResponse.json(fullProfile),
        ),
      ],
    },
  },
};

export const NoReviews: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/:id/profile', () =>
          HttpResponse.json(noReviewsProfile),
        ),
      ],
    },
  },
};

export const NoAvailability: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/:id/profile', () =>
          HttpResponse.json(noAvailabilityProfile),
        ),
      ],
    },
  },
};

export const Loading: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/:id/profile', () =>
          new Promise(() => {}),
        ),
      ],
    },
  },
};

export const Error: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/:id/profile', () =>
          HttpResponse.json({ message: 'Not found' }, { status: 404 }),
        ),
      ],
    },
  },
};
