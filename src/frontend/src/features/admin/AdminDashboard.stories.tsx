import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import AdminDashboard from './AdminDashboard';
import { http, HttpResponse } from 'msw';

const mockUsers = [
  { id: 'd-022', name: 'Dr. Anika Volpe', role: 'Doctor', status: 'PendingReview', plan: null, lastLoginAt: new Date().toISOString(), isFlagged: true },
  { id: 'p-044', name: 'Wesley Tanaka', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: new Date().toISOString(), isFlagged: false },
];

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'a-001', email: 'admin@test.com', given_name: 'Lior', family_name: 'Adler', role: 'Admin', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const adminHandlers = [
  http.get('*/api/v1/admin/users', () =>
    HttpResponse.json({ users: mockUsers, totalCount: mockUsers.length }),
  ),
  http.post('*/api/v1/admin/doctors', () =>
    HttpResponse.json({ doctorId: 'd-new-001' }),
  ),
];

const meta: Meta<typeof AdminDashboard> = {
  title: 'Admin/AdminDashboard',
  component: AdminDashboard,
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
    msw: { handlers: adminHandlers },
  },
};
export default meta;

type Story = StoryObj<typeof AdminDashboard>;

export const UsersTab: Story = {};

export const CreateDoctorTab: Story = {
  play: async ({ canvasElement }) => {
    const btn = canvasElement.querySelector('.admin-nav-btn:last-child') as HTMLButtonElement;
    btn?.click();
  },
};
