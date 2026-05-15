import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import KycSetup from './KycSetup';
import { http, HttpResponse } from 'msw';

const meta: Meta<typeof KycSetup> = {
  title: 'Doctor/KycSetup',
  component: KycSetup,
  decorators: [
    (Story) => (
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <div className="dashboard-shell">
          <main className="dashboard-main" style={{ maxWidth: 640, margin: '0 auto' }}>
            <Story />
          </main>
        </div>
      </QueryClientProvider>
    ),
  ],
  parameters: { layout: 'fullscreen' },
};
export default meta;

type Story = StoryObj<typeof KycSetup>;

export const NotSubmitted: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () =>
          HttpResponse.json({
            status: 'NotSubmitted',
            submittedAt: null,
            lastRejectionReason: null,
            documents: [],
          }),
        ),
        http.post('*/api/v1/doctor/kyc/documents', async () => {
          await new Promise((r) => setTimeout(r, 1000));
          return HttpResponse.json({ message: 'KYC documents submitted successfully' });
        }),
      ],
    },
  },
};

export const Submitted: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () =>
          HttpResponse.json({
            status: 'Submitted',
            submittedAt: new Date(Date.now() - 86400000).toISOString(),
            lastRejectionReason: null,
            documents: [
              { id: 'doc-1', documentType: 'LicensePhoto', fileName: 'medical-license-2026.jpg', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
              { id: 'doc-2', documentType: 'MedicalCertificate', fileName: 'board-certification.pdf', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
              { id: 'doc-3', documentType: 'IdentityProof', fileName: 'drivers-license.png', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
            ],
          }),
        ),
      ],
    },
  },
};

export const Rejected: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () =>
          HttpResponse.json({
            status: 'Rejected',
            submittedAt: new Date(Date.now() - 172800000).toISOString(),
            lastRejectionReason: 'The license photo is too blurry to verify. Please upload a clearer image.',
            documents: [],
          }),
        ),
        http.post('*/api/v1/doctor/kyc/documents', async () => {
          await new Promise((r) => setTimeout(r, 1000));
          return HttpResponse.json({ message: 'KYC documents submitted successfully' });
        }),
      ],
    },
  },
};

export const Approved: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () =>
          HttpResponse.json({
            status: 'Approved',
            submittedAt: new Date(Date.now() - 604800000).toISOString(),
            lastRejectionReason: null,
            documents: [],
          }),
        ),
      ],
    },
  },
};

export const SubmissionError: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/doctor/kyc/status', () =>
          HttpResponse.json({
            status: 'NotSubmitted',
            submittedAt: null,
            lastRejectionReason: null,
            documents: [],
          }),
        ),
        http.post('*/api/v1/doctor/kyc/documents', () =>
          HttpResponse.json({ error: 'Invalid file format detected' }, { status: 400 }),
        ),
      ],
    },
  },
};
