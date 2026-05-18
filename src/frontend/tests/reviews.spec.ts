import { test, expect } from '@playwright/test';
import {
  PATIENT_TOKEN,
  DOCTOR_TOKEN,
  CLINIC_ADMIN_TOKEN,
  loginAs,
  mockBaseRoutes,
  mockDoctorRoutes,
  mockAdminRoutes,
} from './helpers';

const completedAppointment = {
  id: 'appt-done-1',
  scheduledAt: new Date(Date.now() - 86_400_000).toISOString(),
  durationMinutes: 20,
  reason: 'Checkup',
  status: 'Completed',
  notes: 'All good',
  doctorName: 'Dr. House',
  specialty: 'Diagnostics',
  consultationFee: 150,
  patientName: 'Maya Chen',
  patientId: 'patient-1',
  paymentStatus: 'Captured',
  paymentAmount: 150,
};

const mockMyReviews = {
  reviews: [
    {
      id: 'rev-1',
      rating: 5,
      title: 'Excellent doctor',
      body: 'Very thorough examination',
      tags: ['listens', 'thorough'],
      postedAs: 'Maya C.',
      helpfulCount: 3,
      moderationStatus: 'Approved',
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: new Date(Date.now() - 86_400_000).toISOString(),
    },
    {
      id: 'rev-2',
      rating: 2,
      title: 'Long wait time',
      body: 'Had to wait over an hour',
      tags: ['wait times'],
      postedAs: 'Alex T.',
      helpfulCount: 0,
      moderationStatus: 'Approved',
      doctorResponse: null,
      doctorRespondedAt: null,
      createdAt: new Date(Date.now() - 172_800_000).toISOString(),
    },
  ],
  totalCount: 2,
  averageRating: 3.5,
  reviewCount: 2,
};

const mockModerationQueue = {
  reviews: [
    {
      id: 'mod-rev-1',
      rating: 1,
      title: 'Terrible experience',
      body: 'Would not recommend',
      tags: [],
      postedAs: 'Anonymous',
      moderationStatus: 'Pending',
      patientName: 'Maya Chen',
      doctorName: 'Dr. House',
      createdAt: new Date().toISOString(),
    },
  ],
  totalCount: 1,
};

// ── Patient: Leave a Review ─────────────────────────────────────────────────

test.describe('Patient — Leave a review', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'doctor_reviews', isEnabled: true },
      ],
      appointments: [completedAppointment],
    });
  });

  test('shows "Leave a review" button on completed appointments', async ({ page }) => {
    await page.goto('/dashboard');

    await expect(page.getByRole('button', { name: /leave a review/i })).toBeVisible();
  });

  test('opens review form and submits a review', async ({ page }) => {
    await page.route('**/api/v1/reviews', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { reviewId: 'new-rev-1' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');

    await page.getByRole('button', { name: /leave a review/i }).click();

    await expect(page.getByText('How was your visit?')).toBeVisible();

    await page.getByRole('button', { name: '4 stars' }).click();

    const titleInput = page.getByPlaceholder('Summarize your experience…');
    await titleInput.fill('Great visit overall');

    await page.getByRole('button', { name: 'Post review' }).click();

    await expect(page.getByText('How was your visit?')).not.toBeVisible();
    await expect(page.getByRole('button', { name: /leave a review/i })).not.toBeVisible();
  });

  test('can skip the review form', async ({ page }) => {
    await page.goto('/dashboard');

    await page.getByRole('button', { name: /leave a review/i }).click();
    await expect(page.getByText('How was your visit?')).toBeVisible();

    await page.getByRole('button', { name: 'Skip for now' }).click();

    await expect(page.getByText('How was your visit?')).not.toBeVisible();
  });

  test('hides review button when feature is disabled', async ({ page }) => {
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'doctor_reviews', isEnabled: false },
      ],
      appointments: [completedAppointment],
    });

    await page.goto('/dashboard');

    await expect(page.getByText('Completed')).toBeVisible();
    await expect(page.getByRole('button', { name: /leave a review/i })).not.toBeVisible();
  });
});

// ── Doctor: My Reviews ──────────────────────────────────────────────────────

