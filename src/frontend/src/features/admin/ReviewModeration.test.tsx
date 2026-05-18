import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import ReviewModeration from './ReviewModeration';

const mockGetModerationQueue = vi.fn();
const mockModerateReview = vi.fn();

vi.mock('../../shared/api/reviews', () => ({
  getModerationQueue: (...args: unknown[]) => mockGetModerationQueue(...args),
  moderateReview: (...args: unknown[]) => mockModerateReview(...args),
}));

const mockQueue = {
  reviews: [
    {
      id: 'r1',
      rating: 4,
      title: 'Good',
      body: 'Nice doctor',
      tags: ['listens'],
      postedAs: 'Maya C.',
      moderationStatus: 'Pending',
      patientName: 'Maya Carter',
      doctorName: 'Dr. House',
      createdAt: '2026-05-17T10:00:00Z',
    },
  ],
  totalCount: 1,
};

function renderModeration() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <ReviewModeration />
    </QueryClientProvider>,
  );
}

describe('ReviewModeration', () => {
  beforeEach(() => {
    mockGetModerationQueue.mockReset();
    mockModerateReview.mockReset();
    mockGetModerationQueue.mockResolvedValue(mockQueue);
  });

  it('renders moderation queue', async () => {
    renderModeration();

    expect(await screen.findByText('Good')).toBeDefined();
    expect(screen.getByText('Maya Carter')).toBeDefined();
  });

  it('approve button calls moderate API with Approved', async () => {
    const user = userEvent.setup();
    mockModerateReview.mockResolvedValue(undefined);
    renderModeration();

    await screen.findByText('Good');

    await user.click(screen.getByRole('button', { name: 'Approve' }));

    await waitFor(() => {
      expect(mockModerateReview).toHaveBeenCalledTimes(1);
      expect(mockModerateReview).toHaveBeenCalledWith('r1', 'Approved');
    });
  });

  it('hide button calls moderate API with Hidden', async () => {
    const user = userEvent.setup();
    mockModerateReview.mockResolvedValue(undefined);
    renderModeration();

    await screen.findByText('Good');

    await user.click(screen.getByRole('button', { name: 'Hide' }));

    await waitFor(() => {
      expect(mockModerateReview).toHaveBeenCalledTimes(1);
      expect(mockModerateReview).toHaveBeenCalledWith('r1', 'Hidden');
    });
  });

  it('remove button calls moderate API with Removed', async () => {
    const user = userEvent.setup();
    mockModerateReview.mockResolvedValue(undefined);
    renderModeration();

    await screen.findByText('Good');

    await user.click(screen.getByRole('button', { name: 'Remove' }));

    await waitFor(() => {
      expect(mockModerateReview).toHaveBeenCalledTimes(1);
      expect(mockModerateReview).toHaveBeenCalledWith('r1', 'Removed');
    });
  });

  it('filter tabs work', async () => {
    const user = userEvent.setup();
    renderModeration();

    await screen.findByText('Good');

    // Click "All" tab
    await user.click(screen.getByRole('button', { name: 'All' }));

    await waitFor(() => {
      const calls = mockGetModerationQueue.mock.calls;
      const lastCall = calls[calls.length - 1];
      expect(lastCall[0]).toEqual(
        expect.objectContaining({ filter: 'all' }),
      );
    });
  });
});
