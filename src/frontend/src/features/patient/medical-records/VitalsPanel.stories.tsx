import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import VitalsPanel from './VitalsPanel';
import type {
  PatientSnapshotDto,
  VitalReadingDetailDto,
} from '../../../shared/api/medical-records';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof VitalsPanel> = {
  title: 'Patient/MedicalRecords/VitalsPanel',
  component: VitalsPanel,
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

type Story = StoryObj<typeof VitalsPanel>;

const snapshotWithVitals: PatientSnapshotDto = {
  allergies: [],
  activeConditions: [],
  activeMedications: [],
  familyHistory: [],
  latestVitals: {
    bloodPressure: { value: 130, secondaryValue: 85, unit: 'mmHg', measuredAt: '2026-05-10T08:00:00Z' },
    heartRate: { value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2026-05-10T08:00:00Z' },
    weight: { value: 68.5, secondaryValue: null, unit: 'kg', measuredAt: '2026-05-10T08:00:00Z' },
    spO2: { value: 98, secondaryValue: null, unit: '%', measuredAt: '2026-05-10T08:00:00Z' },
  },
  onboardingProgress: 4,
};

const emptySnapshot: PatientSnapshotDto = {
  allergies: [],
  activeConditions: [],
  activeMedications: [],
  familyHistory: [],
  latestVitals: null,
  onboardingProgress: 0,
};

const bpHistory: VitalReadingDetailDto[] = [
  { id: 'v-1', value: 125, secondaryValue: 80, unit: 'mmHg', measuredAt: '2026-05-01T08:00:00Z', source: 'Manual', notes: null, addedBy: 'Maya Chen' },
  { id: 'v-2', value: 128, secondaryValue: 82, unit: 'mmHg', measuredAt: '2026-05-03T08:00:00Z', source: 'Device', notes: 'Morning reading', addedBy: 'Maya Chen' },
  { id: 'v-3', value: 132, secondaryValue: 86, unit: 'mmHg', measuredAt: '2026-05-05T09:00:00Z', source: 'Manual', notes: null, addedBy: 'Maya Chen' },
  { id: 'v-4', value: 127, secondaryValue: 81, unit: 'mmHg', measuredAt: '2026-05-07T08:30:00Z', source: 'ClinicalEntry', notes: 'Office visit', addedBy: 'Dr. Silva' },
  { id: 'v-5', value: 130, secondaryValue: 85, unit: 'mmHg', measuredAt: '2026-05-10T08:00:00Z', source: 'Manual', notes: null, addedBy: 'Maya Chen' },
];

export const WithVitals: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/snapshot', () =>
          HttpResponse.json(snapshotWithVitals),
        ),
        http.get('*/api/v1/medical-records/*/vitals/*/history', () =>
          HttpResponse.json([]),
        ),
        http.post('*/api/v1/medical-records/*/vitals', () =>
          HttpResponse.json({ vitalId: 'v-new' }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/snapshot', () =>
          HttpResponse.json(emptySnapshot),
        ),
        http.get('*/api/v1/medical-records/*/vitals/*/history', () =>
          HttpResponse.json([]),
        ),
        http.post('*/api/v1/medical-records/*/vitals', () =>
          HttpResponse.json({ vitalId: 'v-new' }),
        ),
      ],
    },
  },
};

export const WithChart: Story = {
  name: 'With History Chart',
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/snapshot', () =>
          HttpResponse.json(snapshotWithVitals),
        ),
        http.get('*/api/v1/medical-records/*/vitals/*/history', () =>
          HttpResponse.json(bpHistory),
        ),
        http.post('*/api/v1/medical-records/*/vitals', () =>
          HttpResponse.json({ vitalId: 'v-new' }),
        ),
      ],
    },
  },
};
