import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import AuditLogPage from './AuditLogPage';
import { http, HttpResponse } from 'msw';

const sampleLogs = [
  { id: 'a1', timestamp: new Date(Date.now() - 2 * 60_000).toISOString(), actorId: 'u-1', actorName: 'Maya Chen', action: 'BookAppointment', targetId: 'apt-1234-5678', metadata: '{"DoctorId":"d-001","Reason":"Annual checkup","ScheduledAt":"2026-05-20T10:00:00Z"}', ipAddress: '192.168.1.42' },
  { id: 'a2', timestamp: new Date(Date.now() - 15 * 60_000).toISOString(), actorId: 'u-2', actorName: 'Dr. Anika Volpe', action: 'IssuePrescription', targetId: 'rx-9876-5432', metadata: '{"DrugName":"Amoxicillin","Dosage":"500mg","PatientId":"[REDACTED]"}', ipAddress: '10.0.0.15' },
  { id: 'a3', timestamp: new Date(Date.now() - 30 * 60_000).toISOString(), actorId: 'a-1', actorName: 'Lior Adler', action: 'CreateDoctor', targetId: 'd-002', metadata: '{"FirstName":"Tomás","LastName":"Reyes","Password":"[REDACTED]"}', ipAddress: '172.16.0.1' },
  { id: 'a4', timestamp: new Date(Date.now() - 45 * 60_000).toISOString(), actorId: 'u-3', actorName: 'Elena Kowalski', action: 'CancelAppointment', targetId: 'apt-2222-3333', metadata: null, ipAddress: '192.168.1.100' },
  { id: 'a5', timestamp: new Date(Date.now() - 60 * 60_000).toISOString(), actorId: 'u-1', actorName: 'Maya Chen', action: 'LoginSuccess', targetId: 'u-1', metadata: null, ipAddress: '192.168.1.42' },
  { id: 'a6', timestamp: new Date(Date.now() - 90 * 60_000).toISOString(), actorId: '00000000-0000-0000-0000-000000000000', actorName: 'unknown@bad.com', action: 'LoginFailure', targetId: '00000000-0000-0000-0000-000000000000', metadata: 'Invalid credentials', ipAddress: '45.33.32.156' },
  { id: 'a7', timestamp: new Date(Date.now() - 120 * 60_000).toISOString(), actorId: 'a-1', actorName: 'Lior Adler', action: 'SetFeatureFlag', targetId: 'clinic-1', metadata: '{"FeatureKey":"audit_trail","IsEnabled":true}', ipAddress: '172.16.0.1' },
];

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'a-001', email: 'admin@test.com', given_name: 'Lior', family_name: 'Adler', role: 'PlatformAdmin', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const meta: Meta<typeof AuditLogPage> = {
  title: 'Admin/AuditLogPage',
  component: AuditLogPage,
  decorators: [
    (Story) => {
      localStorage.setItem('token', fakeJwt());
      return (
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <MemoryRouter>
            <AuthProvider>
              <div className="dashboard-shell">
                <main className="dashboard-main dashboard-main--wide">
                  <Story />
                </main>
              </div>
            </AuthProvider>
          </MemoryRouter>
        </QueryClientProvider>
      );
    },
  ],
  parameters: { layout: 'fullscreen' },
};
export default meta;

type Story = StoryObj<typeof AuditLogPage>;

export const Default: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/audit-logs', () =>
          HttpResponse.json({ logs: sampleLogs, totalCount: sampleLogs.length }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/audit-logs', () =>
          HttpResponse.json({ logs: [], totalCount: 0 }),
        ),
      ],
    },
  },
};

export const Loading: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/audit-logs', async () => {
          await new Promise((r) => setTimeout(r, 999_999));
          return HttpResponse.json({ logs: [], totalCount: 0 });
        }),
      ],
    },
  },
};

export const WithExpandedRow: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/audit-logs', () =>
          HttpResponse.json({ logs: sampleLogs, totalCount: sampleLogs.length }),
        ),
      ],
    },
  },
};

export const ManyPages: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/audit-logs', () =>
          HttpResponse.json({ logs: sampleLogs, totalCount: 237 }),
        ),
      ],
    },
  },
};
