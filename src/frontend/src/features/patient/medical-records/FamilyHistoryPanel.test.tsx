import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import FamilyHistoryPanel from './FamilyHistoryPanel';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientFamilyHistory = vi.fn();
const mockAddFamilyHistory = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientFamilyHistory: (...args: unknown[]) => mockGetPatientFamilyHistory(...args),
  addFamilyHistory: (...args: unknown[]) => mockAddFamilyHistory(...args),
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
      <FamilyHistoryPanel patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeFamilyHistoryEntry(overrides: Record<string, unknown> = {}) {
  return {
    id: 'fh-1',
    relationship: 'Mother',
    conditionName: 'Type 2 Diabetes',
    ageAtOnset: 45,
    notes: 'Managed with medication',
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('FamilyHistoryPanel', () => {
  beforeEach(() => {
    mockGetPatientFamilyHistory.mockReset();
    mockAddFamilyHistory.mockReset();
    mockGetPatientFamilyHistory.mockResolvedValue([]);
  });

  it('shows loading state', () => {
    mockGetPatientFamilyHistory.mockReturnValue(new Promise(() => {}));
    renderPanel();

    expect(screen.getByRole('status')).toBeDefined();
    expect(screen.getByText(/Loading family history/i)).toBeDefined();
  });

  it('renders empty state when no entries exist', async () => {
    renderPanel();

    expect(await screen.findByText(/No family history recorded/i)).toBeDefined();
  });

  it('renders family history entries with details', async () => {
    mockGetPatientFamilyHistory.mockResolvedValue([
      makeFamilyHistoryEntry(),
      makeFamilyHistoryEntry({
        id: 'fh-2',
        relationship: 'Father',
        conditionName: 'Hypertension',
        ageAtOnset: null,
        notes: null,
      }),
    ]);

    renderPanel();

    // First entry
    expect(await screen.findByText('Type 2 Diabetes')).toBeDefined();
    expect(screen.getByText('Mother')).toBeDefined();
    expect(screen.getByText('Age at onset: 45')).toBeDefined();
    expect(screen.getByText('Managed with medication')).toBeDefined();

    // Second entry
    expect(screen.getByText('Hypertension')).toBeDefined();
    expect(screen.getByText('Father')).toBeDefined();
  });

  it('shows add entry dialog when button is clicked', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No family history recorded/i);

    await user.click(screen.getByRole('button', { name: /Add Entry/i }));

    expect(screen.getByRole('form', { name: /Add family history form/i })).toBeDefined();
    expect(screen.getByText('Relationship')).toBeDefined();
    expect(screen.getByText('Condition Name')).toBeDefined();
    expect(screen.getByText('Age at Onset')).toBeDefined();
    expect(screen.getByText('Notes')).toBeDefined();
  });

  it('submits add family history form', async () => {
    mockAddFamilyHistory.mockResolvedValue({ id: 'fh-new' });
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No family history recorded/i);

    // Open dialog
    await user.click(screen.getByRole('button', { name: /Add Entry/i }));

    // Fill form
    await user.selectOptions(screen.getByRole('combobox'), 'Father');
    await user.type(screen.getByPlaceholderText(/Type 2 Diabetes/i), 'Heart Disease');
    // Use exact match to avoid also matching "Additional details (optional)"
    await user.type(screen.getByPlaceholderText('Optional'), '55');

    // Submit
    await user.click(screen.getByRole('button', { name: /Save Entry/i }));

    await waitFor(() => {
      expect(mockAddFamilyHistory).toHaveBeenCalledWith('profile-1', {
        relationship: 'Father',
        conditionName: 'Heart Disease',
        ageAtOnset: 55,
        notes: undefined,
      });
    });
  });

  it('closes dialog on cancel', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/No family history recorded/i);

    await user.click(screen.getByRole('button', { name: /Add Entry/i }));
    expect(screen.getByRole('form', { name: /Add family history form/i })).toBeDefined();

    await user.click(screen.getByRole('button', { name: /Cancel/i }));

    expect(screen.queryByRole('form', { name: /Add family history form/i })).toBeNull();
  });

  it('passes patientProfileId to the API call', async () => {
    renderPanel('custom-id');

    await waitFor(() => {
      expect(mockGetPatientFamilyHistory).toHaveBeenCalledWith('custom-id');
    });
  });
});
