import { test, expect } from '@playwright/test';
import { fakeJwt, PATIENT_TOKEN, mockBaseRoutes } from './helpers';

test.describe('Login', () => {
  test('renders brand and form fields', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText('Halen').first()).toBeVisible();
    await expect(page.getByPlaceholder('you@example.com')).toBeVisible();
    await expect(page.getByPlaceholder('••••••••')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
  });

  test('navigates to patient dashboard after successful login', async ({ page }) => {
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({ status: 200, json: { token: PATIENT_TOKEN } }),
    );
    await mockBaseRoutes(page);

    await page.goto('/login');
    await page.getByLabel('Email').fill('patient@test.com');
    await page.getByLabel('Password').fill('Test1234!');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
  });

  test('shows error message on invalid credentials', async ({ page }) => {
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({ status: 401, json: { error: 'Invalid email or password' } }),
    );

    await page.goto('/login');
    await page.getByLabel('Email').fill('bad@test.com');
    await page.getByLabel('Password').fill('wrongpass');
    await page.getByRole('button', { name: 'Sign in' }).click();

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
    const token = fakeJwt({
      sub: '2',
      email: 'new@test.com',
      given_name: 'New',
      family_name: 'User',
      role: 'Patient',
      clinic_id: 'c-001',
      exp: 9_999_999_999,
    });

    await page.route('**/api/v1/auth/register', (route) =>
      route.fulfill({ status: 200, json: { token } }),
    );
    await mockBaseRoutes(page);

    await page.goto('/register');
    await page.getByLabel('First name').fill('New');
    await page.getByLabel('Last name').fill('User');
    await page.getByLabel('Email').fill('new@test.com');
    await page.getByLabel('Password').fill('Test1234!');
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
  });
});

test.describe('Logout', () => {
  test('user can logout and is redirected to login', async ({ page }) => {
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({ status: 200, json: { token: PATIENT_TOKEN } }),
    );
    await mockBaseRoutes(page);

    await page.goto('/login');
    await page.getByLabel('Email').fill('patient@test.com');
    await page.getByLabel('Password').fill('Test1234!');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();

    await page.getByRole('button', { name: 'Sign out' }).click();
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe('Protected routes', () => {
  test('unauthenticated visit to /dashboard redirects to /login', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });
});
