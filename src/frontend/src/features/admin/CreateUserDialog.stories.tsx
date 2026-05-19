import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import CreateUserDialog from './CreateUserDialog';
import { http, HttpResponse } from 'msw';
import { fn } from 'storybook/test';

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'ca-001', email: 'clinicadmin@test.com', given_name: 'Clinic', family_name: 'Admin',
    role: 'ClinicAdmin', clinic_id: 'c-001', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const successHandlers = [
  http.post('*/api/v1/clinic/users', () =>
    HttpResponse.json({ userId: 'u-new-001' }, { status: 201 }),
  ),
];

const errorHandlers = [
  http.post('*/api/v1/clinic/users', () =>
    HttpResponse.json({ error: 'Email already exists' }, { status: 400 }),
  ),
];

const meta: Meta<typeof CreateUserDialog> = {
  title: 'Admin/CreatePatientDialog',
  component: CreateUserDialog,
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

type Story = StoryObj<typeof CreateUserDialog>;

export const Default: Story = {
  args: { onClose: fn(), onCreated: fn() },
};

export const WithServerError: Story = {
  args: { onClose: fn(), onCreated: fn() },
  parameters: { msw: { handlers: errorHandlers } },
};
