import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import React from 'react';
import ConditionsPanel from './ConditionsPanel';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientConditions = vi.fn();
const mockAddCondition = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientConditions: (...args: unknown[]) => mockGetPatientConditions(...args),
  addCondition: (...args: unknown[]) => mockAddCondition(...args),
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
      <ConditionsPanel patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeCondition(overrides: Record<string, unknown> = {}) {
  return {
    id: 'cond-1',
    icdCode: 'I10',
    icdDescription: 'Essential hypertension',
    severity: 'Moderate',
    status: 'Active',
    dateOfOnset: '2024-01-15',
    clinicalNotes: 'Monitor blood pressure regularly',
    addedBy: 'Dr. Santos',
    createdAt: '2024-01-15T10:00:00Z',
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('ConditionsPanel', () => {
  beforeEach(() => {
    mockGetPatientConditions.mockReset();
    mockAddCondition.mockReset();
    mockGetPatientConditions.mockResolvedValue([]);
  });

  it('shows loading state', () => {
    mockGetPatientConditions.mockReturnValue(new Promise(() => {}));
    renderPanel();

    expect(screen.getByRole('status')).toBeDefined();
  });

  it('renders list of conditions', async () => {
    mockGetPatientConditions.mockResolvedValue([
      makeCondition(),
      makeCondition({
        id: 'cond-2',
        icdCode: 'E11',
        icdDescription: 'Type 2 diabetes mellitus',
        severity: 'Mild',
        status: 'Active',
      }),
    ]);

    renderPanel();

    expect(await screen.findByText('I10')).toBeDefined();
    expect(screen.getByText('Essential hypertension')).toBeDefined();
    expect(screen.getByText('E11')).toBeDefined();
    expect(screen.getByText('Type 2 diabetes mellitus')).toBeDefined();
  });

  it('shows severity and status badges', async () => {
    mockGetPatientConditions.mockResolvedValue([makeCondition()]);
    renderPanel();

    expect(await screen.findByText('Moderate')).toBeDefined();
    expect(screen.getByText('Active')).toBeDefined();
  });

  it('shows clinical notes', async () => {
    mockGetPatientConditions.mockResolvedValue([makeCondition()]);
    renderPanel();

    expect(await screen.findByText('Monitor blood pressure regularly')).toBeDefined();
  });

  it('shows empty state when no conditions', async () => {
    mockGetPatientConditions.mockResolvedValue([]);
    renderPanel();

    expect(await screen.findByText(/no conditions/i)).toBeDefined();
  });

  it('opens add form when clicking Add Condition', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/no conditions/i);

    const addButton = screen.getByRole('button', { name: /add condition/i });
    await user.click(addButton);

    expect(screen.getByLabelText(/icd code/i)).toBeDefined();
    expect(screen.getByLabelText(/description/i)).toBeDefined();
  });

  it('validates required fields on submit', async () => {
    const user = userEvent.setup();
    renderPanel();

    await screen.findByText(/no conditions/i);

    await user.click(screen.getByRole('button', { name: /add condition/i }));

    // Submit via fireEvent.submit to bypass HTML5 required constraint validation
    // so the component's custom validate() function runs
    const form = screen.getByRole('form', { name: /add condition form/i });
    fireEvent.submit(form);

    // ICD Code and Description are required -- form should show validation
    await waitFor(() => {
      expect(screen.getByText(/icd code is required/i)).toBeDefined();
    });
  });

  it('submits add condition form', async () => {
    const user = userEvent.setup();
    mockAddCondition.mockResolvedValue({ conditionId: 'new-cond' });

    renderPanel();
    await screen.findByText(/no conditions/i);

    await user.click(screen.getByRole('button', { name: /add condition/i }));

    await user.type(screen.getByLabelText(/icd code/i), 'J45');
    await user.type(screen.getByLabelText(/description/i), 'Asthma');

    await user.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(mockAddCondition).toHaveBeenCalledWith(
        'profile-1',
        expect.objectContaining({
          icdCode: 'J45',
          icdDescription: 'Asthma',
        }),
      );
    });
  });
});
