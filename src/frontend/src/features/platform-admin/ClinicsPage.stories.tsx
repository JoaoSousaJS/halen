import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import ClinicsPage from './ClinicsPage';
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

const mockClinics = [
  { id: 'c-001', name: 'Sunrise Health', slug: 'sunrise-health', isActive: true, createdAt: '2026-03-01T10:00:00Z' },
  { id: 'c-002', name: 'Metro Medical', slug: 'metro-medical', isActive: true, createdAt: '2026-04-15T08:30:00Z' },
  { id: 'c-003', name: 'Valley Care', slug: 'valley-care', isActive: false, createdAt: '2026-01-20T14:00:00Z' },
];

const handlers = [
  http.get('*/api/v1/clinics', () =>
    HttpResponse.json({ clinics: mockClinics, totalCount: mockClinics.length }),
  ),
  http.post('*/api/v1/clinics', () =>
    HttpResponse.json({ clinicId: 'c-new' }, { status: 201 }),
  ),
];

const meta: Meta<typeof ClinicsPage> = {
  title: 'PlatformAdmin/ClinicsPage',
  component: ClinicsPage,
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
    msw: { handlers },
  },
};
export default meta;

type Story = StoryObj<typeof ClinicsPage>;

export const Default: Story = {
  args: { onSelectClinic: fn() },
};

export const EmptyState: Story = {
  args: { onSelectClinic: fn() },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/clinics', () =>
          HttpResponse.json({ clinics: [], totalCount: 0 }),
        ),
      ],
    },
  },
};
