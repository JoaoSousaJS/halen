import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import React from 'react';
import DocumentsPanel from './DocumentsPanel';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientDocuments = vi.fn();
const mockUploadDocument = vi.fn();
const mockDownloadDocument = vi.fn();
const mockDeleteDocument = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientDocuments: (...args: unknown[]) => mockGetPatientDocuments(...args),
  uploadDocument: (...args: unknown[]) => mockUploadDocument(...args),
  downloadDocument: (...args: unknown[]) => mockDownloadDocument(...args),
  deleteDocument: (...args: unknown[]) => mockDeleteDocument(...args),
}));

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

function renderPanel(patientProfileId = 'profile-1') {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <DocumentsPanel patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeDocument(overrides: Record<string, unknown> = {}) {
  return {
    id: 'doc-1',
    title: 'Blood Work Results',
    type: 'LabResult',
    fileName: 'bloodwork-2026.pdf',
    fileSize: 245760,
    uploadedBy: 'Dr. Santos',
    uploadedAt: '2026-04-15T10:00:00Z',
    description: null,
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('DocumentsPanel', () => {
  beforeEach(() => {
    mockGetPatientDocuments.mockReset();
    mockUploadDocument.mockReset();
    mockDownloadDocument.mockReset();
    mockDeleteDocument.mockReset();
    mockGetPatientDocuments.mockResolvedValue([]);
  });

  it('shows loading state', () => {
    mockGetPatientDocuments.mockReturnValue(new Promise(() => {}));
    renderPanel();

    expect(screen.getByRole('status')).toBeDefined();
    expect(screen.getByText(/Loading documents/i)).toBeDefined();
  });

  it('renders empty state when no documents exist', async () => {
    renderPanel();

    expect(await screen.findByText(/No documents uploaded/i)).toBeDefined();
  });

  it('renders documents list with details', async () => {
    mockGetPatientDocuments.mockResolvedValue([
      makeDocument(),
      makeDocument({
        id: 'doc-2',
        title: 'X-Ray Report',
        type: 'Imaging',
        fileName: 'xray-chest.pdf',
        fileSize: 1048576,
        uploadedBy: 'Dr. Costa',
        uploadedAt: '2026-05-01T14:00:00Z',
      }),
    ]);

    renderPanel();

    // First document
    expect(await screen.findByText('Blood Work Results')).toBeDefined();
    expect(screen.getByText(/bloodwork-2026\.pdf/)).toBeDefined();
    expect(screen.getByText(/240\.0 KB/)).toBeDefined();
    expect(screen.getByText('Uploaded by: Dr. Santos')).toBeDefined();
    // 'Lab Result' appears in both filter dropdown and Chip
    expect(screen.getAllByText('Lab Result').length).toBeGreaterThanOrEqual(1);

    // Second document
    expect(screen.getByText('X-Ray Report')).toBeDefined();
    expect(screen.getByText(/xray-chest\.pdf/)).toBeDefined();
    expect(screen.getByText(/1\.0 MB/)).toBeDefined();
    // 'Imaging' appears in both filter dropdown and Chip
    expect(screen.getAllByText('Imaging').length).toBeGreaterThanOrEqual(1);
  });

  it('filters documents by type', async () => {
    mockGetPatientDocuments.mockResolvedValue([
      makeDocument(),
      makeDocument({
        id: 'doc-2',
        title: 'X-Ray Report',
        type: 'Imaging',
        fileName: 'xray.pdf',
        fileSize: 512000,
      }),
    ]);

    const user = userEvent.setup();
    renderPanel();

    await screen.findByText('Blood Work Results');

    // Both documents visible
    expect(screen.getByText('Blood Work Results')).toBeDefined();
    expect(screen.getByText('X-Ray Report')).toBeDefined();

    // Filter to only Lab Results
    const filterSelect = screen.getByLabelText(/Filter by document type/i);
    await user.selectOptions(filterSelect, 'LabResult');

    // Only lab result visible
    expect(screen.getByText('Blood Work Results')).toBeDefined();
    expect(screen.queryByText('X-Ray Report')).toBeNull();
  });

  it('shows no match message when filter excludes all', async () => {
    mockGetPatientDocuments.mockResolvedValue([makeDocument()]);

    const user = userEvent.setup();
    renderPanel();

    await screen.findByText('Blood Work Results');

    const filterSelect = screen.getByLabelText(/Filter by document type/i);
    await user.selectOptions(filterSelect, 'Imaging');

    expect(screen.getByText(/No documents match the selected filter/i)).toBeDefined();
  });

  it('shows upload document dialog when button is clicked', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No documents uploaded/i);

    await user.click(screen.getByRole('button', { name: /Upload Document/i }));

    expect(screen.getByRole('form', { name: /Upload document form/i })).toBeDefined();
    expect(screen.getByText('File')).toBeDefined();
    expect(screen.getByText('Document Type')).toBeDefined();
    expect(screen.getByText('Title')).toBeDefined();
    expect(screen.getByText('Description')).toBeDefined();
  });

  it('has download button per document', async () => {
    mockGetPatientDocuments.mockResolvedValue([makeDocument()]);
    renderPanel();

    const downloadBtn = await screen.findByRole('button', { name: /Download Blood Work Results/i });
    expect(downloadBtn).toBeDefined();
  });

  it('has delete button per document', async () => {
    mockGetPatientDocuments.mockResolvedValue([makeDocument()]);
    renderPanel();

    const deleteBtn = await screen.findByRole('button', { name: /Delete Blood Work Results/i });
    expect(deleteBtn).toBeDefined();
  });

  it('calls deleteDocument when delete is clicked', async () => {
    mockGetPatientDocuments.mockResolvedValue([makeDocument()]);
    mockDeleteDocument.mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderPanel();

    const deleteBtn = await screen.findByRole('button', { name: /Delete Blood Work Results/i });
    await user.click(deleteBtn);

    await waitFor(() => {
      expect(mockDeleteDocument).toHaveBeenCalledWith('profile-1', 'doc-1');
    });
  });

  it('closes upload dialog on cancel', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No documents uploaded/i);

    await user.click(screen.getByRole('button', { name: /Upload Document/i }));
    expect(screen.getByRole('form', { name: /Upload document form/i })).toBeDefined();

    await user.click(screen.getByRole('button', { name: /Cancel/i }));

    expect(screen.queryByRole('form', { name: /Upload document form/i })).toBeNull();
  });

  it('passes patientProfileId to the API call', async () => {
    renderPanel('custom-id');

    await waitFor(() => {
      expect(mockGetPatientDocuments).toHaveBeenCalledWith('custom-id');
    });
  });
});
