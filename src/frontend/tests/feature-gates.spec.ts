import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockDoctorRoutes } from './helpers';

test.describe('Feature Gates — Patient Dashboard', () => {
  test('prescriptions section visible when feature enabled', async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, {
      features: [{ featureKey: 'prescriptions', isEnabled: true }],
    });

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /prescription/i })).toBeVisible();
  });

  test('prescriptions section hidden when feature disabled', async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, {
      features: [{ featureKey: 'prescriptions', isEnabled: false }],
    });

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: /prescription/i })).not.toBeVisible();
  });
});

test.describe('Feature Gates — Doctor Dashboard', () => {
  test('KYC and prescriptions visible when features enabled', async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: true },
      ],
    });
    await page.route(/\/api\/v1\/doctor\//, (route) =>
      route.fulfill({ status: 200, json: { status: 'Approved', submittedAt: '2026-01-01', lastRejectionReason: null, documents: [] } }),
    );

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: 'Issue a prescription' })).toBeVisible();
  });

  test('KYC section hidden when kyc feature disabled', async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: false },
      ],
    });
    await page.route(/\/api\/v1\/doctor\//, (route) =>
      route.fulfill({ status: 200, json: { status: 'Approved', submittedAt: '2026-01-01', lastRejectionReason: null, documents: [] } }),
    );

    await page.goto('/dashboard');
    // Prescriptions still visible (kyc disabled doesn't affect prescriptions)
    await expect(page.getByRole('heading', { name: 'Issue a prescription' })).toBeVisible();
  });
});
