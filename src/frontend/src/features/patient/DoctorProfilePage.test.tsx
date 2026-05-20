import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import DoctorProfilePage from './DoctorProfilePage';
import type { DoctorProfileResponse } from '../../shared/api/doctors';

const mockGetDoctorProfile = vi.fn();
const mockNavigate = vi.fn();

vi.mock('../../shared/api/doctors', async () => {
  const actual = await vi.importActual('../../shared/api/doctors');
  return {
    ...actual,
    getDoctorProfile: (...args: unknown[]) => mockGetDoctorProfile(...args),
  };
});

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockProfileData: DoctorProfileResponse = {
  doctor: {
    id: 'doc-1',
    name: 'Dr. Ana Costa',
    specialty: 'Cardiology',
    consultationFee: 150,
    yearsOfExperience: 10,
    languages: ['English', 'Portuguese'],
    averageRating: 4.5,
    reviewCount: 3,
  },
  availability: [
    {
      dayOfWeek: 'Monday',
      windows: [
        { startTime: '09:00', endTime: '12:00', slotDurationMinutes: 20 },
        { startTime: '14:00', endTime: '16:00', slotDurationMinutes: 20 },
      ],
    },
    {
      dayOfWeek: 'Wednesday',
      windows: [
        { startTime: '14:00', endTime: '17:00', slotDurationMinutes: 20 },
      ],
    },
  ],
  reviewsSummary: {
    averageRating: 4.5,
    totalCount: 3,
    ratingBreakdown: [
      { stars: 5, count: 1 },
      { stars: 4, count: 1 },
      { stars: 3, count: 1 },
      { stars: 2, count: 0 },
      { stars: 1, count: 0 },
    ],
    topTags: [
      { tag: 'thorough', count: 2 },
      { tag: 'listens', count: 1 },
    ],
  },
  reviews: [
    {
      id: 'r1',
      rating: 5,
      title: 'Excellent',
      body: 'Very thorough',
      tags: ['thorough'],
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
  reviewTotalCount: 3,
};

function renderPage(doctorId = 'doc-1') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/doctors/${doctorId}/profile`]}>
        <Routes>
          <Route path="/doctors/:id/profile" element={<DoctorProfilePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DoctorProfilePage', () => {
  beforeEach(() => {
    mockGetDoctorProfile.mockReset();
    mockNavigate.mockReset();
    mockGetDoctorProfile.mockResolvedValue(mockProfileData);
  });

  it('renders doctor name and specialty', async () => {
    renderPage();

    expect(await screen.findByText('Dr. Ana Costa')).toBeDefined();
    expect(screen.getByText('Cardiology')).toBeDefined();
  });

  it('renders consultation fee and experience', async () => {
    renderPage();

    expect(await screen.findByText('$150')).toBeDefined();
    expect(screen.getByText('10 years experience')).toBeDefined();
  });

  it('renders languages', async () => {
    renderPage();

    await screen.findByText('Dr. Ana Costa');
    expect(screen.getByText('English, Portuguese')).toBeDefined();
  });

  it('renders average rating and review count', async () => {
    renderPage();

    await screen.findByText('Dr. Ana Costa');
    expect(screen.getAllByText('4.5').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('3 reviews').length).toBeGreaterThanOrEqual(1);
  });

  it('renders availability schedule', async () => {
    renderPage();

    await screen.findByText('Dr. Ana Costa');
    expect(screen.getByText('Monday')).toBeDefined();
    expect(screen.getByText('09:00 – 12:00')).toBeDefined();
    expect(screen.getByText('14:00 – 16:00')).toBeDefined();
    expect(screen.getByText('Wednesday')).toBeDefined();
    expect(screen.getByText('14:00 – 17:00')).toBeDefined();
  });

  it('renders rating breakdown', async () => {
    renderPage();

    await screen.findByText('Dr. Ana Costa');
    expect(screen.getByText('5 stars')).toBeDefined();
    expect(screen.getByText('4 stars')).toBeDefined();
    expect(screen.getByText('1 star')).toBeDefined();
  });

  it('renders top tags', async () => {
    renderPage();

    await screen.findByText('Dr. Ana Costa');
    expect(screen.getByText('thorough (2)')).toBeDefined();
    expect(screen.getByText('listens (1)')).toBeDefined();
  });

  it('renders review list', async () => {
    renderPage();

    expect(await screen.findByText('Excellent')).toBeDefined();
    expect(screen.getByText('Good visit')).toBeDefined();
    expect(screen.getByText('Very thorough')).toBeDefined();
  });

  it('renders doctor response when present', async () => {
    renderPage();

    expect(await screen.findByText('Thank you!')).toBeDefined();
  });

  it('shows loading state initially', () => {
    mockGetDoctorProfile.mockReturnValue(new Promise(() => {}));
    renderPage();

    expect(screen.getByText('Loading profile…')).toBeDefined();
  });

  it('shows error state on failure', async () => {
    mockGetDoctorProfile.mockRejectedValue(new Error('Network error'));
    renderPage();

    expect(await screen.findByText(/Failed to load/)).toBeDefined();
  });

  it('hides reviews section when reviewsSummary is null', async () => {
    mockGetDoctorProfile.mockResolvedValue({
      ...mockProfileData,
      reviewsSummary: null,
      reviews: [],
      reviewTotalCount: 0,
    });
    renderPage();

    await screen.findByText('Dr. Ana Costa');
    expect(screen.queryByText('5 stars')).toBeNull();
    expect(screen.queryByText('Reviews')).toBeNull();
  });

  it('sort selector changes review query', async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Excellent');

    const sortSelect = screen.getByRole('combobox', { name: 'Sort reviews' });
    await user.selectOptions(sortSelect, 'highest');

    await waitFor(() => {
      const calls = mockGetDoctorProfile.mock.calls;
      const lastCall = calls[calls.length - 1];
      expect(lastCall[1]).toEqual(
        expect.objectContaining({ reviewSortBy: 'highest' }),
      );
    });
  });

  it('navigates to dashboard on book button click', async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Dr. Ana Costa');
    const bookBtn = screen.getByRole('button', { name: /Book appointment/i });
    await user.click(bookBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
  });
});