test.describe('Doctor — My Reviews', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: true },
        { featureKey: 'doctor_reviews', isEnabled: true },
      ],
      myReviews: mockMyReviews,
    });
    await mockDoctorRoutes(page);
    await page.route('**/api/v1/availability/mine', (route) =>
      route.fulfill({ status: 200, json: { windows: [] } }),
    );
  });

  test('shows reviews section with average rating', async ({ page }) => {
    await page.goto('/dashboard');

    await expect(page.getByText('3.5')).toBeVisible();
    await expect(page.getByText('2 reviews')).toBeVisible();
  });

  test('shows review cards with titles', async ({ page }) => {
    await page.goto('/dashboard');

    await expect(page.getByText('Excellent doctor')).toBeVisible();
    await expect(page.getByText('Long wait time')).toBeVisible();
  });

  test('can reply to a review', async ({ page }) => {
    await page.route('**/api/v1/reviews/*/respond', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200 });
      }
      return route.continue();
    });

    await page.goto('/dashboard');

    const replyButtons = page.getByRole('button', { name: 'Reply', exact: true });
    await replyButtons.first().click();

    const textarea = page.getByRole('textbox', { name: /reply to review/i });
    await expect(textarea).toBeVisible();
    await textarea.fill('Thank you for the feedback!');

    await page.getByRole('button', { name: 'Post reply' }).click();
  });

  test('can filter reviews by awaiting reply', async ({ page }) => {
    await page.goto('/dashboard');

    await page.getByRole('button', { name: 'Awaiting reply' }).click();

    await expect(page.getByRole('button', { name: 'Awaiting reply' })).toBeVisible();
  });

  test('hides reviews when feature is disabled', async ({ page }) => {
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: true },
        { featureKey: 'doctor_reviews', isEnabled: false },
      ],
    });
    await mockDoctorRoutes(page);
    await page.route('**/api/v1/availability/mine', (route) =>
      route.fulfill({ status: 200, json: { windows: [] } }),
    );

    await page.goto('/dashboard');

    await expect(page.getByRole('heading', { name: /your availability/i })).toBeVisible();
    await expect(page.getByText('My reviews.')).not.toBeVisible();
  });
});

// ── Admin: Review Moderation ────────────────────────────────────────────────

test.describe('Admin — Review Moderation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, CLINIC_ADMIN_TOKEN);
    await mockBaseRoutes(page, {
      features: [{ featureKey: 'prescriptions', isEnabled: true }],
      moderationQueue: mockModerationQueue,
    });
    await mockAdminRoutes(page);
  });

  test('shows reviews tab and moderation queue', async ({ page }) => {
    await page.goto('/dashboard');

    await page.getByRole('button', { name: 'Reviews' }).click();

    await expect(page.getByRole('heading', { name: /review\s+moderation/i })).toBeVisible();
    await expect(page.getByText('Terrible experience')).toBeVisible();
    await expect(page.getByText('Patient:')).toBeVisible();
    await expect(page.getByText('Doctor:')).toBeVisible();
  });

  test('can approve a review', async ({ page }) => {
    await page.route('**/api/v1/admin/reviews/*/moderate', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200 });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Reviews' }).click();

    await page.getByRole('button', { name: 'Approve' }).click();
  });

  test('can hide a review', async ({ page }) => {
    await page.route('**/api/v1/admin/reviews/*/moderate', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200 });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Reviews' }).click();

    await page.getByRole('button', { name: 'Hide' }).click();
  });

  test('can remove a review', async ({ page }) => {
    await page.route('**/api/v1/admin/reviews/*/moderate', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200 });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Reviews' }).click();

    await page.getByRole('button', { name: 'Remove' }).click();
  });

  test('shows empty state when no reviews pending', async ({ page }) => {
    await mockBaseRoutes(page, {
      features: [{ featureKey: 'prescriptions', isEnabled: true }],
      moderationQueue: { reviews: [], totalCount: 0 },
    });
    await mockAdminRoutes(page);

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Reviews' }).click();

    await expect(page.getByText('No reviews to moderate.')).toBeVisible();
  });
});
