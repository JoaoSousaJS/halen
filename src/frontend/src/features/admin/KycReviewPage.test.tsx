import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import KycReviewPage from './KycReviewPage';

const mockGetDoctorKycDetails = vi.fn();
const mockReviewKyc = vi.fn();

vi.mock('../../shared/api/admin', () => ({
  getDoctorKycDetails: (...args: unknown[]) => mockGetDoctorKycDetails(...args),
  reviewKyc: (...args: unknown[]) => mockReviewKyc(...args),
  downloadKycDocument: vi.fn().mockResolvedValue(new Blob(['test'])),
  listUsers: vi.fn(),
  createDoctor: vi.fn(),
}));

const kycDetails = {
  doctorProfileId: 'dp-1',
  doctorName: 'Dr. House',
  specialty: 'Diagnostics',
  licenseNumber: 'LIC-001',
  status: 'Submitted',
  submittedAt: '2026-05-15T10:00:00Z',
  documents: [
    { id: 'doc-1', documentType: 'LicensePhoto', fileName: 'license.jpg', uploadedAt: '2026-05-15T10:00:00Z' },
    { id: 'doc-2', documentType: 'MedicalCertificate', fileName: 'cert.pdf', uploadedAt: '2026-05-15T10:00:00Z' },
  ],
  reviewHistory: [],
};

function renderPage(onBack = vi.fn()) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return {
    onBack,
    ...render(
      <QueryClientProvider client={client}>
        <KycReviewPage doctorProfileId="dp-1" onBack={onBack} />
      </QueryClientProvider>,
    ),
  };
}

describe('KycReviewPage', () => {
  beforeEach(() => {
    mockGetDoctorKycDetails.mockReset();
    mockReviewKyc.mockReset();
  });

  it('shows loading state', () => {
    mockGetDoctorKycDetails.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText('Loading KYC details…')).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    mockGetDoctorKycDetails.mockRejectedValue(new Error('fail'));
    renderPage();
    expect(await screen.findByText('Failed to load KYC details.')).toBeDefined();
  });

  it('renders doctor info and documents', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    renderPage();

    expect(await screen.findByText('Dr. House')).toBeDefined();
    expect(screen.getByText(/Diagnostics · License: LIC-001/)).toBeDefined();
    expect(screen.getByText('LicensePhoto')).toBeDefined();
    expect(screen.getByText('MedicalCertificate')).toBeDefined();
    expect(screen.getByText('license.jpg')).toBeDefined();
    expect(screen.getByText('cert.pdf')).toBeDefined();
  });

  it('renders download buttons for documents', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    renderPage();

    await screen.findByText('Dr. House');
    const buttons = screen.getAllByText('Download');
    expect(buttons).toHaveLength(2);
    expect(buttons[0].tagName).toBe('BUTTON');
    expect(buttons[1].tagName).toBe('BUTTON');
  });

  it('renders approve and reject buttons for Submitted status', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    renderPage();

    expect(await screen.findByText('Approve')).toBeDefined();
    expect(screen.getByText('Reject')).toBeDefined();
  });

  it('calls reviewKyc with Approved on approve click', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    mockReviewKyc.mockResolvedValue({ message: 'OK' });
    const { onBack } = renderPage();

    await screen.findByText('Approve');
    await act(async () => {
      fireEvent.click(screen.getByText('Approve'));
    });

    await waitFor(() => {
      expect(mockReviewKyc).toHaveBeenCalledWith('dp-1', { decision: 'Approved' });
    });
    await waitFor(() => {
      expect(onBack).toHaveBeenCalled();
    });
  });

  it('shows rejection form when Reject is clicked', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    renderPage();

    await screen.findByText('Reject');
    await act(async () => {
      fireEvent.click(screen.getByText('Reject'));
    });

    expect(screen.getByText('Rejection reason')).toBeDefined();
    expect(screen.getByPlaceholderText('Explain why the documents are being rejected…')).toBeDefined();
    expect(screen.getByText('Confirm rejection')).toBeDefined();
    expect(screen.getByText('Cancel')).toBeDefined();
  });

  it('disables Confirm rejection when reason is empty', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    renderPage();

    await screen.findByText('Reject');
    await act(async () => {
      fireEvent.click(screen.getByText('Reject'));
    });

    const confirmBtn = screen.getByText('Confirm rejection');
    expect((confirmBtn as HTMLButtonElement).disabled).toBe(true);
  });

  it('submits rejection with reason', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    mockReviewKyc.mockResolvedValue({ message: 'OK' });
    const { onBack } = renderPage();

    await screen.findByText('Reject');
    await act(async () => {
      fireEvent.click(screen.getByText('Reject'));
    });

    const textarea = screen.getByPlaceholderText('Explain why the documents are being rejected…');
    await act(async () => {
      fireEvent.change(textarea, { target: { value: 'Blurry photo' } });
    });

    await act(async () => {
      fireEvent.click(screen.getByText('Confirm rejection'));
    });

    await waitFor(() => {
      expect(mockReviewKyc).toHaveBeenCalledWith('dp-1', { decision: 'Rejected', rejectionReason: 'Blurry photo' });
    });
    await waitFor(() => {
      expect(onBack).toHaveBeenCalled();
    });
  });

  it('hides rejection form on Cancel', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    renderPage();

    await screen.findByText('Reject');
    await act(async () => {
      fireEvent.click(screen.getByText('Reject'));
    });

    expect(screen.getByText('Rejection reason')).toBeDefined();

    await act(async () => {
      fireEvent.click(screen.getByText('Cancel'));
    });

    expect(screen.queryByText('Rejection reason')).toBeNull();
  });

  it('calls onBack when back button is clicked', async () => {
    mockGetDoctorKycDetails.mockResolvedValue(kycDetails);
    const { onBack } = renderPage();

    await screen.findByText('Dr. House');
    await act(async () => {
      fireEvent.click(screen.getByText('← Back'));
    });

    expect(onBack).toHaveBeenCalled();
  });

  it('does not show review actions when status is Approved', async () => {
    mockGetDoctorKycDetails.mockResolvedValue({ ...kycDetails, status: 'Approved' });
    renderPage();

    await screen.findByText('Dr. House');
    expect(screen.queryByText('Approve')).toBeNull();
    expect(screen.queryByText('Reject')).toBeNull();
  });

  it('renders review history when present', async () => {
    mockGetDoctorKycDetails.mockResolvedValue({
      ...kycDetails,
      status: 'Rejected',
      reviewHistory: [
        { id: 'r-1', decision: 'Rejected', rejectionReason: 'Blurry photo', reviewerName: 'Admin User', reviewedAt: '2026-05-15T12:00:00Z' },
      ],
    });
    renderPage();

    await screen.findByText('Review History');
    expect(screen.getByText('Blurry photo')).toBeDefined();
    expect(screen.getByText(/by Admin User/)).toBeDefined();
  });
});
