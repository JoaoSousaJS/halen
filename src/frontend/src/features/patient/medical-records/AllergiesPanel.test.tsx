import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import AllergiesPanel from './AllergiesPanel';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientAllergies = vi.fn();
const mockAddAllergy = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientAllergies: (...args: unknown[]) => mockGetPatientAllergies(...args),
  addAllergy: (...args: unknown[]) => mockAddAllergy(...args),
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
      <AllergiesPanel patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeAllergy(overrides: Record<string, unknown> = {}) {
  return {
    id: 'allergy-1',
    allergenName: 'Penicillin',
    reaction: 'Anaphylaxis',
    severity: 'Severe',
    isActive: true,
    dateIdentified: '2023-06-10',
    addedBy: 'Dr. Santos',
    createdAt: '2023-06-10T10:00:00Z',
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('AllergiesPanel', () => {
  beforeEach(() => {
    mockGetPatientAllergies.mockReset();
    mockAddAllergy.mockReset();
    mockGetPatientAllergies.mockResolvedValue([]);
  });

  it('shows loading state', () => {
    mockGetPatientAllergies.mockReturnValue(new Promise(() => {}));
    renderPanel();

    expect(screen.getByRole('status')).toBeDefined();
  });

  it('renders allergies list', async () => {
    mockGetPatientAllergies.mockResolvedValue([
      makeAllergy(),
      makeAllergy({
        id: 'allergy-2',
        allergenName: 'Peanuts',
        reaction: 'Hives',
        severity: 'Moderate',
        isActive: true,
      }),
    ]);

    renderPanel();

    expect(await screen.findByText('Penicillin')).toBeDefined();
    expect(screen.getByText('Anaphylaxis')).toBeDefined();
    expect(screen.getByText('Peanuts')).toBeDefined();
    expect(screen.getByText('Hives')).toBeDefined();
  });

  it('shows severity badges', async () => {
    mockGetPatientAllergies.mockResolvedValue([makeAllergy()]);
    renderPanel();

    expect(await screen.findByText('Severe')).toBeDefined();
    expect(screen.getByText('Active')).toBeDefined();
  });

  it('shows empty state', async () => {
    renderPanel();

    expect(await screen.findByText(/no allergies/i)).toBeDefined();
  });

  it('opens add form and submits', async () => {
    const user = userEvent.setup();
    mockAddAllergy.mockResolvedValue({ allergyId: 'new-allergy' });

    renderPanel();
    await screen.findByText(/no allergies/i);

    await user.click(screen.getByRole('button', { name: /add allergy/i }));

    await user.type(screen.getByLabelText(/allergen name/i), 'Latex');

    await user.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(mockAddAllergy).toHaveBeenCalledWith(
        'profile-1',
        expect.objectContaining({
          allergenName: 'Latex',
        }),
      );
    });
  });

  it('validates allergen name is required', async () => {
    const user = userEvent.setup();
    renderPanel();
    await screen.findByText(/no allergies/i);

    await user.click(screen.getByRole('button', { name: /add allergy/i }));

    // Submit via fireEvent.submit to bypass HTML5 required constraint validation
    // so the component's custom validate() function runs
    const form = screen.getByRole('form', { name: /add allergy form/i });
    fireEvent.submit(form);

    await waitFor(() => {
      expect(screen.getByText(/allergen name is required/i)).toBeDefined();
    });
  });
});
