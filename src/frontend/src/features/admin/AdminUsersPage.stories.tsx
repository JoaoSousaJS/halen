import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import AdminUsersPage from './AdminUsersPage';
import { http, HttpResponse } from 'msw';

const allUsers = [
  { id: 'd-022-aaaa-bbbb', name: 'Dr. Anika Volpe', role: 'Doctor', status: 'PendingReview', plan: null, lastLoginAt: new Date(Date.now() - 4 * 60_000).toISOString(), isFlagged: true },
  { id: 'p-198-cccc-dddd', name: 'Elena Kowalski', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: new Date(Date.now() - 60 * 60_000).toISOString(), isFlagged: true },
  { id: 'd-023-eeee-ffff', name: 'Dr. Tomás Reyes', role: 'Doctor', status: 'Active', plan: null, lastLoginAt: new Date(Date.now() - 2 * 60 * 60_000).toISOString(), isFlagged: false },
  { id: 'p-044-1111-2222', name: 'Wesley Tanaka', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: new Date(Date.now() - 12 * 60_000).toISOString(), isFlagged: false },
  { id: 'p-087-3333-4444', name: 'Soyeon Han', role: 'Patient', status: 'Active', plan: 'Family', lastLoginAt: new Date(Date.now() - 34 * 60_000).toISOString(), isFlagged: false },
  { id: 'd-004-5555-6666', name: 'Dr. Imani Okafor', role: 'Doctor', status: 'Active', plan: null, lastLoginAt: new Date().toISOString(), isFlagged: false },
  { id: 'p-001-7777-8888', name: 'Maya Chen', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: new Date().toISOString(), isFlagged: false },
  { id: 'p-212-9999-0000', name: 'Bertrand Léger', role: 'Patient', status: 'Idle', plan: 'Essentials', lastLoginAt: new Date(Date.now() - 6 * 24 * 60 * 60_000).toISOString(), isFlagged: false },
];

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'a-001', email: 'admin@test.com', given_name: 'Lior', family_name: 'Adler', role: 'Admin', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const meta: Meta<typeof AdminUsersPage> = {
  title: 'Admin/AdminUsersPage',
  component: AdminUsersPage,
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

type Story = StoryObj<typeof AdminUsersPage>;

export const WithUsers: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/users', ({ request }) => {
          const url = new URL(request.url);
          const role = url.searchParams.get('role');
          const flaggedOnly = url.searchParams.get('flaggedOnly') === 'true';
          const search = url.searchParams.get('search')?.toLowerCase();

          let filtered = [...allUsers];
          if (role) filtered = filtered.filter((u) => u.role.toLowerCase() === role);
          if (flaggedOnly) filtered = filtered.filter((u) => u.isFlagged);
          if (search) filtered = filtered.filter((u) => u.name.toLowerCase().includes(search) || u.id.includes(search));

          return HttpResponse.json({ users: filtered, totalCount: filtered.length });
        }),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/users', () => HttpResponse.json({ users: [], totalCount: 0 })),
      ],
    },
  },
};

export const Error: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/users', () =>
          HttpResponse.json({ message: 'Internal error' }, { status: 500 }),
        ),
      ],
    },
  },
};

export const FlaggedOnly: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/users', () => {
          const flagged = allUsers.filter((u) => u.isFlagged);
          return HttpResponse.json({ users: flagged, totalCount: flagged.length });
        }),
      ],
    },
  },
};
