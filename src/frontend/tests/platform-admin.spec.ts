import { test, expect } from '@playwright/test';
import { PLATFORM_ADMIN_TOKEN, loginAs, mockBaseRoutes } from './helpers';

const mockClinics = [
  { id: 'c-001', name: 'Sunrise Health', slug: 'sunrise-health', isActive: true, createdAt: '2026-03-01T10:00:00Z' },
  { id: 'c-002', name: 'Metro Medical', slug: 'metro-medical', isActive: false, createdAt: '2026-04-15T08:30:00Z' },
];

let clinicDetail = {
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
    { featureKey: 'doctor_reviews', isEnabled: false },
    { featureKey: 'medical_records', isEnabled: true },
    { featureKey: 'messaging', isEnabled: false },
    { featureKey: 'audit_trail', isEnabled: true },
  ],
};

test.describe('Platform Admin — Clinics', () => {
  test.beforeEach(async ({ page }) => {
    clinicDetail = {
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
        { featureKey: 'doctor_reviews', isEnabled: false },
        { featureKey: 'medical_records', isEnabled: true },
        { featureKey: 'messaging', isEnabled: false },
        { featureKey: 'audit_trail', isEnabled: true },
      ],
    };

    await loginAs(page, PLATFORM_ADMIN_TOKEN);
    await mockBaseRoutes(page, { features: [] });
    await page.route(/\/api\/v1\/clinics/, (route) => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes('/features/')) {
        if (method === 'PUT') {
          const urlParts = url.split('/features/');
          const featureKey = decodeURIComponent(urlParts[1]);
          const body = route.request().postDataJSON() as { isEnabled: boolean };
          const flag = clinicDetail.featureFlags.find((f) => f.featureKey === featureKey);
          if (flag) flag.isEnabled = body.isEnabled;
          return route.fulfill({ status: 204 });
        }
        return route.fulfill({ status: 200, json: clinicDetail.featureFlags });
      }
      if (url.includes('/admins') && method === 'POST') {
        return route.fulfill({ status: 201, json: { userId: 'u-new-admin' } });
      }
      if (/\/clinics\/[a-z0-9-]+$/.test(new URL(url).pathname)) {
        if (method === 'PUT') {
          const body = route.request().postDataJSON() as { name: string; isActive: boolean };
          clinicDetail.name = body.name;
          clinicDetail.isActive = body.isActive;
          return route.fulfill({ status: 204 });
        }
        return route.fulfill({ status: 200, json: { ...clinicDetail } });
      }
      if (method === 'POST') {
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
    await expect(page.getByRole('heading', { name: 'Create Clinic' })).not.toBeVisible();
  });

  test('navigate to clinic detail and back', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await expect(page.getByRole('heading', { name: 'Sunrise Health' })).toBeVisible();
    await expect(page.getByText('sunrise-health')).toBeVisible();
    await expect(page.getByText('42')).toBeVisible();

    await page.getByRole('button', { name: /back/i }).click();
    await expect(page.getByText('Metro Medical')).toBeVisible();
  });

  test('clinic detail renders two-column layout', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await expect(page.locator('.clinic-detail-columns')).toBeVisible();
    await expect(page.getByText('Clinic Settings')).toBeVisible();
    await expect(page.getByText('Feature Flags')).toBeVisible();
  });

  test('clinic detail shows feature flags with human-readable labels', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await expect(page.locator('.flag-card-label', { hasText: 'Prescriptions' })).toBeVisible();
    await expect(page.locator('.flag-card-label', { hasText: 'KYC Verification' })).toBeVisible();
    await expect(page.locator('.flag-card-label', { hasText: 'Video Calls' })).toBeVisible();
    await expect(page.locator('.flag-card-label', { hasText: 'Doctor Reviews' })).toBeVisible();
    await expect(page.locator('.flag-card-label', { hasText: 'Medical Records' })).toBeVisible();
    await expect(page.locator('.flag-card-label', { hasText: 'Messaging' })).toBeVisible();
    await expect(page.locator('.flag-card-label', { hasText: 'Audit Trail' })).toBeVisible();

    const switches = page.getByRole('switch');
    await expect(switches).toHaveCount(8); // 7 flags + 1 status toggle
  });

  test('feature flag cards show descriptions', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await expect(page.getByText('Allow doctors to issue prescriptions')).toBeVisible();
    await expect(page.getByText('Require doctor identity verification')).toBeVisible();
    await expect(page.getByText('Enable video consultation rooms')).toBeVisible();
  });

  test('inline name edit — save and cancel', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await page.getByRole('button', { name: /Sunrise Health/ }).click();
    const input = page.getByRole('textbox');
    await expect(input).toHaveValue('Sunrise Health');

    await input.fill('Updated Clinic');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByRole('button', { name: /Updated Clinic/ })).toBeVisible();

    await page.getByRole('button', { name: /Updated Clinic/ }).click();
    const input2 = page.getByRole('textbox');
    await input2.fill('Should Not Save');
    await page.getByRole('button', { name: /Cancel/ }).click();

    await expect(page.getByRole('button', { name: /Updated Clinic/ })).toBeVisible();
  });

  test('inline name edit — Enter saves, Escape cancels', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await page.getByRole('button', { name: /Sunrise Health/ }).click();
    const input = page.getByRole('textbox');
    await input.fill('Enter Save Test');
    await input.press('Enter');

    await expect(page.getByRole('button', { name: /Enter Save Test/ })).toBeVisible();

    await page.getByRole('button', { name: /Enter Save Test/ }).click();
    const input2 = page.getByRole('textbox');
    await input2.fill('Should Not Save');
    await input2.press('Escape');

    await expect(page.getByRole('button', { name: /Enter Save Test/ })).toBeVisible();
  });

  test('status toggle shows confirmation dialog for deactivation', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    const statusSwitch = page.getByRole('switch', { name: /active status/i });
    await expect(statusSwitch).toHaveAttribute('aria-checked', 'true');

    await statusSwitch.click();

    await expect(page.getByText(/deactivate this clinic/i)).toBeVisible();
    await expect(page.getByText(/will be suspended/i)).toBeVisible();

    await page.getByRole('button', { name: /deactivate/i }).click();

    await expect(statusSwitch).toHaveAttribute('aria-checked', 'false');
  });

  test('feature flag toggle persists', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    const kycSwitch = page.getByRole('switch', { name: /KYC Verification/i });
    await expect(kycSwitch).toHaveAttribute('aria-checked', 'false');

    await kycSwitch.click();

    await expect(kycSwitch).toHaveAttribute('aria-checked', 'true');
  });

  test('feature flag toggle error shows inline message', async ({ page }) => {
    await page.route('**/api/v1/clinics/c-001/features/**', (route) => {
      if (route.request().method() === 'PUT') {
        return route.fulfill({ status: 500, json: { message: 'Internal server error' } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    const kycSwitch = page.getByRole('switch', { name: /KYC Verification/i });
    await kycSwitch.click();

    await expect(page.getByRole('alert')).toBeVisible();
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

  test('create clinic admin — opens dialog and submits', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await page.getByRole('button', { name: /create clinic admin/i }).click();
    await expect(page.getByRole('heading', { name: 'Create Clinic Admin' })).toBeVisible();

    await page.getByPlaceholder('Jane').fill('Alice');
    await page.getByPlaceholder('Doe').fill('Santos');
    await page.getByPlaceholder('admin@clinic.com').fill('alice@sunrise.com');
    await page.getByPlaceholder('Min. 8 characters').fill('Admin1234!');

    await page.getByRole('button', { name: 'Create admin' }).click();

    await expect(page.getByRole('heading', { name: 'Create Clinic Admin' })).not.toBeVisible();
  });

  test('create clinic admin — shows server error', async ({ page }) => {
    await page.route('**/api/v1/clinics/c-001/admins', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 400, json: { error: 'Email already exists' } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await page.getByRole('button', { name: /create clinic admin/i }).click();

    await page.getByPlaceholder('Jane').fill('Alice');
    await page.getByPlaceholder('Doe').fill('Santos');
    await page.getByPlaceholder('admin@clinic.com').fill('alice@sunrise.com');
    await page.getByPlaceholder('Min. 8 characters').fill('Admin1234!');

    await page.getByRole('button', { name: 'Create admin' }).click();

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Create Clinic Admin' })).toBeVisible();
  });

  test('create clinic admin — button disabled for inactive clinic', async ({ page }) => {
    clinicDetail = { ...clinicDetail, isActive: false, name: 'Deactivated Clinic' };

    await page.goto('/dashboard');
    await page.getByText('Sunrise Health').click();

    await expect(page.getByRole('button', { name: /create clinic admin/i })).toBeDisabled();
  });
});
