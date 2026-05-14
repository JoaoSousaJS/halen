import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import DoctorDashboard from './DoctorDashboard';
import { http, HttpResponse } from 'msw';

const mockAppointments = [
  {
    id: 'appt-1',
    scheduledAt: new Date(Date.now() + 86400000).toISOString(),
    durationMinutes: 20,
    reason: 'Chest pain evaluation',
    status: 'Scheduled',
    notes: null,
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'Maya Chen',
  },
  {
    id: 'appt-2',
    scheduledAt: new Date(Date.now() + 172800000).toISOString(),
    durationMinutes: 20,
    reason: 'Headache follow-up',
    status: 'Scheduled',
    notes: null,
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'John Smith',
  },
  {
    id: 'appt-3',
    scheduledAt: new Date(Date.now() - 86400000).toISOString(),
    durationMinutes: 20,
    reason: 'Annual checkup',
    status: 'Completed',
    notes: 'Patient is healthy. Recommended exercise.',
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'Sarah Connor',
  },
];

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '2', email: 'doctor@test.com', given_name: 'Gregory', family_name: 'House', role: 'Doctor', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const meta: Meta<typeof DoctorDashboard> = {
  title: 'Doctor/DoctorDashboard',
  component: DoctorDashboard,
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

type Story = StoryObj<typeof DoctorDashboard>;

export const WithSchedule: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/appointments', () => HttpResponse.json(mockAppointments)),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/appointments', () => HttpResponse.json([])),
      ],
    },
  },
};
