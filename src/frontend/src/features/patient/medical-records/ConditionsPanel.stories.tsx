import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import ConditionsPanel from './ConditionsPanel';
import type { ConditionDto } from '../../../shared/api/medical-records';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof ConditionsPanel> = {
  title: 'Patient/MedicalRecords/ConditionsPanel',
  component: ConditionsPanel,
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

type Story = StoryObj<typeof ConditionsPanel>;

const mockConditions: ConditionDto[] = [
  {
    id: 'cond-1',
    icdCode: 'I10',
    icdDescription: 'Essential Hypertension',
    dateOfOnset: '2025-06-15',
    severity: 'Moderate',
    status: 'Active',
    clinicalNotes: 'Patient reports occasional headaches. Started on ACE inhibitor.',
    addedBy: 'Dr. Silva',
    createdAt: '2025-06-15T14:00:00Z',
  },
  {
    id: 'cond-2',
    icdCode: 'J45',
    icdDescription: 'Asthma',
    dateOfOnset: '2020-03-10',
    severity: 'Mild',
    status: 'InRemission',
    clinicalNotes: null,
    addedBy: 'Dr. Andrade',
    createdAt: '2024-01-10T09:00:00Z',
  },
  {
    id: 'cond-3',
    icdCode: 'E11',
    icdDescription: 'Type 2 Diabetes Mellitus',
    dateOfOnset: '2024-11-01',
    severity: 'Severe',
    status: 'Active',
    clinicalNotes: 'HbA1c at 8.2%. Metformin + diet management. Follow-up in 3 months.',
    addedBy: 'Dr. Silva',
    createdAt: '2024-11-01T10:00:00Z',
  },
];

export const WithConditions: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/conditions', () =>
          HttpResponse.json(mockConditions),
        ),
        http.post('*/api/v1/medical-records/*/conditions', () =>
          HttpResponse.json({ conditionId: 'cond-new' }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/conditions', () =>
          HttpResponse.json([]),
        ),
        http.post('*/api/v1/medical-records/*/conditions', () =>
          HttpResponse.json({ conditionId: 'cond-new' }),
        ),
      ],
    },
  },
};

export const AddingNew: Story = {
  name: 'Adding New (click "Add Condition")',
  parameters: {
    docs: {
      description: {
        story:
          'Shows the conditions list with the form toggle. Click "Add Condition" to reveal the form for adding a new condition entry.',
      },
    },
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/conditions', () =>
          HttpResponse.json(mockConditions),
        ),
        http.post('*/api/v1/medical-records/*/conditions', () =>
          HttpResponse.json({ conditionId: 'cond-new' }),
        ),
      ],
    },
  },
};
