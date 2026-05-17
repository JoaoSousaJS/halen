import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, loginAs, mockBaseRoutes } from './helpers';

// ── Mock Data ─────────────────────────────────────────────────────────────────

const mockProfile = {
  id: '1',
  firstName: 'Maya',
  lastName: 'Chen',
  email: 'patient@test.com',
  role: 'Patient',
  createdAt: '2025-06-01T00:00:00Z',
  lastLoginAt: '2026-05-17T10:00:00Z',
  specialty: null,
  consultationFee: null,
  yearsOfExperience: null,
  languages: null,
  dateOfBirth: '1990-03-15',
  city: 'Portland',
  subscriptionPlan: null,
};

// ── User Profile Tests ────────────────────────────────────────────────────────

test.describe('User Profile', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await page.route('**/api/v1/profile/me', (route) => {
      // Skip sub-paths like /me/change-password
      if (/\/profile\/me\//.test(route.request().url())) {
        return route.fallback();
      }
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: { profile: mockProfile } });
      }
      return route.fallback();
    });
  });

  test('patient clicks name in header, navigates to profile page', async ({ page }) => {
    await page.goto('/dashboard');

    // The user name is rendered as a link to /profile in the DashboardShell header
    await page.getByRole('link', { name: 'Maya Chen' }).click();

    // Should navigate to /profile
    await expect(page).toHaveURL(/\/profile/);

    // Profile page should render with profile heading
    await expect(page.getByRole('heading', { name: 'Profile' })).toBeVisible();
  });

  test('profile page shows patient fields', async ({ page }) => {
    await page.goto('/profile');

    // Check all expected patient fields are visible
    await expect(page.getByLabel('First name')).toHaveValue('Maya');
    await expect(page.getByLabel('Last name')).toHaveValue('Chen');
    await expect(page.getByLabel('Email')).toHaveValue('patient@test.com');
    await expect(page.getByLabel('Date of birth')).toHaveValue('1990-03-15');
    await expect(page.getByLabel('City')).toHaveValue('Portland');
  });

  test('patient edits first name and saves successfully', async ({ page }) => {
    await page.route('**/api/v1/profile/me', (route) => {
      if (/\/profile\/me\//.test(route.request().url())) {
        return route.fallback();
      }
      if (route.request().method() === 'PUT') {
        return route.fulfill({ status: 200, json: {} });
      }
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: { profile: mockProfile } });
      }
      return route.fallback();
    });

    await page.goto('/profile');

    // Clear and type new first name
    const firstNameInput = page.getByLabel('First name');
    await firstNameInput.clear();
    await firstNameInput.fill('Mia');

    // Click Save changes
    await page.getByRole('button', { name: 'Save changes' }).click();

    // Success message
    await expect(page.getByText('Profile updated successfully.')).toBeVisible();
  });

  test('patient changes password successfully', async ({ page }) => {
    await page.route('**/api/v1/profile/me/change-password', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200, json: {} });
      }
      return route.fallback();
    });

    await page.goto('/profile');

    // Fill password form
    await page.getByLabel('Current password').fill('OldP@ss123');
    await page.getByLabel('New password', { exact: true }).fill('NewP@ss456');
    await page.getByLabel('Confirm new password').fill('NewP@ss456');

    // Submit
    await page.getByRole('button', { name: 'Change password' }).click();

    // Success message
    await expect(page.getByText('Password changed successfully.')).toBeVisible();
  });

  test('password confirm mismatch shows error', async ({ page }) => {
    await page.goto('/profile');

    // Fill with mismatched passwords
    await page.getByLabel('Current password').fill('OldP@ss123');
    await page.getByLabel('New password', { exact: true }).fill('NewP@ss456');
    await page.getByLabel('Confirm new password').fill('WrongP@ss789');

    // Submit
    await page.getByRole('button', { name: 'Change password' }).click();

    // Error message (client-side validation, no API call needed)
    await expect(page.getByText('New password and confirmation do not match.')).toBeVisible();
  });
});
