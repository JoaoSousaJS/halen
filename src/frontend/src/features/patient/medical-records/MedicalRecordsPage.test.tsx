import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import React from 'react';
import MedicalRecordsPage from './MedicalRecordsPage';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientHeader = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientHeader: (...args: unknown[]) => mockGetPatientHeader(...args),
  getPatientTimeline: () => Promise.resolve({ entries: [], totalCount: 0 }),
  getPatientSnapshot: () =>
    Promise.resolve({
      activeConditions: [],
      allergies: [],
      activeMedications: [],
      familyHistory: [],
      latestVitals: null,
      onboardingProgress: 0,
    }),
  getPatientConditions: () => Promise.resolve([]),
  getPatientAllergies: () => Promise.resolve([]),
  getPatientVitalsHistory: () => Promise.resolve([]),
}));

vi.mock('../../../shared/components/AuthProvider', () => ({
  useAuth: () => ({ user: { given_name: 'Test', family_name: 'Patient' } }),
}));

vi.mock('../../../shared/components/DashboardShell', () => ({
  DashboardShell: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
}));

vi.mock('../../../shared/components/FeatureGate', () => ({
  FeatureGate: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
}));

vi.mock('react-router-dom', () => ({
  useParams: () => ({ patientProfileId: 'profile-1' }),
}));

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

function renderPage(patientProfileId = 'profile-1') {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MedicalRecordsPage patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeHeader(overrides: Record<string, unknown> = {}) {
  return {
    patientProfileId: 'profile-1',
    patientName: 'Maria Silva',
    city: 'Lisbon',
    allergyChips: ['Penicillin', 'Peanuts'],
    conditionChips: ['Hypertension'],
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('MedicalRecordsPage', () => {
  beforeEach(() => {
    mockGetPatientHeader.mockReset();
    mockGetPatientHeader.mockResolvedValue(makeHeader());
  });

  it('shows loading state while header is fetching', () => {
    mockGetPatientHeader.mockReturnValue(new Promise(() => {})); // never resolves
    renderPage();

    expect(screen.getByRole('status')).toBeDefined();
    expect(screen.getByText(/Loading/i)).toBeDefined();
  });

  it('renders patient header with name, city, and chips', async () => {
    renderPage();

    expect(await screen.findByText('Maria Silva')).toBeDefined();
    expect(screen.getByText('Lisbon')).toBeDefined();
    expect(screen.getByText('Penicillin')).toBeDefined();
    expect(screen.getByText('Peanuts')).toBeDefined();
    expect(screen.getByText('Hypertension')).toBeDefined();
  });

  it('renders all tab buttons', async () => {
    renderPage();

    await screen.findByText('Maria Silva');

    const tabNames = [
      'Timeline',
      'Snapshot',
      'Conditions',
      'Allergies',
      'Vitals',
      'Medications',
      'Family History',
      'Documents',
    ];
    for (const name of tabNames) {
      expect(screen.getByRole('tab', { name })).toBeDefined();
    }
  });

  it('defaults to the Timeline tab', async () => {
    renderPage();

    await screen.findByText('Maria Silva');

    const timelineTab = screen.getByRole('tab', { name: 'Timeline' });
    expect(timelineTab.getAttribute('aria-selected')).toBe('true');
  });

  it('can switch tabs', async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Maria Silva');

    const snapshotTab = screen.getByRole('tab', { name: 'Snapshot' });
    await user.click(snapshotTab);

    expect(snapshotTab.getAttribute('aria-selected')).toBe('true');

    const timelineTab = screen.getByRole('tab', { name: 'Timeline' });
    expect(timelineTab.getAttribute('aria-selected')).toBe('false');
  });

  it('passes patientProfileId to the API call', async () => {
    renderPage('custom-profile-id');

    await waitFor(() => {
      expect(mockGetPatientHeader).toHaveBeenCalledWith('custom-profile-id');
    });
  });
});
