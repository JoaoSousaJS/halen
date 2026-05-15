import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import KycSetup from './KycSetup';

const mockGetKycStatus = vi.fn();
const mockSubmitKycDocuments = vi.fn();

vi.mock('../../shared/api/doctor', () => ({
  getKycStatus: (...args: unknown[]) => mockGetKycStatus(...args),
  submitKycDocuments: (...args: unknown[]) => mockSubmitKycDocuments(...args),
}));

function renderSetup() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <KycSetup />
    </QueryClientProvider>,
  );
}

function createFile(name: string): File {
  return new File(['dummy'], name, { type: 'image/jpeg' });
}

describe('KycSetup', () => {
  beforeEach(() => {
    mockGetKycStatus.mockReset();
    mockSubmitKycDocuments.mockReset();
  });

  it('shows loading state', () => {
    mockGetKycStatus.mockReturnValue(new Promise(() => {}));
    renderSetup();
    expect(screen.getByText('Loading KYC status…')).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    mockGetKycStatus.mockRejectedValue(new Error('fail'));
    renderSetup();
    expect(await screen.findByText('Failed to load KYC status.')).toBeDefined();
  });

  it('shows upload form when status is NotSubmitted', async () => {
    mockGetKycStatus.mockResolvedValue({
      status: 'NotSubmitted',
      submittedAt: null,
      lastRejectionReason: null,
      documents: [],
    });
    renderSetup();

    expect(await screen.findByText('Complete your KYC verification')).toBeDefined();
    expect(screen.getByText('License photo')).toBeDefined();
    expect(screen.getByText('Medical certificate')).toBeDefined();
    expect(screen.getByText('Identity proof')).toBeDefined();
    expect(screen.getByText('Submit for review')).toBeDefined();
  });

  it('shows waiting message when status is Submitted', async () => {
    mockGetKycStatus.mockResolvedValue({
      status: 'Submitted',
      submittedAt: '2026-05-15T10:00:00Z',
      lastRejectionReason: null,
      documents: [
        { id: 'd1', documentType: 'LicensePhoto', fileName: 'license.jpg', uploadedAt: '2026-05-15T10:00:00Z' },
      ],
    });
    renderSetup();

    expect(await screen.findByText('KYC documents submitted')).toBeDefined();
    expect(screen.getByText(/Your documents are under review/)).toBeDefined();
    expect(screen.getByText(/LicensePhoto: license.jpg/)).toBeDefined();
  });

  it('shows approved message when status is Approved', async () => {
    mockGetKycStatus.mockResolvedValue({
      status: 'Approved',
      submittedAt: '2026-05-15T10:00:00Z',
      lastRejectionReason: null,
      documents: [],
    });
    renderSetup();

    expect(await screen.findByText('Your KYC documents have been approved.')).toBeDefined();
  });

  it('shows rejection reason and resubmit form when status is Rejected', async () => {
    mockGetKycStatus.mockResolvedValue({
      status: 'Rejected',
      submittedAt: '2026-05-15T10:00:00Z',
      lastRejectionReason: 'Blurry photo',
      documents: [],
    });
    renderSetup();

    expect(await screen.findByText('Complete your KYC verification')).toBeDefined();
    expect(screen.getByText(/Your previous submission was rejected/)).toBeDefined();
    expect(screen.getByText('Blurry photo')).toBeDefined();
    expect(screen.getByText('Submit for review')).toBeDefined();
  });

  it('disables submit button when not all files are selected', async () => {
    mockGetKycStatus.mockResolvedValue({
      status: 'NotSubmitted',
      submittedAt: null,
      lastRejectionReason: null,
      documents: [],
    });
    renderSetup();

    const btn = await screen.findByText('Submit for review');
    expect((btn as HTMLButtonElement).disabled).toBe(true);
  });

  it('submits files and invalidates query on success', async () => {
    mockGetKycStatus.mockResolvedValue({
      status: 'NotSubmitted',
      submittedAt: null,
      lastRejectionReason: null,
      documents: [],
    });
    mockSubmitKycDocuments.mockResolvedValue({ message: 'OK' });
    renderSetup();

    await screen.findByText('Submit for review');
    const fileInputs = document.querySelectorAll('input[type="file"]');

    await act(async () => {
      fireEvent.change(fileInputs[0], { target: { files: [createFile('license.jpg')] } });
      fireEvent.change(fileInputs[1], { target: { files: [createFile('cert.pdf')] } });
      fireEvent.change(fileInputs[2], { target: { files: [createFile('id.png')] } });
    });

    expect((screen.getByText('Submit for review') as HTMLButtonElement).disabled).toBe(false);

    const form = document.querySelector('form')!;
    await act(async () => {
      fireEvent.submit(form);
    });

    await waitFor(() => {
      expect(mockSubmitKycDocuments).toHaveBeenCalled();
      const args = mockSubmitKycDocuments.mock.calls[0][0];
      expect(args.licensePhoto).toBeInstanceOf(File);
      expect(args.medicalCertificate).toBeInstanceOf(File);
      expect(args.identityProof).toBeInstanceOf(File);
    });
  });

  it('shows submitting state while mutation is pending', async () => {
    mockGetKycStatus.mockResolvedValue({
      status: 'NotSubmitted',
      submittedAt: null,
      lastRejectionReason: null,
      documents: [],
    });
    mockSubmitKycDocuments.mockReturnValue(new Promise(() => {}));
    renderSetup();

    await screen.findByText('Submit for review');
    const fileInputs = document.querySelectorAll('input[type="file"]');

    await act(async () => {
      fireEvent.change(fileInputs[0], { target: { files: [createFile('license.jpg')] } });
      fireEvent.change(fileInputs[1], { target: { files: [createFile('cert.pdf')] } });
      fireEvent.change(fileInputs[2], { target: { files: [createFile('id.png')] } });
    });

    const form = document.querySelector('form')!;
    await act(async () => {
      fireEvent.submit(form);
    });

    expect(await screen.findByText('Submitting…')).toBeDefined();
  });
});
