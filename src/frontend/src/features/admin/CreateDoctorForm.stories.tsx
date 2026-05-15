import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import CreateDoctorForm from './CreateDoctorForm';
import { http, HttpResponse } from 'msw';

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'a-001', email: 'admin@test.com', given_name: 'Lior', family_name: 'Adler', role: 'Admin', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const meta: Meta<typeof CreateDoctorForm> = {
  title: 'Admin/CreateDoctorForm',
  component: CreateDoctorForm,
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

type Story = StoryObj<typeof CreateDoctorForm>;

export const Default: Story = {
  parameters: {
    msw: {
      handlers: [
        http.post('*/api/v1/admin/doctors', () =>
          HttpResponse.json({ doctorId: 'd-new-001' }),
        ),
      ],
    },
  },
};

export const SubmissionError: Story = {
  parameters: {
    msw: {
      handlers: [
        http.post('*/api/v1/admin/doctors', () =>
          HttpResponse.json({ error: 'A doctor with this email already exists' }, { status: 400 }),
        ),
      ],
    },
  },
};
