import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import AvailabilityEditor from './AvailabilityEditor';
import { http, HttpResponse } from 'msw';
import type { AvailabilityWindow } from '../../shared/api/availability';

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(
    JSON.stringify({
      sub: 'doc-001',
      email: 'doctor@test.com',
      given_name: 'Gregory',
      family_name: 'House',
      role: 'Doctor',
      exp: 9999999999,
    }),
  );
  return `${header}.${body}.fake`;
}

function makeWindow(
  day: string,
  start: string,
  end: string,
  id: string,
): AvailabilityWindow {
  return {
    id,
    dayOfWeek: day,
    startTime: start,
    endTime: end,
    slotDurationMinutes: 20,
  };
}

const emptyWindows: AvailabilityWindow[] = [];

const partialWindows: AvailabilityWindow[] = [
  makeWindow('Monday', '09:00', '12:00', 'w-1'),
  makeWindow('Monday', '14:00', '17:00', 'w-2'),
  makeWindow('Wednesday', '10:00', '13:00', 'w-3'),
  makeWindow('Friday', '08:00', '11:00', 'w-4'),
  makeWindow('Friday', '13:00', '16:00', 'w-5'),
];

const fullWindows: AvailabilityWindow[] = [
  makeWindow('Monday', '09:00', '12:00', 'w-10'),
  makeWindow('Monday', '14:00', '17:00', 'w-11'),
  makeWindow('Tuesday', '08:00', '12:00', 'w-12'),
  makeWindow('Wednesday', '09:00', '13:00', 'w-13'),
  makeWindow('Thursday', '10:00', '14:00', 'w-14'),
  makeWindow('Thursday', '15:00', '18:00', 'w-15'),
  makeWindow('Friday', '09:00', '12:00', 'w-16'),
  makeWindow('Saturday', '10:00', '13:00', 'w-17'),
  makeWindow('Sunday', '11:00', '14:00', 'w-18'),
];

function availabilityHandlers(windows: AvailabilityWindow[]) {
  return [
    http.get('*/api/v1/availability/mine', () =>
      HttpResponse.json({ windows }),
    ),
    http.put('*/api/v1/availability/mine', () =>
      new HttpResponse(null, { status: 204 }),
    ),
  ];
}

const meta: Meta<typeof AvailabilityEditor> = {
  title: 'Features/AvailabilityEditor',
  component: AvailabilityEditor,
  decorators: [
    (Story) => {
      localStorage.setItem('token', fakeJwt());
      return (
        <QueryClientProvider
          client={
            new QueryClient({
              defaultOptions: { queries: { retry: false } },
            })
          }
        >
          <MemoryRouter>
            <AuthProvider>
              <div
                style={{ padding: 24, minHeight: 200, background: '#0b0e0c' }}
              >
                <Story />
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

type Story = StoryObj<typeof AvailabilityEditor>;

export const EmptySchedule: Story = {
  parameters: {
    msw: { handlers: availabilityHandlers(emptyWindows) },
  },
};

export const PartialSchedule: Story = {
  parameters: {
    msw: { handlers: availabilityHandlers(partialWindows) },
  },
};

export const FullSchedule: Story = {
  parameters: {
    msw: { handlers: availabilityHandlers(fullWindows) },
  },
};
