import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import FamilyHistoryPanel from './FamilyHistoryPanel';
import type { FamilyHistoryDto } from '../../../shared/api/medical-records';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof FamilyHistoryPanel> = {
  title: 'Patient/MedicalRecords/FamilyHistoryPanel',
  component: FamilyHistoryPanel,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <div style={{ padding: 24, maxWidth: 700, background: '#0b0e0c' }}>
          <Story />
        </div>
      </QueryClientProvider>
    ),
  ],
  args: {
    patientProfileId: 'pp-1',
  },
};
export default meta;

type Story = StoryObj<typeof FamilyHistoryPanel>;

const mockFamilyHistory: FamilyHistoryDto[] = [
  {
    id: 'fh-1',
    relationship: 'Father',
    conditionName: 'Type 2 Diabetes',
    ageAtOnset: 52,
    notes: 'Managed with insulin since age 60.',
    addedBy: 'Maya Chen',
    createdAt: '2026-03-20T10:00:00Z',
  },
  {
    id: 'fh-2',
    relationship: 'Mother',
    conditionName: 'Breast Cancer',
    ageAtOnset: 48,
    notes: 'BRCA2 positive. In remission after treatment.',
    addedBy: 'Maya Chen',
    createdAt: '2026-03-20T10:05:00Z',
  },
  {
    id: 'fh-3',
    relationship: 'Grandfather',
    conditionName: 'Coronary Artery Disease',
    ageAtOnset: 65,
    notes: null,
    addedBy: 'Dr. Silva',
    createdAt: '2026-04-01T14:00:00Z',
  },
  {
    id: 'fh-4',
    relationship: 'Sister',
    conditionName: 'Asthma',
    ageAtOnset: null,
    notes: 'Childhood-onset, still active.',
    addedBy: 'Maya Chen',
    createdAt: '2026-04-10T09:00:00Z',
  },
];

export const WithEntries: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/family-history', () =>
          HttpResponse.json(mockFamilyHistory),
        ),
        http.post('*/api/v1/medical-records/*/family-history', () =>
          HttpResponse.json({ familyHistoryId: 'fh-new' }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/family-history', () =>
          HttpResponse.json([]),
        ),
        http.post('*/api/v1/medical-records/*/family-history', () =>
          HttpResponse.json({ familyHistoryId: 'fh-new' }),
        ),
      ],
    },
  },
};
