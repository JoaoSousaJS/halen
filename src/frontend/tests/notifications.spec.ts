import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, loginAs, mockBaseRoutes } from './helpers';

test.describe('Notifications — graceful degradation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
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
