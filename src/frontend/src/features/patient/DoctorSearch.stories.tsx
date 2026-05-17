import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import DoctorSearch from './DoctorSearch';

const meta: Meta<typeof DoctorSearch> = {
  title: 'Patient/DoctorSearch',
  component: DoctorSearch,
  decorators: [
    (Story) => (
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <div style={{ padding: 24, maxWidth: 640, background: '#0b0e0c' }}>
          <Story />
        </div>
      </QueryClientProvider>
    ),
  ],
  parameters: { layout: 'centered' },
};
export default meta;

type Story = StoryObj<typeof DoctorSearch>;

const mockDoctors = [
  {
    id: 'doc-1',
    name: 'Dr. Silva',
    specialty: 'Cardiology',
    consultationFee: 150,
    yearsOfExperience: 10,
    languages: ['English', 'Portuguese'],
    nextAvailableSlot: { startUtc: '2026-05-19T09:00:00Z', dayOfWeek: 'Monday' },
  },
  {
    id: 'doc-2',
    name: 'Dr. Costa',
    specialty: 'Dermatology',
    consultationFee: 120,
    yearsOfExperience: 7,
    languages: ['English'],
    nextAvailableSlot: null,
  },
  {
    id: 'doc-3',
    name: 'Dr. Mendes',
    specialty: 'Psychiatry',
    consultationFee: 200,
    yearsOfExperience: 15,
    languages: ['English', 'Portuguese', 'Spanish'],
    nextAvailableSlot: { startUtc: '2026-05-20T14:00:00Z', dayOfWeek: 'Tuesday' },
  },
];

const specialties = ['Cardiology', 'Dermatology', 'Neurology', 'Psychiatry', 'Surgery'];

export const WithResults: Story = {
  args: { onSelect: () => {} },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/search', () =>
          HttpResponse.json({ doctors: mockDoctors, totalCount: mockDoctors.length }),
        ),
        http.get('*/api/v1/doctors/specialties', () =>
          HttpResponse.json({ specialties }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  args: { onSelect: () => {} },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/search', () =>
          HttpResponse.json({ doctors: [], totalCount: 0 }),
        ),
        http.get('*/api/v1/doctors/specialties', () =>
          HttpResponse.json({ specialties }),
        ),
      ],
    },
  },
};

export const Paginated: Story = {
  args: { onSelect: () => {} },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/search', () =>
          HttpResponse.json({ doctors: mockDoctors, totalCount: 25 }),
        ),
        http.get('*/api/v1/doctors/specialties', () =>
          HttpResponse.json({ specialties }),
        ),
      ],
    },
  },
};

export const Loading: Story = {
  args: { onSelect: () => {} },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctors/search', () => new Promise(() => {})),
        http.get('*/api/v1/doctors/specialties', () =>
          HttpResponse.json({ specialties }),
        ),
      ],
    },
  },
};
