import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import ProfilePage from './ProfilePage';
import { http, HttpResponse } from 'msw';
import type { ProfileDto } from '../../shared/api/profile';

function fakeJwt(overrides: {
  sub: string;
  email: string;
  given_name: string;
  family_name: string;
  role: string;
}): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({ ...overrides, exp: 9999999999 }));
  return `${header}.${body}.fake`;
}

const baseProfile: Omit<ProfileDto, 'role'> = {
  id: 'u-001',
  firstName: 'Maya',
  lastName: 'Chen',
  email: 'maya@test.com',
  createdAt: '2026-01-15T10:30:00Z',
  lastLoginAt: '2026-05-16T08:00:00Z',
  specialty: null,
  consultationFee: null,
  yearsOfExperience: null,
  languages: null,
  dateOfBirth: null,
  city: null,
  subscriptionPlan: null,
};

const patientProfile: ProfileDto = {
  ...baseProfile,
  role: 'Patient',
  dateOfBirth: '1992-06-15',
  city: 'Portland',
};

const doctorProfile: ProfileDto = {
  ...baseProfile,
  id: 'doc-001',
  firstName: 'Gregory',
  lastName: 'House',
  email: 'house@test.com',
  role: 'Doctor',
  specialty: 'Diagnostics',
  consultationFee: 150,
  yearsOfExperience: 20,
  languages: ['English', 'Spanish', 'French'],
};

const adminProfile: ProfileDto = {
  ...baseProfile,
  id: 'admin-001',
  firstName: 'Lisa',
  lastName: 'Cuddy',
  email: 'cuddy@test.com',
  role: 'PlatformAdmin',
};

function profileHandlers(profile: ProfileDto) {
  return [
    http.get('*/api/v1/profile/me', () =>
      HttpResponse.json({ profile }),
    ),
    http.put('*/api/v1/profile/me', () =>
      new HttpResponse(null, { status: 204 }),
    ),
    http.post('*/api/v1/profile/me/change-password', () =>
      new HttpResponse(null, { status: 204 }),
    ),
  ];
}

function profileHandlersWithPasswordError(profile: ProfileDto) {
  return [
    http.get('*/api/v1/profile/me', () =>
      HttpResponse.json({ profile }),
    ),
    http.put('*/api/v1/profile/me', () =>
      new HttpResponse(null, { status: 204 }),
    ),
    http.post('*/api/v1/profile/me/change-password', () =>
      HttpResponse.json(
        { error: 'Current password is incorrect.' },
        { status: 400 },
      ),
    ),
  ];
}

function makeDecorator(jwt: string) {
  return (Story: React.ComponentType) => {
    localStorage.setItem('token', jwt);
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
            <Story />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
  };
}

const meta: Meta<typeof ProfilePage> = {
  title: 'Features/ProfilePage',
  component: ProfilePage,
  parameters: { layout: 'fullscreen' },
};
export default meta;

type Story = StoryObj<typeof ProfilePage>;

export const PatientProfile: Story = {
  decorators: [
    makeDecorator(
      fakeJwt({
        sub: patientProfile.id,
        email: patientProfile.email,
        given_name: patientProfile.firstName,
        family_name: patientProfile.lastName,
        role: 'Patient',
      }),
    ),
  ],
  parameters: {
    msw: { handlers: profileHandlers(patientProfile) },
  },
};

export const DoctorProfile: Story = {
  decorators: [
    makeDecorator(
      fakeJwt({
        sub: doctorProfile.id,
        email: doctorProfile.email,
        given_name: doctorProfile.firstName,
        family_name: doctorProfile.lastName,
        role: 'Doctor',
      }),
    ),
  ],
  parameters: {
    msw: { handlers: profileHandlers(doctorProfile) },
  },
};

export const AdminProfile: Story = {
  decorators: [
    makeDecorator(
      fakeJwt({
        sub: adminProfile.id,
        email: adminProfile.email,
        given_name: adminProfile.firstName,
        family_name: adminProfile.lastName,
        role: 'PlatformAdmin',
      }),
    ),
  ],
  parameters: {
    msw: { handlers: profileHandlers(adminProfile) },
  },
};

export const PasswordChangeError: Story = {
  decorators: [
    makeDecorator(
      fakeJwt({
        sub: patientProfile.id,
        email: patientProfile.email,
        given_name: patientProfile.firstName,
        family_name: patientProfile.lastName,
        role: 'Patient',
      }),
    ),
  ],
  parameters: {
    msw: { handlers: profileHandlersWithPasswordError(patientProfile) },
  },
};
