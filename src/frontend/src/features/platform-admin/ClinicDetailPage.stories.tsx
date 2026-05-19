import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import ClinicDetailPage from './ClinicDetailPage';
import { http, HttpResponse } from 'msw';
import { fn } from 'storybook/test';
import { userEvent, within } from 'storybook/test';

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'pa-001', email: 'platform@test.com', given_name: 'Platform', family_name: 'Admin',
    role: 'PlatformAdmin', clinic_id: 'c-root', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const allFlags = [
  { featureKey: 'prescriptions', isEnabled: true },
  { featureKey: 'kyc', isEnabled: false },
  { featureKey: 'video_calls', isEnabled: true },
  { featureKey: 'doctor_reviews', isEnabled: false },
  { featureKey: 'medical_records', isEnabled: true },
  { featureKey: 'messaging', isEnabled: false },
  { featureKey: 'audit_trail', isEnabled: true },
];

const mockClinic = {
  id: 'c-001',
  name: 'Sunrise Health',
  slug: 'sunrise-health',
  isActive: true,
  userCount: 42,
  createdAt: '2026-03-01T10:00:00Z',
  featureFlags: allFlags,
};

const handlers = [
  http.get('*/api/v1/clinics/c-001', () => HttpResponse.json(mockClinic)),
  http.put('*/api/v1/clinics/c-001', () => new HttpResponse(null, { status: 204 })),
  http.put('*/api/v1/clinics/c-001/features/:key', () => new HttpResponse(null, { status: 204 })),
];

const meta: Meta<typeof ClinicDetailPage> = {
  title: 'PlatformAdmin/ClinicDetailPage',
  component: ClinicDetailPage,
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

type Story = StoryObj<typeof ClinicDetailPage>;

export const Default: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
};

export const InactiveClinic: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/clinics/c-001', () =>
          HttpResponse.json({ ...mockClinic, isActive: false, name: 'Deactivated Clinic' }),
        ),
      ],
    },
  },
};

export const EditingName: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const nameButton = await canvas.findByRole('button', { name: /Sunrise Health/ });
    await userEvent.click(nameButton);
  },
};

export const FlagToggleError: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/clinics/c-001', () => HttpResponse.json(mockClinic)),
        http.put('*/api/v1/clinics/c-001/features/:key', () =>
          HttpResponse.json({ message: 'Feature toggle failed' }, { status: 500 }),
        ),
      ],
    },
  },
};

export const AllFlagsEnabled: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/clinics/c-001', () =>
          HttpResponse.json({
            ...mockClinic,
            featureFlags: allFlags.map((f) => ({ ...f, isEnabled: true })),
          }),
        ),
      ],
    },
  },
};

export const AllFlagsDisabled: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/clinics/c-001', () =>
          HttpResponse.json({
            ...mockClinic,
            featureFlags: allFlags.map((f) => ({ ...f, isEnabled: false })),
          }),
        ),
      ],
    },
  },
};

export const LongClinicName: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/clinics/c-001', () =>
          HttpResponse.json({
            ...mockClinic,
            name: 'Centro Integrado de Saúde e Bem-Estar da Região Metropolitana de Lisboa Norte',
          }),
        ),
      ],
    },
  },
};

export const DeactivationConfirmation: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const statusSwitch = await canvas.findByRole('switch', { name: /active status/i });
    await userEvent.click(statusSwitch);
  },
};

export const UnknownFeatureKey: Story = {
  args: { clinicId: 'c-001', onBack: fn() },
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/clinics/c-001', () =>
          HttpResponse.json({
            ...mockClinic,
            featureFlags: [
              ...allFlags,
              { featureKey: 'beta_experimental', isEnabled: true },
            ],
          }),
        ),
      ],
    },
  },
};
