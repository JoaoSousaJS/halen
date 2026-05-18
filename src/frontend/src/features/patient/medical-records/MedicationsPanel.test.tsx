import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import MedicationsPanel from './MedicationsPanel';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientMedications = vi.fn();
const mockAddMedication = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientMedications: (...args: unknown[]) => mockGetPatientMedications(...args),
  addMedication: (...args: unknown[]) => mockAddMedication(...args),
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
      <MedicationsPanel patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeMedication(overrides: Record<string, unknown> = {}) {
  return {
    id: 'med-1',
    medicationName: 'Lisinopril',
    dosage: '10mg',
    frequency: 'Once daily',
    startDate: '2026-01-15T00:00:00Z',
    endDate: null,
    prescribedByName: 'Dr. Santos',
    linkedPrescriptionId: null,
    addedBy: 'Dr. Santos',
    createdAt: '2026-01-15T00:00:00Z',
    isActive: true,
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('MedicationsPanel', () => {
  beforeEach(() => {
    mockGetPatientMedications.mockReset();
    mockAddMedication.mockReset();
    mockGetPatientMedications.mockResolvedValue([]);
  });

  it('shows loading state', () => {
    mockGetPatientMedications.mockReturnValue(new Promise(() => {}));
    renderPanel();

    expect(screen.getByRole('status')).toBeDefined();
    expect(screen.getByText(/Loading medications/i)).toBeDefined();
  });

  it('renders empty state when no medications exist', async () => {
    renderPanel();

    expect(await screen.findByText(/No medications recorded/i)).toBeDefined();
  });

  it('renders medications list with details', async () => {
    mockGetPatientMedications.mockResolvedValue([
      makeMedication(),
      makeMedication({
        id: 'med-2',
        medicationName: 'Metformin',
        dosage: '500mg',
        frequency: 'Twice daily',
        isActive: false,
        endDate: '2026-03-01T00:00:00Z',
        prescribedByName: null,
      }),
    ]);

    renderPanel();

    // First medication
    expect(await screen.findByText('Lisinopril')).toBeDefined();
    expect(screen.getByText('Dosage: 10mg')).toBeDefined();
    expect(screen.getByText('Frequency: Once daily')).toBeDefined();
    expect(screen.getByText('Prescribed by: Dr. Santos')).toBeDefined();
    expect(screen.getByText('Active')).toBeDefined();

    // Second medication
    expect(screen.getByText('Metformin')).toBeDefined();
    expect(screen.getByText('Dosage: 500mg')).toBeDefined();
    expect(screen.getByText('Inactive')).toBeDefined();
  });

  it('shows add medication dialog when button is clicked', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No medications recorded/i);

    const addBtn = screen.getByRole('button', { name: /Add Medication/i });
    await user.click(addBtn);

    expect(screen.getByRole('form', { name: /Add medication form/i })).toBeDefined();
    expect(screen.getByText('Medication Name')).toBeDefined();
    expect(screen.getByText('Dosage')).toBeDefined();
    expect(screen.getByText('Frequency')).toBeDefined();
    expect(screen.getByText('Start Date')).toBeDefined();
    expect(screen.getByText('End Date')).toBeDefined();
    expect(screen.getByText('Prescribed By')).toBeDefined();
  });

  it('submits add medication form', async () => {
    mockAddMedication.mockResolvedValue({ id: 'med-new' });
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No medications recorded/i);

    // Open dialog
    await user.click(screen.getByRole('button', { name: /Add Medication/i }));

    // Fill form
    await user.type(screen.getByPlaceholderText(/Lisinopril/i), 'Amoxicillin');
    await user.type(screen.getByPlaceholderText(/10mg/i), '500mg');
    await user.type(screen.getByPlaceholderText(/Once daily/i), 'Three times daily');
    await user.type(screen.getByLabelText(/Start Date/i), '2026-05-01');

    // Submit
    await user.click(screen.getByRole('button', { name: /Save Medication/i }));

    await waitFor(() => {
      expect(mockAddMedication).toHaveBeenCalledWith('profile-1', {
        medicationName: 'Amoxicillin',
        dosage: '500mg',
        frequency: 'Three times daily',
        startDate: '2026-05-01',
        endDate: undefined,
        prescribedByName: undefined,
      });
    });
  });

  it('closes dialog on cancel', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No medications recorded/i);

    await user.click(screen.getByRole('button', { name: /Add Medication/i }));
    expect(screen.getByRole('form', { name: /Add medication form/i })).toBeDefined();

    await user.click(screen.getByRole('button', { name: /Cancel/i }));

    expect(screen.queryByRole('form', { name: /Add medication form/i })).toBeNull();
  });

  it('passes patientProfileId to the API call', async () => {
    renderPanel('custom-id');

    await waitFor(() => {
      expect(mockGetPatientMedications).toHaveBeenCalledWith('custom-id');
    });
  });
});
