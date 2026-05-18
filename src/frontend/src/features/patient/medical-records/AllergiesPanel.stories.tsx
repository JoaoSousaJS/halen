import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import AllergiesPanel from './AllergiesPanel';
import type { AllergyDto } from '../../../shared/api/medical-records';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof AllergiesPanel> = {
  title: 'Patient/MedicalRecords/AllergiesPanel',
  component: AllergiesPanel,
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

type Story = StoryObj<typeof AllergiesPanel>;

const mockAllergies: AllergyDto[] = [
  {
    id: 'allergy-1',
    allergenName: 'Penicillin',
    reaction: 'Anaphylaxis — throat swelling, difficulty breathing',
    severity: 'Severe',
    dateIdentified: '2022-08-15',
    isActive: true,
    addedBy: 'Dr. Costa',
    createdAt: '2022-08-15T10:00:00Z',
  },
  {
    id: 'allergy-2',
    allergenName: 'Peanuts',
    reaction: 'Hives, mild swelling',
    severity: 'Moderate',
    dateIdentified: '2018-03-20',
    isActive: true,
    addedBy: 'Dr. Andrade',
    createdAt: '2018-03-20T14:00:00Z',
  },
  {
    id: 'allergy-3',
    allergenName: 'Latex',
    reaction: 'Contact dermatitis',
    severity: 'Mild',
    dateIdentified: '2023-11-05',
    isActive: false,
    addedBy: 'Dr. Silva',
    createdAt: '2023-11-05T09:00:00Z',
  },
];

export const WithAllergies: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/allergies', () =>
          HttpResponse.json(mockAllergies),
        ),
        http.post('*/api/v1/medical-records/*/allergies', () =>
          HttpResponse.json({ allergyId: 'allergy-new' }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/allergies', () =>
          HttpResponse.json([]),
        ),
        http.post('*/api/v1/medical-records/*/allergies', () =>
          HttpResponse.json({ allergyId: 'allergy-new' }),
        ),
      ],
    },
  },
};
