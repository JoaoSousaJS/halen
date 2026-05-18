import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import DoctorMyReviews from './DoctorMyReviews';

const mockGetMyReviews = vi.fn();
const mockRespondToReview = vi.fn();

vi.mock('../../shared/api/reviews', () => ({
  getMyReviews: (...args: unknown[]) => mockGetMyReviews(...args),
  respondToReview: (...args: unknown[]) => mockRespondToReview(...args),
}));

const mockMyReviews = {
  reviews: [
    {
      id: 'r1',
      rating: 5,
      title: 'Great doctor',
      body: 'Very helpful',
      tags: ['listens'],
      postedAs: 'Maya C.',
      helpfulCount: 2,
      moderationStatus: 'Approved',
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-17T10:00:00Z',
    },
    {
      id: 'r2',
      rating: 2,
      title: 'Not satisfied',
      body: 'Too short',
      tags: [],
      postedAs: 'John D.',
      helpfulCount: 0,
      moderationStatus: 'Approved',
      doctorResponse: 'I apologize',
      doctorRespondedAt: '2026-05-18T10:00:00Z',
      createdAt: '2026-05-16T10:00:00Z',
    },
  ],
  totalCount: 2,
  averageRating: 3.5,
  reviewCount: 2,
};

function renderMyReviews() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <DoctorMyReviews />
    </QueryClientProvider>,
  );
}

describe('DoctorMyReviews', () => {
  beforeEach(() => {
    mockGetMyReviews.mockReset();
    mockRespondToReview.mockReset();
    mockGetMyReviews.mockResolvedValue(mockMyReviews);
  });

  it('renders summary row with aggregates', async () => {
    renderMyReviews();

    expect(await screen.findByText('3.5')).toBeDefined();
    expect(screen.getByText('2 reviews')).toBeDefined();
  });

  it('filter tabs switch active filter', async () => {
    const user = userEvent.setup();
    renderMyReviews();

    await screen.findByText('3.5');

    // Click "Awaiting reply" tab
    await user.click(screen.getByRole('button', { name: 'Awaiting reply' }));

    await waitFor(() => {
      const calls = mockGetMyReviews.mock.calls;
      const lastCall = calls[calls.length - 1];
      expect(lastCall[0]).toEqual(
        expect.objectContaining({ filter: 'awaiting-reply' }),
      );
    });
  });

  it('reply button opens composer', async () => {
    const user = userEvent.setup();
    renderMyReviews();

    // Wait for reviews to load — r1 has no doctorResponse so it shows "Reply"
    await screen.findByText('Great doctor');

    // Click Reply on the first review (the one without a response)
    await user.click(screen.getByRole('button', { name: 'Reply' }));

    // Reply textarea should appear
    expect(
      screen.getByPlaceholderText('Write your reply…'),
    ).toBeDefined();
  });

  it('reply submission calls API', async () => {
    const user = userEvent.setup();
    mockRespondToReview.mockResolvedValue(undefined);
    renderMyReviews();

    await screen.findByText('Great doctor');

    // Open reply composer
    await user.click(screen.getByRole('button', { name: 'Reply' }));

    // Type a reply
    const textarea = screen.getByPlaceholderText('Write your reply…');
    await user.type(textarea, 'Thank you for your feedback');

    // Submit
    await user.click(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() => {
      expect(mockRespondToReview).toHaveBeenCalledTimes(1);
      expect(mockRespondToReview).toHaveBeenCalledWith(
        'r1',
        'Thank you for your feedback',
      );
    });
  });

  it('shows moderation status badges', async () => {
    renderMyReviews();

    await screen.findByText('Great doctor');

    // Both reviews have "Approved" status — the Chip component renders the status text
    const approvedChips = screen.getAllByText('Approved');
    expect(approvedChips.length).toBeGreaterThanOrEqual(1);
  });
});
