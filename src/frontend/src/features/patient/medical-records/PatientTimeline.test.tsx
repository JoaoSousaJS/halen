import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import React from 'react';
import PatientTimeline from './PatientTimeline';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientTimeline = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientTimeline: (...args: unknown[]) => mockGetPatientTimeline(...args),
}));

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

function renderTimeline(patientProfileId = 'profile-1') {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PatientTimeline patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

function makeTimelineEntry(overrides: Record<string, unknown> = {}) {
  return {
    id: 'entry-1',
    type: 'Condition',
    occurredAt: '2026-03-15T10:00:00Z',
    title: 'Hypertension diagnosed',
    subtitle: 'Primary hypertension',
    addedBy: 'Dr. Santos',
    ...overrides,
  };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('PatientTimeline', () => {
  beforeEach(() => {
    mockGetPatientTimeline.mockReset();
  });

  it('shows loading state', () => {
    mockGetPatientTimeline.mockReturnValue(new Promise(() => {}));
    renderTimeline();

    expect(screen.getByRole('status')).toBeDefined();
  });

  it('renders timeline entries from API data', async () => {
    mockGetPatientTimeline.mockResolvedValue({
      entries: [
        makeTimelineEntry(),
        makeTimelineEntry({
          id: 'entry-2',
          type: 'Allergy',
          title: 'Penicillin allergy recorded',
          subtitle: 'Severe reaction',
          addedBy: 'Dr. Costa',
          occurredAt: '2026-04-01T14:00:00Z',
        }),
      ],
      totalCount: 2,
    });

    renderTimeline();

    expect(await screen.findByText('Hypertension diagnosed')).toBeDefined();
    expect(screen.getByText('Primary hypertension')).toBeDefined();
    expect(screen.getByText('Dr. Santos')).toBeDefined();

    expect(screen.getByText('Penicillin allergy recorded')).toBeDefined();
    expect(screen.getByText('Severe reaction')).toBeDefined();
    expect(screen.getByText('Dr. Costa')).toBeDefined();
  });

  it('shows empty state when no entries exist', async () => {
    mockGetPatientTimeline.mockResolvedValue({ entries: [], totalCount: 0 });
    renderTimeline();

    expect(await screen.findByText(/no medical events/i)).toBeDefined();
  });

  it('shows type badges on entries', async () => {
    mockGetPatientTimeline.mockResolvedValue({
      entries: [
        makeTimelineEntry({ type: 'Condition' }),
        makeTimelineEntry({ id: 'entry-2', type: 'Allergy', title: 'Allergy added' }),
      ],
      totalCount: 2,
    });

    renderTimeline();

    await screen.findByText('Hypertension diagnosed');
    // Type names appear in both filter checkboxes and Chip badges
    expect(screen.getAllByText('Condition').length).toBeGreaterThanOrEqual(2);
    expect(screen.getAllByText('Allergy').length).toBeGreaterThanOrEqual(2);
  });

  it('supports pagination controls', async () => {
    mockGetPatientTimeline.mockResolvedValue({
      entries: [makeTimelineEntry()],
      totalCount: 25,
    });

    renderTimeline();

    await screen.findByText('Hypertension diagnosed');

    // Should show pagination when totalCount > items displayed
    expect(screen.getByRole('navigation', { name: /pagination/i })).toBeDefined();
  });

  it('renders filter controls for type', async () => {
    mockGetPatientTimeline.mockResolvedValue({
      entries: [makeTimelineEntry()],
      totalCount: 1,
    });

    renderTimeline();

    await screen.findByText('Hypertension diagnosed');

    // Should have filter checkboxes
    expect(screen.getByRole('group', { name: /filter by type/i })).toBeDefined();
  });
});
