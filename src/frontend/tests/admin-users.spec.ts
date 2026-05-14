import { test, expect } from '@playwright/test';

function fakeJwt(payload: object): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const body = Buffer.from(JSON.stringify(payload)).toString('base64url');
  return `${header}.${body}.fake-sig`;
}

const adminToken = fakeJwt({
  sub: 'a-001',
  email: 'admin@test.com',
  given_name: 'Lior',
  family_name: 'Adler',
  role: 'Admin',
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

    await page.getByRole('button', { name: 'Doctor', exact: true }).click();
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

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByText('User management')).not.toBeVisible();
  });
});
