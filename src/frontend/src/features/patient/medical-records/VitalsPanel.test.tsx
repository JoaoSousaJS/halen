import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import VitalsPanel from './VitalsPanel';

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetPatientSnapshot = vi.fn();
const mockGetPatientVitalsHistory = vi.fn();
const mockAddVital = vi.fn();

vi.mock('../../../shared/api/medical-records', () => ({
  getPatientSnapshot: (...args: unknown[]) => mockGetPatientSnapshot(...args),
  getPatientVitalsHistory: (...args: unknown[]) => mockGetPatientVitalsHistory(...args),
  addVital: (...args: unknown[]) => mockAddVital(...args),
}));

// Mock recharts to avoid rendering issues in test environment
vi.mock('recharts', () => ({
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: () => null,
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: () => null,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
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
      <VitalsPanel patientProfileId={patientProfileId} />
    </QueryClientProvider>,
  );
}

const defaultSnapshot = {
  activeConditions: [],
  allergies: [],
  activeMedications: [],
  familyHistory: [],
  latestVitals: {
    bloodPressure: { value: 120, secondaryValue: 80, unit: 'mmHg', measuredAt: '2026-05-10T08:00:00Z' },
    heartRate: { value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2026-05-10T08:00:00Z' },
    weight: { value: 75, secondaryValue: null, unit: 'kg', measuredAt: '2026-05-10T08:00:00Z' },
    spO2: null,
  },
  onboardingProgress: 3,
};

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('VitalsPanel', () => {
  beforeEach(() => {
    mockGetPatientSnapshot.mockReset();
    mockGetPatientVitalsHistory.mockReset();
    mockAddVital.mockReset();
    mockGetPatientSnapshot.mockResolvedValue(defaultSnapshot);
    mockGetPatientVitalsHistory.mockResolvedValue([]);
  });

  it('shows loading state', () => {
    mockGetPatientSnapshot.mockReturnValue(new Promise(() => {}));
    renderPanel();

    expect(screen.getByRole('status')).toBeDefined();
  });

  it('renders latest vitals summary', async () => {
    renderPanel();

    expect(await screen.findByText('120/80')).toBeDefined();
    expect(screen.getByText('mmHg')).toBeDefined();
    expect(screen.getByText('72')).toBeDefined();
    expect(screen.getByText('bpm')).toBeDefined();
  });

  it('renders vital type selector', async () => {
    renderPanel();

    await screen.findByText('120/80');

    const typeSelector = screen.getByRole('combobox', { name: /vital type/i });
    expect(typeSelector).toBeDefined();
  });

  it('can select a vital type and see history', async () => {
    const user = userEvent.setup();
    mockGetPatientVitalsHistory.mockResolvedValue([
      { id: 'v1', value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2026-05-10T08:00:00Z', source: 'Manual', notes: null, addedBy: 'Self' },
      { id: 'v2', value: 68, secondaryValue: null, unit: 'bpm', measuredAt: '2026-05-09T08:00:00Z', source: 'Manual', notes: null, addedBy: 'Self' },
    ]);

    renderPanel();
    await screen.findByText('120/80');

    const typeSelector = screen.getByRole('combobox', { name: /vital type/i });
    await user.selectOptions(typeSelector, 'HeartRate');

    await waitFor(() => {
      expect(mockGetPatientVitalsHistory).toHaveBeenCalledWith(
        'profile-1',
        'HeartRate',
      );
    });
  });

  it('shows add vital form', async () => {
    const user = userEvent.setup();
    renderPanel();
    await screen.findByText('120/80');

    await user.click(screen.getByRole('button', { name: /add vital/i }));

    // Use exact match to avoid also matching "Diastolic value"
    expect(screen.getByLabelText('Value')).toBeDefined();
  });

  it('submits add vital form', async () => {
    const user = userEvent.setup();
    mockAddVital.mockResolvedValue({ vitalId: 'new-vital' });

    renderPanel();
    await screen.findByText('120/80');

    await user.click(screen.getByRole('button', { name: /add vital/i }));

    // Select type
    const typeField = screen.getByLabelText(/^type$/i);
    await user.selectOptions(typeField, 'HeartRate');

    const valueField = screen.getByLabelText(/value/i);
    await user.type(valueField, '75');

    await user.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(mockAddVital).toHaveBeenCalledWith(
        'profile-1',
        expect.objectContaining({
          vitalType: 'HeartRate',
          value: 75,
        }),
      );
    });
  });
});
