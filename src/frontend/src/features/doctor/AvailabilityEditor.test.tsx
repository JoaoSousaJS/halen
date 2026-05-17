import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import AvailabilityEditor from './AvailabilityEditor';
import type { AvailabilityWindow } from '../../shared/api/availability';

const mockGetMyAvailability = vi.fn();
const mockSetMyAvailability = vi.fn();

vi.mock('../../shared/api/availability', () => ({
  getMyAvailability: (...args: unknown[]) => mockGetMyAvailability(...args),
  setMyAvailability: (...args: unknown[]) => mockSetMyAvailability(...args),
}));

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

function makeWindow(overrides: Partial<AvailabilityWindow> = {}): AvailabilityWindow {
  return {
    id: crypto.randomUUID(),
    dayOfWeek: 'Monday',
    startTime: '09:00',
    endTime: '12:00',
    slotDurationMinutes: 30,
    ...overrides,
  };
}

function renderEditor() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <AvailabilityEditor />
    </QueryClientProvider>,
  );
}

describe('AvailabilityEditor', () => {
  beforeEach(() => {
    mockGetMyAvailability.mockReset();
    mockSetMyAvailability.mockReset();
  });

  it('renders day cards for all 7 days of the week', async () => {
    mockGetMyAvailability.mockResolvedValue([]);
    renderEditor();

    for (const day of DAYS) {
      expect(await screen.findByText(day)).toBeDefined();
    }
  });

  it('displays existing availability windows as chips when data loads', async () => {
    mockGetMyAvailability.mockResolvedValue([
      makeWindow({ dayOfWeek: 'Monday', startTime: '09:00', endTime: '12:00' }),
      makeWindow({ dayOfWeek: 'Wednesday', startTime: '14:00', endTime: '17:00' }),
    ]);
    renderEditor();

    expect(await screen.findByText('09:00 - 12:00')).toBeDefined();
    expect(screen.getByText('14:00 - 17:00')).toBeDefined();
  });

  it('shows "No windows" for days without availability', async () => {
    mockGetMyAvailability.mockResolvedValue([]);
    renderEditor();

    await screen.findByText('Monday');
    const noWindowsElements = screen.getAllByText('No windows');
    expect(noWindowsElements.length).toBe(7);
  });

  it('Add window button shows inline form with time inputs', async () => {
    const user = userEvent.setup();
    mockGetMyAvailability.mockResolvedValue([]);
    renderEditor();

    await screen.findByText('Monday');
    const addButtons = screen.getAllByText('+ Add window');
    expect(addButtons.length).toBe(7);

    await user.click(addButtons[0]);

    expect(screen.getByText('Start')).toBeDefined();
    expect(screen.getByText('End')).toBeDefined();
    expect(screen.getByText('Add')).toBeDefined();
    expect(screen.getByText('Cancel')).toBeDefined();
  });

  it('Cancel button hides the inline form', async () => {
    const user = userEvent.setup();
    mockGetMyAvailability.mockResolvedValue([]);
    renderEditor();

    await screen.findByText('Monday');
    const addButtons = screen.getAllByText('+ Add window');
    await user.click(addButtons[0]);

    expect(screen.getByText('Start')).toBeDefined();

    await user.click(screen.getByText('Cancel'));

    expect(screen.queryByText('Start')).toBeNull();
  });

  it('Save button calls setMyAvailability with the correct slots', async () => {
    const user = userEvent.setup();
    mockGetMyAvailability.mockResolvedValue([
      makeWindow({ dayOfWeek: 'Tuesday', startTime: '10:00', endTime: '11:00' }),
    ]);
    mockSetMyAvailability.mockResolvedValue(undefined);
    renderEditor();

    await screen.findByText('10:00 - 11:00');

    await user.click(screen.getByText('Save availability'));

    await waitFor(() => {
      expect(mockSetMyAvailability).toHaveBeenCalledTimes(1);
      const slots = mockSetMyAvailability.mock.calls[0][0];
      expect(slots).toEqual([
        { dayOfWeek: 'Tuesday', startTime: '10:00', endTime: '11:00' },
      ]);
    });
  });

  it('adding a window and saving includes it in the payload', async () => {
    const user = userEvent.setup();
    mockGetMyAvailability.mockResolvedValue([]);
    mockSetMyAvailability.mockResolvedValue(undefined);
    renderEditor();

    await screen.findByText('Monday');
    const addButtons = screen.getAllByText('+ Add window');
    // Click the first "+ Add window" (Monday)
    await user.click(addButtons[0]);

    const startInput = screen.getByLabelText('Start') as HTMLInputElement;
    const endInput = screen.getByLabelText('End') as HTMLInputElement;

    // fireEvent.change works well for time inputs
    fireEvent.change(startInput, { target: { value: '08:00' } });
    fireEvent.change(endInput, { target: { value: '12:00' } });

    await user.click(screen.getByText('Add'));

    // The chip should now be visible
    expect(screen.getByText('08:00 - 12:00')).toBeDefined();

    // Now save
    await user.click(screen.getByText('Save availability'));

    await waitFor(() => {
      expect(mockSetMyAvailability).toHaveBeenCalledTimes(1);
      const slots = mockSetMyAvailability.mock.calls[0][0];
      expect(slots).toContainEqual({
        dayOfWeek: 'Monday',
        startTime: '08:00',
        endTime: '12:00',
      });
    });
  });

  it('removing a window updates the local state', async () => {
    const user = userEvent.setup();
    mockGetMyAvailability.mockResolvedValue([
      makeWindow({ dayOfWeek: 'Monday', startTime: '09:00', endTime: '12:00' }),
      makeWindow({ dayOfWeek: 'Monday', startTime: '14:00', endTime: '17:00' }),
    ]);
    renderEditor();

    expect(await screen.findByText('09:00 - 12:00')).toBeDefined();
    expect(screen.getByText('14:00 - 17:00')).toBeDefined();

    // Click the remove button for the first window
    const removeButton = screen.getByRole('button', {
      name: 'Remove 09:00 - 12:00 on Monday',
    });
    await user.click(removeButton);

    // First window should be gone
    expect(screen.queryByText('09:00 - 12:00')).toBeNull();
    // Second window should remain
    expect(screen.getByText('14:00 - 17:00')).toBeDefined();
  });

  it('shows validation error when start time is after end time', async () => {
    const user = userEvent.setup();
    mockGetMyAvailability.mockResolvedValue([]);
    renderEditor();

    await screen.findByText('Monday');
    const addButtons = screen.getAllByText('+ Add window');
    await user.click(addButtons[0]);

    const startInput = screen.getByLabelText('Start') as HTMLInputElement;
    const endInput = screen.getByLabelText('End') as HTMLInputElement;

    fireEvent.change(startInput, { target: { value: '14:00' } });
    fireEvent.change(endInput, { target: { value: '10:00' } });

    await user.click(screen.getByText('Add'));

    expect(screen.getByText('Start time must be before end time.')).toBeDefined();
  });

  it('shows loading state', () => {
    mockGetMyAvailability.mockReturnValue(new Promise(() => {}));
    renderEditor();
    expect(screen.getByText('Loading availability...')).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    mockGetMyAvailability.mockRejectedValue(new Error('fail'));
    renderEditor();
    expect(await screen.findByText('Failed to load availability.')).toBeDefined();
  });
});
