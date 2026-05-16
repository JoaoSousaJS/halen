import { test, expect } from '@playwright/test';
import { fakeJwt } from './helpers';

const platformAdminToken = fakeJwt({
  sub: 'pa-001',
  email: 'platform@halen.dev',
  given_name: 'Platform',
  family_name: 'Admin',
  role: 'PlatformAdmin',
  clinic_id: 'c-root',
  exp: 9_999_999_999,
});

const mockClinics = [
  { id: 'c-001', name: 'Sunrise Health', slug: 'sunrise-health', isActive: true, createdAt: '2026-03-01T10:00:00Z' },
  { id: 'c-002', name: 'Metro Medical', slug: 'metro-medical', isActive: false, createdAt: '2026-04-15T08:30:00Z' },
];

const mockClinicDetail = {
  id: 'c-001',
  name: 'Sunrise Health',
  slug: 'sunrise-health',
  isActive: true,
  userCount: 42,
  createdAt: '2026-03-01T10:00:00Z',
  featureFlags: [
    { featureKey: 'prescriptions', isEnabled: true },
    { featureKey: 'kyc', isEnabled: false },
    { featureKey: 'video_calls', isEnabled: true },
  ],
};

test.describe('Platform Admin — Clinics', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, platformAdminToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/me/features', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route(/\/api\/v1\/clinics/, (route) => {
      const url = route.request().url();
      if (url.includes('/features/')) {
        return route.fulfill({ status: 204 });
      }
      if (/\/clinics\/[a-z0-9-]+$/.test(new URL(url).pathname)) {
        return route.fulfill({ status: 200, json: mockClinicDetail });
      }
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { clinicId: 'c-new' } });
      }
      return route.fulfill({ status: 200, json: { clinics: mockClinics, totalCount: mockClinics.length } });
    });
  });

  test('shows Platform Admin branding and clinics list', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Platform Admin · Platform')).toBeVisible();
    await expect(page.getByText('Sunrise Health')).toBeVisible();
    await expect(page.getByText('Metro Medical')).toBeVisible();
  });

  test('shows clinic status badges', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Active').first()).toBeVisible();
    await expect(page.getByText('Inactive')).toBeVisible();
  });

  test('create clinic dialog opens and submits', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Create clinic' }).click();
    await expect(page.getByRole('heading', { name: 'Create Clinic' })).toBeVisible();

    await page.getByLabel('Name').fill('New Clinic');
    await expect(page.getByLabel('Slug')).toHaveValue('new-clinic');

    await page.getByRole('button', { name: 'Create', exact: true }).click();
    // Dialog closes on success
    await expect(page.getByRole('heading', { name: 'Create Clinic' })).not.toBeVisible();
  });

  test('navigate to clinic detail and back', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await expect(page.getByRole('heading', { name: 'Sunrise Health' })).toBeVisible();
    await expect(page.getByText('sunrise-health')).toBeVisible();
    await expect(page.getByText('42')).toBeVisible(); // user count

    await page.getByRole('button', { name: /back/i }).click();
    await expect(page.getByText('Metro Medical')).toBeVisible();
  });

  test('clinic detail shows feature flags with toggle', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await expect(page.getByText('Feature Flags')).toBeVisible();
    await expect(page.getByText('prescriptions')).toBeVisible();
    await expect(page.getByText('kyc')).toBeVisible();
    await expect(page.getByText('video_calls')).toBeVisible();
  });

  test('edit clinic form appears and saves', async ({ page }) => {
    await page.route('**/api/v1/clinics/c-001', (route) => {
      if (route.request().method() === 'PUT') {
        return route.fulfill({ status: 204 });
      }
      return route.fulfill({ status: 200, json: mockClinicDetail });
    });

    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();
    await page.getByRole('button', { name: 'Edit clinic' }).click();

    await expect(page.getByLabel('Name')).toHaveValue('Sunrise Health');
    await page.getByLabel('Name').fill('Sunrise Health Updated');
    await page.getByRole('button', { name: 'Save' }).click();

    // Form closes after save
    await expect(page.getByRole('button', { name: 'Edit clinic' })).toBeVisible();
  });

  test('search filters clinics', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Sunrise Health')).toBeVisible();
    await expect(page.getByText('Metro Medical')).toBeVisible();

    await page.route('**/api/v1/clinics**', (route) => {
      const url = new URL(route.request().url());
      const search = url.searchParams.get('search')?.toLowerCase() || '';
      const filtered = mockClinics.filter((c) => c.name.toLowerCase().includes(search));
      return route.fulfill({ status: 200, json: { clinics: filtered, totalCount: filtered.length } });
    });

    await page.getByPlaceholder('Search clinics...').fill('Sunrise');
    await expect(page.getByText('Sunrise Health')).toBeVisible();
    await expect(page.getByText('Metro Medical')).not.toBeVisible();
  });
});
