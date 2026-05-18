import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import MedicationsPanel from './MedicationsPanel';
import type { MedicationDto } from '../../../shared/api/medical-records';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof MedicationsPanel> = {
  title: 'Patient/MedicalRecords/MedicationsPanel',
  component: MedicationsPanel,
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

type Story = StoryObj<typeof MedicationsPanel>;

const mockMedications: MedicationDto[] = [
  {
    id: 'med-1',
    medicationName: 'Lisinopril',
    dosage: '10mg',
    frequency: 'Once daily',
    startDate: '2026-01-15',
    endDate: null,
    isActive: true,
    prescribedByName: 'Dr. Silva',
    linkedPrescriptionId: 'rx-101',
    addedBy: 'Dr. Silva',
    createdAt: '2026-01-15T14:00:00Z',
  },
  {
    id: 'med-2',
    medicationName: 'Metformin',
    dosage: '500mg',
    frequency: 'Twice daily',
    startDate: '2025-11-01',
    endDate: null,
    isActive: true,
    prescribedByName: 'Dr. Costa',
    linkedPrescriptionId: null,
    addedBy: 'Dr. Costa',
    createdAt: '2025-11-01T10:00:00Z',
  },
  {
    id: 'med-3',
    medicationName: 'Amoxicillin',
    dosage: '500mg',
    frequency: 'Three times daily',
    startDate: '2026-04-01',
    endDate: '2026-04-10',
    isActive: false,
    prescribedByName: 'Dr. Andrade',
    linkedPrescriptionId: null,
    addedBy: 'Dr. Andrade',
    createdAt: '2026-04-01T09:00:00Z',
  },
];

export const WithMedications: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/medications', () =>
          HttpResponse.json(mockMedications),
        ),
        http.post('*/api/v1/medical-records/*/medications', () =>
          HttpResponse.json({ medicationId: 'med-new' }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/medications', () =>
          HttpResponse.json([]),
        ),
        http.post('*/api/v1/medical-records/*/medications', () =>
          HttpResponse.json({ medicationId: 'med-new' }),
        ),
      ],
    },
  },
};
