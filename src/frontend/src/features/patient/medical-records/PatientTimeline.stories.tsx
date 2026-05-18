import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import PatientTimeline from './PatientTimeline';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof PatientTimeline> = {
  title: 'Patient/MedicalRecords/PatientTimeline',
  component: PatientTimeline,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <div style={{ padding: 24, maxWidth: 800, background: '#0b0e0c' }}>
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

type Story = StoryObj<typeof PatientTimeline>;

const mixedEntries = [
  {
    id: 'tl-1',
    type: 'Condition',
    occurredAt: '2026-05-10T14:00:00Z',
    title: 'Hypertension diagnosed',
    subtitle: 'ICD-10: I10 — Essential hypertension',
    addedBy: 'Dr. Silva',
  },
  {
    id: 'tl-2',
    type: 'Allergy',
    occurredAt: '2026-04-22T09:00:00Z',
    title: 'Penicillin allergy recorded',
    subtitle: 'Severe — Anaphylactic reaction',
    addedBy: 'Dr. Costa',
  },
  {
    id: 'tl-3',
    type: 'Vital',
    occurredAt: '2026-04-15T08:30:00Z',
    title: 'Blood Pressure: 130/85 mmHg',
    subtitle: 'Manual entry',
    addedBy: null,
  },
  {
    id: 'tl-4',
    type: 'Medication',
    occurredAt: '2026-04-10T11:00:00Z',
    title: 'Lisinopril 10mg started',
    subtitle: 'Once daily — prescribed by Dr. Silva',
    addedBy: 'Dr. Silva',
  },
  {
    id: 'tl-5',
    type: 'FamilyHistory',
    occurredAt: '2026-03-20T10:00:00Z',
    title: 'Father — Type 2 Diabetes',
    subtitle: 'Onset at age 52',
    addedBy: null,
  },
  {
    id: 'tl-6',
    type: 'Document',
    occurredAt: '2026-03-01T16:00:00Z',
    title: 'Blood work results uploaded',
    subtitle: 'Lab Result — CBC panel',
    addedBy: 'Maya Chen',
  },
  {
    id: 'tl-7',
    type: 'Vital',
    occurredAt: '2026-02-28T07:45:00Z',
    title: 'Heart Rate: 72 bpm',
    subtitle: 'Device',
    addedBy: null,
  },
  {
    id: 'tl-8',
    type: 'Condition',
    occurredAt: '2026-02-15T13:00:00Z',
    title: 'Asthma — status changed to In Remission',
    subtitle: 'ICD-10: J45',
    addedBy: 'Dr. Andrade',
  },
];

export const WithEntries: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/timeline', () =>
          HttpResponse.json({
            entries: mixedEntries,
            totalCount: mixedEntries.length,
          }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/timeline', () =>
          HttpResponse.json({ entries: [], totalCount: 0 }),
        ),
      ],
    },
  },
};

export const Filtered: Story = {
  name: 'Filtered (interact to filter)',
  parameters: {
    docs: {
      description: {
        story:
          'Timeline with mixed entries. Use the checkboxes to filter by type. The filter state is internal — uncheck types to see the filtered view.',
      },
    },
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/timeline', ({ request }) => {
          const url = new URL(request.url);
          const types = url.searchParams.getAll('filterTypes');
          const filtered =
            types.length > 0
              ? mixedEntries.filter((e) => types.includes(e.type))
              : mixedEntries;
          return HttpResponse.json({
            entries: filtered,
            totalCount: filtered.length,
          });
        }),
      ],
    },
  },
};
