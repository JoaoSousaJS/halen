import type { Meta, StoryObj } from '@storybook/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import DocumentsPanel from './DocumentsPanel';
import type { DocumentDto } from '../../../shared/api/medical-records';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof DocumentsPanel> = {
  title: 'Patient/MedicalRecords/DocumentsPanel',
  component: DocumentsPanel,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <div style={{ padding: 24, maxWidth: 700, background: '#0b0e0c' }}>
          <Story />
        </div>
      </QueryClientProvider>
    ),
  ],
  args: {
    patientProfileId: 'pp-1',
  },
};
export default meta;

type Story = StoryObj<typeof DocumentsPanel>;

const mockDocuments: DocumentDto[] = [
  {
    id: 'doc-1',
    documentType: 'LabResult',
    title: 'Complete Blood Count (CBC)',
    description: 'Routine annual blood work panel.',
    fileName: 'cbc-results-2026-04.pdf',
    contentType: 'application/pdf',
    fileSizeBytes: 245760,
    uploadedBy: 'Dr. Silva',
    createdAt: '2026-04-20T16:00:00Z',
  },
  {
    id: 'doc-2',
    documentType: 'Imaging',
    title: 'Chest X-Ray',
    description: 'Baseline imaging for asthma follow-up.',
    fileName: 'chest-xray-frontal.dcm',
    contentType: 'application/dicom',
    fileSizeBytes: 8388608,
    uploadedBy: 'Dr. Andrade',
    createdAt: '2026-03-15T11:00:00Z',
  },
  {
    id: 'doc-3',
    documentType: 'Referral',
    title: 'Cardiology Referral',
    description: 'Referral for hypertension specialist evaluation.',
    fileName: 'referral-cardiology.pdf',
    contentType: 'application/pdf',
    fileSizeBytes: 102400,
    uploadedBy: 'Dr. Costa',
    createdAt: '2026-02-28T09:00:00Z',
  },
  {
    id: 'doc-4',
    documentType: 'Other',
    title: 'Insurance Authorization',
    description: null,
    fileName: 'auth-letter.pdf',
    contentType: 'application/pdf',
    fileSizeBytes: 51200,
    uploadedBy: 'Maya Chen',
    createdAt: '2026-01-10T14:00:00Z',
  },
];

export const WithDocuments: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/documents', () =>
          HttpResponse.json(mockDocuments),
        ),
        http.post('*/api/v1/medical-records/*/documents', () =>
          HttpResponse.json({ documentId: 'doc-new' }),
        ),
        http.delete('*/api/v1/medical-records/documents/*', () =>
          new HttpResponse(null, { status: 204 }),
        ),
      ],
    },
  },
};

export const Empty: Story = {
  parameters: {
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/documents', () =>
          HttpResponse.json([]),
        ),
        http.post('*/api/v1/medical-records/*/documents', () =>
          HttpResponse.json({ documentId: 'doc-new' }),
        ),
      ],
    },
  },
};

export const Filtered: Story = {
  name: 'Filtered (use filter dropdown)',
  parameters: {
    docs: {
      description: {
        story:
          'Shows documents with the filter dropdown. Select a document type from the filter to see the client-side filtering in action.',
      },
    },
    msw: {
      handlers: [
        http.get('*/api/v1/medical-records/*/documents', () =>
          HttpResponse.json(mockDocuments),
        ),
        http.post('*/api/v1/medical-records/*/documents', () =>
          HttpResponse.json({ documentId: 'doc-new' }),
        ),
        http.delete('*/api/v1/medical-records/documents/*', () =>
          new HttpResponse(null, { status: 204 }),
        ),
      ],
    },
  },
};
