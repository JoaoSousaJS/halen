import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../../../shared/components/AuthProvider';
import { http, HttpResponse } from 'msw';
import MedicalRecordsPage from './MedicalRecordsPage';

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(
    JSON.stringify({
      sub: '1',
      email: 'patient@test.com',
      given_name: 'Maya',
      family_name: 'Chen',
      role: 'Patient',
      clinic_id: 'clinic-1',
      exp: 9999999999,
    }),
  );
  return `${header}.${body}.fake`;
}

const mockHeader = {
  patientProfileId: 'pp-1',
  patientName: 'Maya Chen',
  city: 'Porto',
  allergyChips: ['Penicillin', 'Peanuts'],
  conditionChips: ['Hypertension', 'Asthma'],
};

const mockTimeline = {
  entries: [
    {
      id: 'tl-1',
      type: 'Condition',
      occurredAt: '2026-03-10T14:00:00Z',
      title: 'Hypertension diagnosed',
      subtitle: 'ICD-10: I10',
      addedBy: 'Dr. Silva',
    },
    {
      id: 'tl-2',
      type: 'Allergy',
      occurredAt: '2026-02-15T09:00:00Z',
      title: 'Penicillin allergy recorded',
      subtitle: 'Severe reaction',
      addedBy: 'Dr. Costa',
    },
  ],
  totalCount: 2,
};

const mockSnapshot = {
  allergies: [{ id: 'a-1', allergenName: 'Penicillin', reaction: 'Rash', severity: 'Severe' }],
  activeConditions: [{ id: 'c-1', icdDescription: 'Hypertension', severity: 'Moderate' }],
  activeMedications: [{ id: 'm-1', medicationName: 'Lisinopril', dosage: '10mg', frequency: 'Once daily', startDate: '2026-01-01' }],
  familyHistory: [{ id: 'fh-1', relationship: 'Father', conditionName: 'Type 2 Diabetes' }],
  latestVitals: {
    bloodPressure: { value: 130, secondaryValue: 85, unit: 'mmHg', measuredAt: '2026-05-10T08:00:00Z' },
    heartRate: { value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2026-05-10T08:00:00Z' },
    weight: { value: 68.5, secondaryValue: null, unit: 'kg', measuredAt: '2026-05-10T08:00:00Z' },
    spO2: { value: 98, secondaryValue: null, unit: '%', measuredAt: '2026-05-10T08:00:00Z' },
  },
  onboardingProgress: 5,
};

const mockFeatureFlags = [
  { featureKey: 'medical_records', isEnabled: true },
];

const allHandlers = [
  http.get('*/api/v1/me/features', () => HttpResponse.json(mockFeatureFlags)),
  http.get('*/api/v1/medical-records/*/header', () => HttpResponse.json(mockHeader)),
  http.get('*/api/v1/medical-records/*/timeline', () => HttpResponse.json(mockTimeline)),
  http.get('*/api/v1/medical-records/*/snapshot', () => HttpResponse.json(mockSnapshot)),
  http.get('*/api/v1/medical-records/*/conditions', () => HttpResponse.json([])),
  http.get('*/api/v1/medical-records/*/allergies', () => HttpResponse.json([])),
  http.get('*/api/v1/medical-records/*/vitals/*/history', () => HttpResponse.json([])),
  http.get('*/api/v1/medical-records/*/medications', () => HttpResponse.json([])),
  http.get('*/api/v1/medical-records/*/family-history', () => HttpResponse.json([])),
  http.get('*/api/v1/medical-records/*/documents', () => HttpResponse.json([])),
];

const meta: Meta<typeof MedicalRecordsPage> = {
  title: 'Patient/MedicalRecords/MedicalRecordsPage',
  component: MedicalRecordsPage,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => {
      localStorage.setItem('token', fakeJwt());
      return (
        <QueryClientProvider
          client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}
        >
          <MemoryRouter initialEntries={['/patient/records/pp-1']}>
            <AuthProvider>
              <Routes>
                <Route
                  path="/patient/records/:patientProfileId"
                  element={<Story />}
                />
              </Routes>
            </AuthProvider>
          </MemoryRouter>
        </QueryClientProvider>
      );
    },
  ],
};
export default meta;

type Story = StoryObj<typeof MedicalRecordsPage>;

export const Default: Story = {
  parameters: {
    msw: { handlers: allHandlers },
  },
};

export const Loading: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/me/features', () => HttpResponse.json(mockFeatureFlags)),
        http.get('*/api/v1/medical-records/*/header', async () => {
          await new Promise(() => {});
          return HttpResponse.json(mockHeader);
        }),
      ],
    },
  },
};

export const EmptyState: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/me/features', () => HttpResponse.json(mockFeatureFlags)),
        http.get('*/api/v1/medical-records/*/header', () =>
          HttpResponse.json({
            ...mockHeader,
            allergyChips: [],
            conditionChips: [],
          }),
        ),
        http.get('*/api/v1/medical-records/*/timeline', () =>
          HttpResponse.json({ entries: [], totalCount: 0 }),
        ),
        http.get('*/api/v1/medical-records/*/snapshot', () =>
          HttpResponse.json({
            allergies: [],
            activeConditions: [],
            activeMedications: [],
            familyHistory: [],
            latestVitals: null,
            onboardingProgress: 0,
          }),
        ),
        http.get('*/api/v1/medical-records/*/conditions', () => HttpResponse.json([])),
        http.get('*/api/v1/medical-records/*/allergies', () => HttpResponse.json([])),
        http.get('*/api/v1/medical-records/*/vitals/*/history', () => HttpResponse.json([])),
        http.get('*/api/v1/medical-records/*/medications', () => HttpResponse.json([])),
        http.get('*/api/v1/medical-records/*/family-history', () => HttpResponse.json([])),
        http.get('*/api/v1/medical-records/*/documents', () => HttpResponse.json([])),
      ],
    },
  },
};
