import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import PatientSnapshot from './PatientSnapshot';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientSnapshot = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientSnapshot: (...args: unknown[]) => mockGetPatientSnapshot(...args),
}));

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

function renderSnapshot(patientProfileId = 'profile-1') {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PatientSnapshot patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeSnapshot(overrides: Record<string, unknown> = {}) {
  return {
    activeConditions: [
      { id: 'c1', icdDescription: 'Hypertension', severity: 'Moderate' },
      { id: 'c2', icdDescription: 'Diabetes Type 2', severity: 'Mild' },
    ],
    allergies: [
      { id: 'a1', allergenName: 'Penicillin', reaction: null, severity: 'Severe' },
    ],
    activeMedications: [
      { id: 'm1', medicationName: 'Lisinopril', dosage: '10mg daily', frequency: 'Once daily', startDate: null },
    ],
    familyHistory: [
      { id: 'f1', conditionName: 'Heart disease', relationship: 'Father' },
    ],
    latestVitals: {
      bloodPressure: { value: 120, secondaryValue: 80, unit: 'mmHg', measuredAt: '2026-05-10T08:00:00Z' },
      heartRate: { value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2026-05-10T08:00:00Z' },
      weight: null,
      spO2: null,
    },
    onboardingProgress: 4,
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('PatientSnapshot', () => {
  beforeEach(() => {
    mockGetPatientSnapshot.mockReset();
  });

  it('shows loading state', () => {
    mockGetPatientSnapshot.mockReturnValue(new Promise(() => {}));
    renderSnapshot();

    expect(screen.getByRole('status')).toBeDefined();
  });

  it('renders snapshot cards with data', async () => {
    mockGetPatientSnapshot.mockResolvedValue(makeSnapshot());
    renderSnapshot();

    // Active Conditions card
    expect(await screen.findByText('Active Conditions')).toBeDefined();
    expect(screen.getByText('Hypertension')).toBeDefined();
    expect(screen.getByText('Diabetes Type 2')).toBeDefined();

    // Allergies card
    expect(screen.getByText('Allergies')).toBeDefined();
    expect(screen.getByText('Penicillin')).toBeDefined();

    // Current Medications card
    expect(screen.getByText('Current Medications')).toBeDefined();
    expect(screen.getByText('Lisinopril')).toBeDefined();

    // Family History card
    expect(screen.getByText('Family History')).toBeDefined();
    expect(screen.getByText('Heart disease')).toBeDefined();

    // Latest Vitals card
    expect(screen.getByText('Latest Vitals')).toBeDefined();
    expect(screen.getByText('120/80')).toBeDefined();
  });

  it('shows onboarding progress', async () => {
    mockGetPatientSnapshot.mockResolvedValue(makeSnapshot({ onboardingProgress: 4 }));
    renderSnapshot();

    expect(await screen.findByText(/4 of 6/i)).toBeDefined();
    expect(screen.getByRole('progressbar')).toBeDefined();
  });

  it('shows full onboarding progress when complete', async () => {
    mockGetPatientSnapshot.mockResolvedValue(makeSnapshot({ onboardingProgress: 6 }));
    renderSnapshot();

    expect(await screen.findByText(/6 of 6/i)).toBeDefined();
  });

  it('handles empty state for all sections', async () => {
    mockGetPatientSnapshot.mockResolvedValue(
      makeSnapshot({
        activeConditions: [],
        allergies: [],
        activeMedications: [],
        familyHistory: [],
        latestVitals: null,
        onboardingProgress: 0,
      }),
    );
    renderSnapshot();

    // Each empty card should show "Get started" messaging
    const emptyMessages = await screen.findAllByText(/get started/i);
    expect(emptyMessages.length).toBeGreaterThanOrEqual(1);
  });
});
