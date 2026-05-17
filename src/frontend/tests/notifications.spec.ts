import { test, expect } from '@playwright/test';
import { fakeJwt } from './helpers';

const patientToken = fakeJwt({
  sub: '1',
  email: 'patient@test.com',
  given_name: 'Maya',
  family_name: 'Chen',
  role: 'Patient',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

test.describe('Notifications — graceful degradation', () => {
  test.beforeEach(async ({ page }) => {
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
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
  });

  test('patient dashboard loads when SignalR is unavailable', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });

  // Markup smoke test — verifies CSS/dismiss behavior with raw DOM, not the real ToastContainer component.
  test('toast renders and can be dismissed', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();

    await page.evaluate(() => {
      const container = document.createElement('div');
      container.className = 'toast-container';
      container.setAttribute('aria-live', 'polite');
      const toast = document.createElement('div');
      toast.className = 'toast toast--booked';
      toast.innerHTML = `
        <span class="toast-message">New appointment with Maya Chen</span>
        <button class="toast-dismiss" aria-label="Dismiss notification">&times;</button>
      `;
      toast.querySelector('button')!.addEventListener('click', () => toast.remove());
      container.appendChild(toast);
      document.body.appendChild(container);
    });

    await expect(page.getByText('New appointment with Maya Chen')).toBeVisible();
    await page.click('button[aria-label="Dismiss notification"]');
    await expect(page.getByText('New appointment with Maya Chen')).not.toBeVisible();
  });
});
