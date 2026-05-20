import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, loginAs, mockBaseRoutes } from './helpers';
import type { DoctorProfileResponse } from '../src/shared/api/doctors';

const profileData: DoctorProfileResponse = {
  doctor: {
    id: 'doc-1',
    name: 'Dr. Ana Costa',
    specialty: 'Cardiology',
    consultationFee: 150,
    yearsOfExperience: 12,
    languages: ['English', 'Portuguese'],
    averageRating: 4.5,
    reviewCount: 3,
  },
  availability: [
    {
      dayOfWeek: 'Monday',
      windows: [
        { startTime: '09:00', endTime: '12:00', slotDurationMinutes: 20 },
        { startTime: '14:00', endTime: '17:00', slotDurationMinutes: 20 },
      ],
    },
    {
      dayOfWeek: 'Wednesday',
      windows: [
        { startTime: '10:00', endTime: '14:00', slotDurationMinutes: 30 },
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
      title: 'Excellent care',
      body: 'Very thorough and professional.',
      tags: ['thorough'],
      postedAs: 'Maya C.',
      helpfulCount: 8,
      doctorResponse: 'Thank you, Maya!',
      doctorRespondedAt: '2026-05-11T09:00:00Z',
      createdAt: '2026-05-10T14:30:00Z',
    },
    {
      id: 'r2',
      rating: 4,
      title: 'Good visit',
      body: 'Helpful and friendly.',
      tags: ['listens'],
      postedAs: 'John D.',
      helpfulCount: 2,
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: '2026-05-08T09:00:00Z',
    },
  ],
  reviewTotalCount: 3,
};

async function mockProfileRoute(page: import('@playwright/test').Page) {
  await page.route('**/api/v1/doctors/*/profile**', (route) =>
    route.fulfill({ status: 200, json: profileData }),
  );
}

test.describe('Doctor Profile Page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await mockProfileRoute(page);
  });

  test('displays doctor info, availability, and reviews', async ({ page }) => {
    await page.goto('/doctors/doc-1/profile');

    await expect(page.getByText('Dr. Ana Costa')).toBeVisible();
    await expect(page.getByText('Cardiology')).toBeVisible();
    await expect(page.getByText('$150')).toBeVisible();
    await expect(page.getByText('12 years experience')).toBeVisible();
    await expect(page.getByText('English, Portuguese')).toBeVisible();

    await expect(page.getByText('Monday')).toBeVisible();
    await expect(page.getByText('09:00 – 12:00')).toBeVisible();
    await expect(page.getByText('Wednesday')).toBeVisible();

    await expect(page.getByText('Excellent care')).toBeVisible();
    await expect(page.getByText('Good visit')).toBeVisible();
    await expect(page.getByText('Thank you, Maya!')).toBeVisible();

    await expect(page.getByText('5 stars')).toBeVisible();
    await expect(page.getByText('thorough (2)')).toBeVisible();
  });

  test('sort selector refetches reviews', async ({ page }) => {
    let lastSortBy = '';
    await page.route('**/api/v1/doctors/*/profile**', (route) => {
      const url = new URL(route.request().url());
      lastSortBy = url.searchParams.get('reviewSortBy') ?? 'newest';
      return route.fulfill({ status: 200, json: profileData });
    });

    await page.goto('/doctors/doc-1/profile');
    await expect(page.getByText('Dr. Ana Costa')).toBeVisible();

    await page.getByRole('combobox', { name: 'Sort reviews' }).selectOption('highest');

    await page.waitForTimeout(500);
    expect(lastSortBy).toBe('highest');
  });

  test('book appointment button navigates to dashboard', async ({ page }) => {
    await page.goto('/doctors/doc-1/profile');
    await expect(page.getByText('Dr. Ana Costa')).toBeVisible();

    await page.getByRole('button', { name: /Book appointment/i }).click();

    await expect(page).toHaveURL('/dashboard');
  });

  test('shows loading state', async ({ page }) => {
    await page.route('**/api/v1/doctors/*/profile**', (route) =>
      new Promise(() => {}),
    );

    await page.goto('/doctors/doc-1/profile');

    await expect(page.getByText('Loading profile…')).toBeVisible();
  });

  test('hides reviews when reviewsSummary is null', async ({ page }) => {
    await page.route('**/api/v1/doctors/*/profile**', (route) =>
      route.fulfill({
        status: 200,
        json: {
          ...profileData,
          reviewsSummary: null,
          reviews: [],
          reviewTotalCount: 0,
        },
      }),
    );

    await page.goto('/doctors/doc-1/profile');
    await expect(page.getByText('Dr. Ana Costa')).toBeVisible();

    await expect(page.getByText('5 stars')).not.toBeVisible();
    await expect(page.getByText('Excellent care')).not.toBeVisible();
  });

  test('navigates from doctor search to profile', async ({ page }) => {
    await mockBaseRoutes(page, {
      searchDoctors: [
        {
          id: 'doc-1',
          name: 'Dr. Ana Costa',
          specialty: 'Cardiology',
          consultationFee: 150,
          yearsOfExperience: 12,
          languages: ['English', 'Portuguese'],
          nextAvailableSlot: null,
          averageRating: 4.5,
          reviewCount: 3,
        },
      ],
    });

    await page.goto('/dashboard');
    await expect(page.getByText('Dr. Ana Costa')).toBeVisible();

    await page.getByRole('link', { name: 'View profile' }).click();

    await expect(page).toHaveURL('/doctors/doc-1/profile');
    await expect(page.getByRole('heading', { name: 'Dr. Ana Costa' })).toBeVisible();
  });
});
