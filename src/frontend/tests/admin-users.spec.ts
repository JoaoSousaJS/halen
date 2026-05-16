import { test, expect } from '@playwright/test';
import { fakeJwt } from './helpers';

const adminToken = fakeJwt({
  sub: 'a-001',
  email: 'admin@test.com',
  given_name: 'Lior',
  family_name: 'Adler',
  role: 'ClinicAdmin',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

const patientToken = fakeJwt({
  sub: 'p-001',
  email: 'patient@test.com',
  given_name: 'Maya',
  family_name: 'Chen',
  role: 'Patient',
  exp: 9_999_999_999,
});

const mockUsers = [
  { id: 'd-022', name: 'Dr. Anika Volpe', role: 'Doctor', status: 'PendingReview', plan: null, lastLoginAt: new Date().toISOString(), isFlagged: true },
  { id: 'p-044', name: 'Wesley Tanaka', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: new Date().toISOString(), isFlagged: false },
];

test.describe('Admin Users Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, adminToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/admin/users**', (route) => {
      const url = new URL(route.request().url());
      const role = url.searchParams.get('role');
      const search = url.searchParams.get('search')?.toLowerCase();
      let filtered = [...mockUsers];
      if (role) filtered = filtered.filter((u) => u.role.toLowerCase() === role);
      if (search) filtered = filtered.filter((u) => u.name.toLowerCase().includes(search));
      return route.fulfill({ status: 200, json: { users: filtered, totalCount: filtered.length } });
    });
  });

  test('admin sees user table with data', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Dr. Anika Volpe')).toBeVisible();
    await expect(page.getByText('Wesley Tanaka')).toBeVisible();
  });

  test('filter tabs filter by role', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Dr. Anika Volpe')).toBeVisible();

    await page.getByRole('tab', { name: 'Doctor', exact: true }).click();
    await expect(page.getByText('Dr. Anika Volpe')).toBeVisible();
    await expect(page.getByText('Wesley Tanaka')).not.toBeVisible();
  });

  test('flagged user shows Review button', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('button', { name: 'Review', exact: true })).toBeVisible();
  });

  test('shows Pending KYC for doctor in PendingReview', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Pending KYC')).toBeVisible();
  });

  test('search filters users by name', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Dr. Anika Volpe')).toBeVisible();
    await expect(page.getByText('Wesley Tanaka')).toBeVisible();

    await page.getByPlaceholder('Search by name or email…').fill('Wesley');
    await expect(page.getByText('Wesley Tanaka')).toBeVisible();
    await expect(page.getByText('Dr. Anika Volpe')).not.toBeVisible();
  });
});

test.describe('Admin Dashboard — tab navigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, adminToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/admin/users**', (route) =>
      route.fulfill({ status: 200, json: { users: mockUsers, totalCount: mockUsers.length } }),
    );
    await page.route('**/api/v1/admin/doctors', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200, json: { doctorId: 'd-new-001' } });
      }
      return route.continue();
    });
  });

  test('can switch to Create doctor tab and back', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Users.')).toBeVisible();

    await page.getByRole('button', { name: 'Create doctor' }).click();
    await expect(page.getByText('doctor account.')).toBeVisible();
    await expect(page.getByText('Users.')).not.toBeVisible();

    await page.getByRole('button', { name: 'Users' }).click();
    await expect(page.getByText('Users.')).toBeVisible();
  });

  test('create doctor form submits successfully', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Create doctor' }).click();

    await page.getByPlaceholder('James').fill('Gregory');
    await page.getByPlaceholder('Wilson').fill('House');
    await page.getByPlaceholder('doctor@halen.dev').fill('house@halen.dev');
    await page.getByPlaceholder('8+ characters, include a digit').fill('Secure123!');
    await page.getByPlaceholder('Cardiology').fill('Diagnostics');
    await page.getByPlaceholder('MED-12345').fill('MED-99999');
    await page.getByPlaceholder('150').fill('200');
    await page.getByRole('spinbutton', { name: 'Years of experience' }).fill('15');

    await page.getByRole('button', { name: 'Create doctor account' }).click();
    await expect(page.getByText('Doctor account created for house@halen.dev')).toBeVisible();
  });

  test('create doctor form shows error on failure', async ({ page }) => {
    await page.route('**/api/v1/admin/doctors', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 400, json: { error: 'Email already exists' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Create doctor' }).click();

    await page.getByPlaceholder('James').fill('Gregory');
    await page.getByPlaceholder('Wilson').fill('House');
    await page.getByPlaceholder('doctor@halen.dev').fill('house@halen.dev');
    await page.getByPlaceholder('8+ characters, include a digit').fill('Secure123!');
    await page.getByPlaceholder('Cardiology').fill('Diagnostics');
    await page.getByPlaceholder('MED-12345').fill('MED-99999');
    await page.getByPlaceholder('150').fill('200');
    await page.getByRole('spinbutton', { name: 'Years of experience' }).fill('15');

    await page.getByRole('button', { name: 'Create doctor account' }).click();
    await expect(page.getByText('Email already exists')).toBeVisible();
  });
});

test.describe('Admin Users — access control', () => {
  test('patient cannot see admin users page', async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, patientToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByText('User management')).not.toBeVisible();
  });
});
