import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import PatientDashboard from './PatientDashboard';
import { http, HttpResponse } from 'msw';

const mockDoctors = [
  { id: 'doc-1', name: 'Dr. House', specialty: 'Diagnostics', consultationFee: 150, yearsOfExperience: 20 },
  { id: 'doc-2', name: 'Dr. Grey', specialty: 'Surgery', consultationFee: 200, yearsOfExperience: 8 },
];

const mockAppointments = [
  {
    id: 'appt-1',
    scheduledAt: new Date(Date.now() + 86400000).toISOString(),
    durationMinutes: 20,
    reason: 'Annual checkup',
    status: 'Scheduled',
    notes: null,
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'Maya Chen',
  },
  {
    id: 'appt-2',
    scheduledAt: new Date(Date.now() - 86400000).toISOString(),
    durationMinutes: 20,
    reason: 'Follow-up visit',
    status: 'Completed',
    notes: 'Patient is recovering well.',
    doctorName: 'Dr. Grey',
    specialty: 'Surgery',
    consultationFee: 200,
    patientName: 'Maya Chen',
  },
];

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '1', email: 'patient@test.com', given_name: 'Maya', family_name: 'Chen', role: 'Patient', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const meta: Meta<typeof PatientDashboard> = {
  title: 'Patient/PatientDashboard',
  component: PatientDashboard,
  decorators: [
    (Story) => {
      localStorage.setItem('token', fakeJwt());
      return (
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <MemoryRouter>
            <AuthProvider>
              <Story />
            </AuthProvider>
          </MemoryRouter>
        </QueryClientProvider>
      );
    },
  ],
  parameters: {
    layout: 'fullscreen',
  },
};
export default meta;

type Story = StoryObj<typeof PatientDashboard>;

export const WithAppointments: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/appointments/doctors', () => HttpResponse.json(mockDoctors)),
        http.get('*/api/v1/appointments', () => HttpResponse.json(mockAppointments)),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/appointments/doctors', () => HttpResponse.json(mockDoctors)),
        http.get('*/api/v1/appointments', () => HttpResponse.json([])),
      ],
    },
  },
};
