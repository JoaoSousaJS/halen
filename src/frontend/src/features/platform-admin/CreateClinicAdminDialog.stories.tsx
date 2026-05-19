import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import CreateClinicAdminDialog from './CreateClinicAdminDialog';
import { http, HttpResponse } from 'msw';
import { fn } from 'storybook/test';

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'pa-001', email: 'platform@test.com', given_name: 'Platform', family_name: 'Admin',
    role: 'PlatformAdmin', clinic_id: 'c-root', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const successHandlers = [
  http.post('*/api/v1/clinics/c-001/admins', () =>
    HttpResponse.json({ userId: 'u-new-001' }, { status: 201 }),
  ),
];

const errorHandlers = [
  http.post('*/api/v1/clinics/c-001/admins', () =>
    HttpResponse.json({ error: 'Email already exists' }, { status: 400 }),
  ),
];

const meta: Meta<typeof CreateClinicAdminDialog> = {
  title: 'PlatformAdmin/CreateClinicAdminDialog',
  component: CreateClinicAdminDialog,
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
    layout: 'centered',
    msw: { handlers: successHandlers },
  },
};
export default meta;

type Story = StoryObj<typeof CreateClinicAdminDialog>;

export const Default: Story = {
  args: { clinicId: 'c-001', onClose: fn(), onCreated: fn() },
};

export const WithServerError: Story = {
  args: { clinicId: 'c-001', onClose: fn(), onCreated: fn() },
  parameters: { msw: { handlers: errorHandlers } },
};
