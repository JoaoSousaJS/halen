import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import ReviewForm from './ReviewForm';

const mockSubmitReview = vi.fn();

vi.mock('../../shared/api/reviews', () => ({
  submitReview: (...args: unknown[]) => mockSubmitReview(...args),
}));

const defaultProps = {
  appointmentId: 'apt-1',
  patientFirstName: 'Test',
  patientLastInitial: 'P',
  onClose: vi.fn(),
  onSuccess: vi.fn(),
};

function renderForm(overrides: Partial<typeof defaultProps> = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <ReviewForm {...defaultProps} {...overrides} />
    </QueryClientProvider>,
  );
}

describe('ReviewForm', () => {
  beforeEach(() => {
    mockSubmitReview.mockReset();
    defaultProps.onClose = vi.fn();
    defaultProps.onSuccess = vi.fn();
  });

  it('renders star selector, title input, tag chips', () => {
    renderForm();

    // 5 star buttons
    const starButtons = [1, 2, 3, 4, 5].map((n) =>
      screen.getByRole('button', { name: `${n} star${n > 1 ? 's' : ''}` }),
    );
    expect(starButtons).toHaveLength(5);

    // Title input
    expect(screen.getByPlaceholderText('Summarize your experience')).toBeDefined();

    // At least one tag chip (the component renders ALLOWED_TAGS as checkboxes)
    const tagCheckboxes = screen.getAllByRole('checkbox');
    expect(tagCheckboxes.length).toBeGreaterThanOrEqual(1);
  });

  it('disables submit until rating and title provided', () => {
    renderForm();

    const submitButton = screen.getByRole('button', { name: 'Post review' });
    expect(submitButton.getAttribute('disabled')).not.toBeNull();
  });

  it('enables submit when rating and title filled', async () => {
    const user = userEvent.setup();
    renderForm();

    // Click 4 stars
    await user.click(screen.getByRole('button', { name: '4 stars' }));

    // Type a title (3+ chars)
    const titleInput = screen.getByPlaceholderText('Summarize your experience');
    await user.type(titleInput, 'Great visit');

    const submitButton = screen.getByRole('button', { name: 'Post review' });
    expect(submitButton.getAttribute('disabled')).toBeNull();
  });

  it('calls submitReview API on submit', async () => {
    const user = userEvent.setup();
    mockSubmitReview.mockResolvedValue({ reviewId: 'r-new' });
    renderForm();

    // Fill the form
    await user.click(screen.getByRole('button', { name: '5 stars' }));
    await user.type(
      screen.getByPlaceholderText('Summarize your experience'),
      'Wonderful doctor',
    );
    await user.type(
      screen.getByPlaceholderText('Tell others about your experience'),
      'Very thorough and kind',
    );

    // Submit
    await user.click(screen.getByRole('button', { name: 'Post review' }));

    await waitFor(() => {
      expect(mockSubmitReview).toHaveBeenCalledTimes(1);
    });

    expect(mockSubmitReview).toHaveBeenCalledWith({
      appointmentId: 'apt-1',
      rating: 5,
      title: 'Wonderful doctor',
      body: 'Very thorough and kind',
      tags: [],
    });
  });

  it('shows character counter for body', async () => {
    const user = userEvent.setup();
    renderForm();

    // Initially counter should show 0/600
    expect(screen.getByText('0/600')).toBeDefined();

    // Type in body textarea
    const body = screen.getByPlaceholderText('Tell others about your experience');
    await user.type(body, 'Hello');

    expect(screen.getByText('5/600')).toBeDefined();
  });

  it('limits tag selection to 6', async () => {
    const user = userEvent.setup();
    renderForm();

    const tagButtons = screen.getAllByRole('checkbox');
    // There are 8 tags in ALLOWED_TAGS, click the first 7
    for (let i = 0; i < 7 && i < tagButtons.length; i++) {
      await user.click(tagButtons[i]);
    }

    // Count how many are checked
    const checkedTags = screen
      .getAllByRole('checkbox')
      .filter((btn) => btn.getAttribute('aria-checked') === 'true');
    expect(checkedTags).toHaveLength(6);

    // The 7th tag should be disabled (not checked, at max capacity)
    const uncheckedTags = screen
      .getAllByRole('checkbox')
      .filter((btn) => btn.getAttribute('aria-checked') === 'false');
    const disabledUnchecked = uncheckedTags.filter(
      (btn) => btn.getAttribute('disabled') !== null,
    );
    expect(disabledUnchecked.length).toBeGreaterThanOrEqual(1);
  });

  it('calls onClose when Skip clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderForm({ onClose });

    await user.click(screen.getByRole('button', { name: 'Skip for now' }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('shows privacy notice with name', () => {
    renderForm();

    expect(screen.getByText('Posted as Test P.')).toBeDefined();
  });
});
