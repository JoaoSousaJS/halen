import { test, expect } from '@playwright/test';
import { fakeJwt } from './helpers';

const clinicAdminToken = fakeJwt({
  sub: 'ca-001',
  email: 'clinicadmin@test.com',
  given_name: 'Clinic',
  family_name: 'Admin',
  role: 'ClinicAdmin',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

const mockUsers = [
  { id: 'u-001', name: 'Dr. House', role: 'Doctor', status: 'Active', plan: null, lastLoginAt: new Date().toISOString(), isFlagged: false },
  { id: 'u-002', name: 'Jane Patient', role: 'Patient', status: 'Active', plan: null, lastLoginAt: new Date().toISOString(), isFlagged: false },
];

test.describe('Clinic Admin — User Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, clinicAdminToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/admin/users**', (route) =>
      route.fulfill({ status: 200, json: { users: mockUsers, totalCount: mockUsers.length } }),
    );
    await page.route('**/api/v1/admin/doctors', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200, json: { doctorId: 'd-new' } });
      }
      return route.continue();
    });
    await page.route('**/api/v1/clinic/users', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { userId: 'u-new' } });
      }
      return route.continue();
    });
    await page.route('**/api/v1/me/features', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
  });

  test('shows Clinic Admin branding', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Clinic Admin · Clinic')).toBeVisible();
    await expect(page.getByText('Dr. House')).toBeVisible();
  });

  test('create user dialog opens and submits', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: '+ Create user' }).click();

    await expect(page.getByRole('heading', { name: 'Create User' })).toBeVisible();

    await page.getByLabel('Email').fill('newuser@test.com');
    await page.getByLabel('First name').fill('New');
    await page.getByLabel('Last name').fill('User');
    await page.getByLabel('Temporary password').fill('Secure123!');
    await page.getByLabel('Role').selectOption('Patient');

    await page.locator('.modal button[type="submit"]').click();
    // Dialog closes on success
    await expect(page.getByRole('heading', { name: 'Create User' })).not.toBeVisible();
  });

  test('create user dialog shows server error', async ({ page }) => {
    await page.route('**/api/v1/clinic/users', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 400, json: { error: 'Email already exists in this clinic' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: '+ Create user' }).click();

    await page.getByLabel('Email').fill('existing@test.com');
    await page.getByLabel('First name').fill('Dup');
    await page.getByLabel('Last name').fill('User');
    await page.getByLabel('Temporary password').fill('Secure123!');

    await page.locator('.modal button[type="submit"]').click();
    await expect(page.getByText('Email already exists in this clinic')).toBeVisible();
  });

  test('create user cancel closes dialog', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: '+ Create user' }).click();
    await expect(page.getByRole('heading', { name: 'Create User' })).toBeVisible();

    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.getByRole('heading', { name: 'Create User' })).not.toBeVisible();
  });
});
