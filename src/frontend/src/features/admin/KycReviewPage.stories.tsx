import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import KycReviewPage from './KycReviewPage';
import { http, HttpResponse } from 'msw';

const documents = [
  { id: 'doc-1', documentType: 'LicensePhoto', fileName: 'medical-license-2026.jpg', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
  { id: 'doc-2', documentType: 'MedicalCertificate', fileName: 'board-certification.pdf', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
  { id: 'doc-3', documentType: 'IdentityProof', fileName: 'drivers-license.png', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
];

const submittedDetails = {
  doctorProfileId: 'dp-001',
  doctorName: 'Dr. Anika Volpe',
  specialty: 'Cardiology',
  licenseNumber: 'MED-29481',
  status: 'Submitted',
  submittedAt: new Date(Date.now() - 86400000).toISOString(),
  documents,
  reviewHistory: [],
};

const rejectedDetails = {
  ...submittedDetails,
  status: 'Rejected',
  reviewHistory: [
    {
      id: 'rev-1',
      decision: 'Rejected',
      rejectionReason: 'The license photo is too blurry to verify. Please upload a clearer image.',
      reviewerName: 'Lior Adler',
      reviewedAt: new Date(Date.now() - 172800000).toISOString(),
    },
  ],
};

const approvedDetails = {
  ...submittedDetails,
  status: 'Approved',
  reviewHistory: [
    {
      id: 'rev-1',
      decision: 'Approved',
      rejectionReason: null,
      reviewerName: 'Lior Adler',
      reviewedAt: new Date(Date.now() - 3600000).toISOString(),
    },
  ],
};

const meta: Meta<typeof KycReviewPage> = {
  title: 'Admin/KycReviewPage',
  component: KycReviewPage,
  args: {
    doctorProfileId: 'dp-001',
    onBack: () => {},
  },
  decorators: [
    (Story) => (
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <div className="dashboard-shell">
          <main className="dashboard-main dashboard-main--wide">
            <Story />
          </main>
        </div>
      </QueryClientProvider>
    ),
  ],
  parameters: { layout: 'fullscreen' },
};
export default meta;

type Story = StoryObj<typeof KycReviewPage>;

export const PendingReview: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/doctors/*/kyc', () => HttpResponse.json(submittedDetails)),
        http.post('*/api/v1/admin/doctors/*/kyc/review', async () => {
          await new Promise((r) => setTimeout(r, 800));
          return HttpResponse.json({ message: 'KYC review recorded' });
        }),
        http.get('*/api/v1/admin/kyc/documents/*', () =>
          new HttpResponse(new Blob(['fake-file-content']), {
            headers: { 'Content-Type': 'application/octet-stream' },
          }),
        ),
      ],
    },
  },
};

export const PreviouslyRejected: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/doctors/*/kyc', () => HttpResponse.json(rejectedDetails)),
      ],
    },
  },
};

export const AlreadyApproved: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/doctors/*/kyc', () => HttpResponse.json(approvedDetails)),
      ],
    },
  },
};

export const LoadingError: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/admin/doctors/*/kyc', () =>
          HttpResponse.json({ message: 'Internal error' }, { status: 500 }),
        ),
      ],
    },
  },
};
