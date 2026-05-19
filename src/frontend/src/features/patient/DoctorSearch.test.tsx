import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import DoctorSearch from './DoctorSearch';
import type { DoctorSearchDto } from '../../shared/api/doctors';

const mockSearchDoctors = vi.fn();
const mockListSpecialties = vi.fn();

vi.mock('../../shared/api/doctors', () => ({
  searchDoctors: (...args: unknown[]) => mockSearchDoctors(...args),
  listSpecialties: (...args: unknown[]) => mockListSpecialties(...args),
}));

function makeDoctor(overrides: Partial<DoctorSearchDto> = {}): DoctorSearchDto {
  return {
    id: crypto.randomUUID(),
    name: 'Dr. Silva',
    specialty: 'Cardiology',
    consultationFee: 150,
    yearsOfExperience: 10,
    languages: ['English', 'Portuguese'],
    nextAvailableSlot: null,
    ...overrides,
  };
}

function renderSearch(onSelect = vi.fn()) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <DoctorSearch onSelect={onSelect} />
    </QueryClientProvider>,
  );
}

describe('DoctorSearch', () => {
  beforeEach(() => {
    mockSearchDoctors.mockReset();
    mockListSpecialties.mockReset();
    mockListSpecialties.mockResolvedValue(['Cardiology', 'Dermatology', 'Neurology']);
  });

  it('renders search input', () => {
    mockSearchDoctors.mockResolvedValue({ doctors: [], totalCount: 0 });
    renderSearch();

    expect(screen.getByPlaceholderText('Search doctors...')).toBeDefined();
  });

  it('renders filter controls', async () => {
    mockSearchDoctors.mockResolvedValue({ doctors: [], totalCount: 0 });
    renderSearch();

    // Wait for specialties to load — now rendered as pill buttons
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /all specialties/i })).toBeDefined();
    });
    expect(screen.getByRole('button', { name: /sort by/i })).toBeDefined();
  });

  it('shows loading state', () => {
    mockSearchDoctors.mockReturnValue(new Promise(() => {}));
    renderSearch();

    expect(screen.getByText('Loading doctors...')).toBeDefined();
  });

  it('renders doctor cards from search results', async () => {
    const doctors = [
      makeDoctor({ name: 'Dr. Almeida' }),
      makeDoctor({ name: 'Dr. Santos' }),
    ];
    mockSearchDoctors.mockResolvedValue({ doctors, totalCount: 2 });
    renderSearch();

    expect(await screen.findByText('Dr. Almeida')).toBeDefined();
    expect(screen.getByText('Dr. Santos')).toBeDefined();
  });

  it('calls onSelect when a doctor card is clicked', async () => {
    const user = userEvent.setup();
    const doctor = makeDoctor({ name: 'Dr. Costa' });
    mockSearchDoctors.mockResolvedValue({ doctors: [doctor], totalCount: 1 });
    const onSelect = vi.fn();
    renderSearch(onSelect);

    await screen.findByText('Dr. Costa');
    await user.click(screen.getByRole('button', { name: 'Select Dr. Costa' }));

    expect(onSelect).toHaveBeenCalledTimes(1);
    expect(onSelect).toHaveBeenCalledWith(doctor);
  });

  it('shows empty state when no results', async () => {
    mockSearchDoctors.mockResolvedValue({ doctors: [], totalCount: 0 });
    renderSearch();

    expect(await screen.findByText('No doctors match your filters')).toBeDefined();
  });

  it('debounces search input', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    mockSearchDoctors.mockResolvedValue({ doctors: [], totalCount: 0 });
    renderSearch();

    // Wait for initial query to fire
    await vi.advanceTimersByTimeAsync(10);

    const input = screen.getByPlaceholderText('Search doctors...');
    await user.type(input, 'cardio');

    // Should not have called searchDoctors with the typed term yet
    const callsAfterType = mockSearchDoctors.mock.calls.filter(
      (call: unknown[]) => (call[0] as Record<string, unknown>)?.search === 'cardio',
    );
    expect(callsAfterType.length).toBe(0);

    // Advance past debounce
    await vi.advanceTimersByTimeAsync(350);

    await waitFor(() => {
      const callsWithSearch = mockSearchDoctors.mock.calls.filter(
        (call: unknown[]) => (call[0] as Record<string, unknown>)?.search === 'cardio',
      );
      expect(callsWithSearch.length).toBeGreaterThanOrEqual(1);
    });

    vi.useRealTimers();
  });
});
