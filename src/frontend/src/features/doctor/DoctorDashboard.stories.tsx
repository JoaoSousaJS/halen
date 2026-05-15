import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import DoctorDashboard from './DoctorDashboard';
import { http, HttpResponse } from 'msw';

const mockAppointments = [
  {
    id: 'appt-1',
    scheduledAt: new Date(Date.now() + 86400000).toISOString(),
    durationMinutes: 20,
    reason: 'Chest pain evaluation',
    status: 'Scheduled',
    notes: null,
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'Maya Chen',
    patientId: 'patient-1',
  },
  {
    id: 'appt-2',
    scheduledAt: new Date(Date.now() + 172800000).toISOString(),
    durationMinutes: 20,
    reason: 'Headache follow-up',
    status: 'Scheduled',
    notes: null,
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'John Smith',
    patientId: 'patient-2',
  },
  {
    id: 'appt-3',
    scheduledAt: new Date(Date.now() - 86400000).toISOString(),
    durationMinutes: 20,
    reason: 'Annual checkup',
    status: 'Completed',
    notes: 'Patient is healthy. Recommended exercise.',
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'Sarah Connor',
    patientId: 'patient-3',
  },
];

const mockPrescriptions = [
  {
    id: 'rx-1',
    drugName: 'Amoxicillin',
    dosage: '500mg',
    frequency: 'Twice daily',
    refillsRemaining: 3,
    status: 'Active',
    pharmacyName: 'CVS Pharmacy',
    doctorName: 'Dr. House',
    patientName: 'Maya Chen',
    createdAt: new Date(Date.now() - 86400000).toISOString(),
  },
  {
    id: 'rx-2',
    drugName: 'Ibuprofen',
    dosage: '400mg',
    frequency: 'As needed',
    refillsRemaining: 0,
    status: 'Completed',
    pharmacyName: null,
    doctorName: 'Dr. House',
    patientName: 'John Smith',
    createdAt: new Date(Date.now() - 604800000).toISOString(),
  },
];

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '2', email: 'doctor@test.com', given_name: 'Gregory', family_name: 'House', role: 'Doctor', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

const meta: Meta<typeof DoctorDashboard> = {
  title: 'Doctor/DoctorDashboard',
  component: DoctorDashboard,
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
  },
};
export default meta;

type Story = StoryObj<typeof DoctorDashboard>;

const kycApproved = {
  status: 'Approved',
  submittedAt: new Date(Date.now() - 604800000).toISOString(),
  lastRejectionReason: null,
  documents: [],
};

const kycNotSubmitted = {
  status: 'NotSubmitted',
  submittedAt: null,
  lastRejectionReason: null,
  documents: [],
};

const kycSubmitted = {
  status: 'Submitted',
  submittedAt: new Date(Date.now() - 86400000).toISOString(),
  lastRejectionReason: null,
  documents: [
    { id: 'doc-1', documentType: 'LicensePhoto', fileName: 'license.jpg', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
    { id: 'doc-2', documentType: 'MedicalCertificate', fileName: 'cert.pdf', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
    { id: 'doc-3', documentType: 'IdentityProof', fileName: 'id.png', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
  ],
};

const kycRejected = {
  status: 'Rejected',
  submittedAt: new Date(Date.now() - 172800000).toISOString(),
  lastRejectionReason: 'The license photo is too blurry to verify. Please upload a clearer image.',
  documents: [],
};

export const WithSchedule: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () => HttpResponse.json(kycApproved)),
        http.get('*/api/v1/appointments', () => HttpResponse.json(mockAppointments)),
        http.get('*/api/v1/prescriptions', () => HttpResponse.json(mockPrescriptions)),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () => HttpResponse.json(kycApproved)),
        http.get('*/api/v1/appointments', () => HttpResponse.json([])),
        http.get('*/api/v1/prescriptions', () => HttpResponse.json([])),
      ],
    },
  },
};

export const KycNotSubmitted: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () => HttpResponse.json(kycNotSubmitted)),
        http.get('*/api/v1/appointments', () => HttpResponse.json([])),
        http.get('*/api/v1/prescriptions', () => HttpResponse.json([])),
      ],
    },
  },
};

export const KycSubmitted: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () => HttpResponse.json(kycSubmitted)),
        http.get('*/api/v1/appointments', () => HttpResponse.json([])),
        http.get('*/api/v1/prescriptions', () => HttpResponse.json([])),
      ],
    },
  },
};

export const KycRejected: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () => HttpResponse.json(kycRejected)),
        http.get('*/api/v1/appointments', () => HttpResponse.json([])),
        http.get('*/api/v1/prescriptions', () => HttpResponse.json([])),
      ],
    },
  },
};
