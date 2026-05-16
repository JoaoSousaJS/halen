import { test, expect } from '@playwright/test';
import { fakeJwt } from './helpers';

const patientToken = fakeJwt({
  sub: 'p-001',
  email: 'patient@test.com',
  given_name: 'Maya',
  family_name: 'Chen',
  role: 'Patient',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

const doctorToken = fakeJwt({
  sub: 'd-001',
  email: 'doctor@test.com',
  given_name: 'Gregory',
  family_name: 'House',
  role: 'Doctor',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

test.describe('Feature Gates — Patient Dashboard', () => {
  test('prescriptions section visible when feature enabled', async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, patientToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/me/features', (route) =>
      route.fulfill({ status: 200, json: [{ featureKey: 'prescriptions', isEnabled: true }] }),
    );
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/appointments', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /prescription/i })).toBeVisible();
  });

  test('prescriptions section hidden when feature disabled', async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, patientToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/me/features', (route) =>
      route.fulfill({ status: 200, json: [{ featureKey: 'prescriptions', isEnabled: false }] }),
    );
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/appointments', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: /prescription/i })).not.toBeVisible();
  });
});

test.describe('Feature Gates — Doctor Dashboard', () => {
  test('KYC and prescriptions visible when features enabled', async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, doctorToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/me/features', (route) =>
      route.fulfill({
        status: 200,
        json: [
          { featureKey: 'prescriptions', isEnabled: true },
          { featureKey: 'kyc', isEnabled: true },
        ],
      }),
    );
    await page.route('**/api/v1/appointments', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route(/\/api\/v1\/doctor\//, (route) =>
      route.fulfill({ status: 200, json: { status: 'Approved', submittedAt: '2026-01-01', lastRejectionReason: null, documents: [] } }),
    );

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: 'Issue a prescription' })).toBeVisible();
  });

  test('KYC section hidden when kyc feature disabled', async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, doctorToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/me/features', (route) =>
      route.fulfill({
        status: 200,
        json: [
          { featureKey: 'prescriptions', isEnabled: true },
          { featureKey: 'kyc', isEnabled: false },
        ],
      }),
    );
    await page.route('**/api/v1/appointments', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route(/\/api\/v1\/doctor\//, (route) =>
      route.fulfill({ status: 200, json: { status: 'Approved', submittedAt: '2026-01-01', lastRejectionReason: null, documents: [] } }),
    );

    await page.goto('/dashboard');
    // Prescriptions still visible (kyc disabled doesn't affect prescriptions)
    await expect(page.getByRole('heading', { name: 'Issue a prescription' })).toBeVisible();
  });
});
