import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from './AuthProvider';
import { FeatureGate } from './FeatureGate';
import { http, HttpResponse } from 'msw';

function fakeJwt(overrides: Record<string, unknown> = {}): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: 'p-001', email: 'patient@test.com', given_name: 'Test', family_name: 'Patient',
    role: 'Patient', clinic_id: 'c-001', exp: 9999999999, ...overrides,
  }));
  return `${header}.${body}.fake`;
}

const enabledHandlers = [
  http.get('*/api/v1/me/features', () =>
    HttpResponse.json([
      { featureKey: 'prescriptions', isEnabled: true },
      { featureKey: 'kyc', isEnabled: false },
    ]),
  ),
];

const disabledHandlers = [
  http.get('*/api/v1/me/features', () =>
    HttpResponse.json([
      { featureKey: 'prescriptions', isEnabled: false },
      { featureKey: 'kyc', isEnabled: false },
    ]),
  ),
];

const meta: Meta<typeof FeatureGate> = {
  title: 'Shared/FeatureGate',
  component: FeatureGate,
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
};
export default meta;

type Story = StoryObj<typeof FeatureGate>;

export const FeatureEnabled: Story = {
  args: {
    feature: 'prescriptions',
    children: <div style={{ padding: 16, background: '#e8f5e9', borderRadius: 8 }}>Prescriptions section is visible</div>,
  },
  parameters: { msw: { handlers: enabledHandlers } },
};

export const FeatureDisabled: Story = {
  args: {
    feature: 'prescriptions',
    children: <div style={{ padding: 16, background: '#e8f5e9', borderRadius: 8 }}>This should NOT be visible</div>,
    fallback: <div style={{ padding: 16, background: '#fff3e0', borderRadius: 8 }}>Feature not available for your clinic</div>,
  },
  parameters: { msw: { handlers: disabledHandlers } },
};

export const FeatureDisabledNoFallback: Story = {
  args: {
    feature: 'prescriptions',
    children: <div>Hidden content</div>,
  },
  parameters: { msw: { handlers: disabledHandlers } },
};
