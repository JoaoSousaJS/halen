import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import PatientSnapshot from './PatientSnapshot';
import type { PatientSnapshotDto } from '../../../shared/api/medical-records';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof PatientSnapshot> = {
  title: 'Patient/MedicalRecords/PatientSnapshot',
  component: PatientSnapshot,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <div style={{ padding: 24, maxWidth: 900, background: '#0b0e0c' }}>
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

type Story = StoryObj<typeof PatientSnapshot>;

const fullSnapshot: PatientSnapshotDto = {
  allergies: [
    { id: 'a-1', allergenName: 'Penicillin', reaction: 'Anaphylaxis', severity: 'Severe' },
    { id: 'a-2', allergenName: 'Peanuts', reaction: 'Hives', severity: 'Moderate' },
  ],
  activeConditions: [
    { id: 'c-1', icdDescription: 'Essential Hypertension', severity: 'Moderate' },
    { id: 'c-2', icdDescription: 'Asthma', severity: 'Mild' },
  ],
  activeMedications: [
    { id: 'm-1', medicationName: 'Lisinopril', dosage: '10mg', frequency: 'Once daily', startDate: '2026-01-15' },
    { id: 'm-2', medicationName: 'Albuterol', dosage: '90mcg', frequency: 'As needed', startDate: '2025-09-01' },
  ],
  familyHistory: [
    { id: 'fh-1', relationship: 'Father', conditionName: 'Type 2 Diabetes' },
    { id: 'fh-2', relationship: 'Mother', conditionName: 'Breast Cancer' },
  ],
  latestVitals: {
    bloodPressure: { value: 130, secondaryValue: 85, unit: 'mmHg', measuredAt: '2026-05-10T08:00:00Z' },
    heartRate: { value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2026-05-10T08:00:00Z' },
    weight: { value: 68.5, secondaryValue: null, unit: 'kg', measuredAt: '2026-05-10T08:00:00Z' },
    spO2: { value: 98, secondaryValue: null, unit: '%', measuredAt: '2026-05-10T08:00:00Z' },
  },
  onboardingProgress: 6,
};

const partialSnapshot: PatientSnapshotDto = {
  allergies: [
    { id: 'a-1', allergenName: 'Penicillin', reaction: 'Rash', severity: 'Moderate' },
  ],
  activeConditions: [],
  activeMedications: [],
  familyHistory: [],
  latestVitals: null,
  onboardingProgress: 2,
};

const emptySnapshot: PatientSnapshotDto = {
  allergies: [],
  activeConditions: [],
  activeMedications: [],
  familyHistory: [],
  latestVitals: null,
  onboardingProgress: 0,
};

export const FullSnapshot: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/snapshot', () =>
          HttpResponse.json(fullSnapshot),
        ),
      ],
    },
  },
};

export const PartialOnboarding: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/snapshot', () =>
          HttpResponse.json(partialSnapshot),
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
      ],
    },
  },
};
