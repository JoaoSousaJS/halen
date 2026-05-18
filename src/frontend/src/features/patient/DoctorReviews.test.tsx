import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import DoctorReviews from './DoctorReviews';

const mockGetDoctorReviews = vi.fn();
const mockVoteHelpful = vi.fn();

vi.mock('../../shared/api/reviews', () => ({
  getDoctorReviews: (...args: unknown[]) => mockGetDoctorReviews(...args),
  voteHelpful: (...args: unknown[]) => mockVoteHelpful(...args),
}));

const mockReviewsData = {
  reviews: [
    {
      id: 'r1',
      rating: 5,
      title: 'Excellent',
      body: 'Very thorough',
      tags: ['listens'],
      postedAs: 'Maya C.',
      helpfulCount: 3,
      doctorResponse: 'Thank you!',
      doctorRespondedAt: '2026-05-18T12:00:00Z',
      createdAt: '2026-05-17T10:00:00Z',
    },
    {
      id: 'r2',
      rating: 4,
      title: 'Good visit',
      body: 'Helpful doctor',
      tags: [],
      postedAs: 'John D.',
      helpfulCount: 0,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-16T10:00:00Z',
    },
  ],
  totalCount: 2,
  averageRating: 4.5,
  reviewCount: 2,
  ratingBreakdown: [
    { stars: 5, count: 1 },
    { stars: 4, count: 1 },
    { stars: 3, count: 0 },
    { stars: 2, count: 0 },
    { stars: 1, count: 0 },
  ],
  topTags: [{ tag: 'listens', count: 1 }],
};

function renderReviews(doctorProfileId = 'doc-1') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <DoctorReviews doctorProfileId={doctorProfileId} />
    </QueryClientProvider>,
  );
}

describe('DoctorReviews', () => {
  beforeEach(() => {
    mockGetDoctorReviews.mockReset();
    mockVoteHelpful.mockReset();
    mockGetDoctorReviews.mockResolvedValue(mockReviewsData);
  });

  it('renders aggregate score and review count', async () => {
    renderReviews();

    expect(await screen.findByText('4.5')).toBeDefined();
    expect(screen.getByText('2 reviews')).toBeDefined();
  });

  it('renders rating breakdown bars', async () => {
    renderReviews();

    await screen.findByText('4.5');

    // 5 breakdown rows: "5 stars", "4 stars", "3 stars", "2 stars", "1 star"
    expect(screen.getByText('5 stars')).toBeDefined();
    expect(screen.getByText('4 stars')).toBeDefined();
    expect(screen.getByText('3 stars')).toBeDefined();
    expect(screen.getByText('2 stars')).toBeDefined();
    expect(screen.getByText('1 star')).toBeDefined();
  });

  it('renders tag cloud with counts', async () => {
    renderReviews();

    await screen.findByText('4.5');

    // The Chip renders "listens (1)" via the topTags mapping
    expect(screen.getByText('listens (1)')).toBeDefined();
  });

  it('renders review list', async () => {
    renderReviews();

    expect(await screen.findByText('Excellent')).toBeDefined();
    expect(screen.getByText('Good visit')).toBeDefined();
  });

  it('renders doctor response when present', async () => {
    renderReviews();

    expect(await screen.findByText('Thank you!')).toBeDefined();
  });

  it('sort selector changes query', async () => {
    const user = userEvent.setup();
    renderReviews();

    await screen.findByText('4.5');

    const sortSelect = screen.getByRole('combobox', { name: 'Sort reviews' });
    await user.selectOptions(sortSelect, 'highest');

    await waitFor(() => {
      const calls = mockGetDoctorReviews.mock.calls;
      const lastCall = calls[calls.length - 1];
      expect(lastCall[1]).toEqual(
        expect.objectContaining({ sortBy: 'highest' }),
      );
    });
  });

  it('vote helpful button calls API', async () => {
    const user = userEvent.setup();
    mockVoteHelpful.mockResolvedValue({ newCount: 4 });
    renderReviews();

    await screen.findByText('Excellent');

    // Click the helpful button on the first review (helpfulCount = 3)
    const helpfulButton = screen.getByRole('button', {
      name: 'Mark review as helpful, currently 3 votes',
    });
    await user.click(helpfulButton);

    await waitFor(() => {
      expect(mockVoteHelpful).toHaveBeenCalledTimes(1);
      expect(mockVoteHelpful).toHaveBeenCalledWith('r1');
    });
  });
});
