import { test, expect } from '@playwright/test';

// Builds a structurally valid JWT with the given payload.
// The signature segment is fake — the app never validates it client-side.
function fakeJwt(payload: object): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const body = Buffer.from(JSON.stringify(payload)).toString('base64url');
  return `${header}.${body}.fake-sig`;
}

const patientToken = fakeJwt({
  sub: '1',
  email: 'patient@test.com',
  given_name: 'Maya',
  family_name: 'Chen',
  role: 'Patient',
  exp: 9_999_999_999,
});

test.describe('Login', () => {
  test('renders brand and form fields', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText('Halen').first()).toBeVisible();
    await expect(page.getByPlaceholder('you@example.com')).toBeVisible();
    await expect(page.getByPlaceholder('••••••••')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
  });

  test('navigates to patient dashboard after successful login', async ({ page }) => {
    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({ status: 200, json: { token: patientToken } }),
    );
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/appointments', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );

    await page.goto('/login');
    await page.fill('input[type="email"]', 'patient@test.com');
    await page.fill('input[type="password"]', 'Test1234!');
    await page.click('button[type="submit"]');

    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
  });

  test('shows error message on invalid credentials', async ({ page }) => {
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({ status: 401, json: { error: 'Invalid email or password' } }),
    );

    await page.goto('/login');
    await page.fill('input[type="email"]', 'bad@test.com');
    await page.fill('input[type="password"]', 'wrongpass');
    await page.click('button[type="submit"]');

    await expect(page.getByText('Invalid email or password')).toBeVisible();
  });
});

test.describe('Register', () => {
  test('renders all form fields', async ({ page }) => {
    await page.goto('/register');
    await expect(page.getByPlaceholder('Maya')).toBeVisible();
    await expect(page.getByPlaceholder('Chen')).toBeVisible();
    await expect(page.getByPlaceholder('you@example.com')).toBeVisible();
    await expect(page.getByPlaceholder('8+ characters, include a digit')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create account' })).toBeVisible();
  });

  test('navigates to patient dashboard after successful registration', async ({ page }) => {
    await page.route('**/hubs/**', (route) => route.abort());
    const token = fakeJwt({
      sub: '2',
      email: 'new@test.com',
      given_name: 'New',
      family_name: 'User',
      role: 'Patient',
      exp: 9_999_999_999,
    });

    await page.route('**/api/v1/auth/register', (route) =>
      route.fulfill({ status: 200, json: { token } }),
    );
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route('**/api/v1/appointments', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );

    await page.goto('/register');
    await page.fill('input[placeholder="Maya"]', 'New');
    await page.fill('input[placeholder="Chen"]', 'User');
    await page.fill('input[type="email"]', 'new@test.com');
    await page.fill('input[placeholder="8+ characters, include a digit"]', 'Test1234!');
    await page.click('button[type="submit"]');

    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
  });
});

test.describe('Protected routes', () => {
  test('unauthenticated visit to /dashboard redirects to /login', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });
});
