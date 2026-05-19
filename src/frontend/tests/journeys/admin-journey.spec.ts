import { test, expect } from '@playwright/test';
import { CLINIC_ADMIN_TOKEN, loginAs, mockBaseRoutes, mockAdminRoutes } from '../helpers';

/**
 * Admin Journey E2E Test
 *
 * Simulates a realistic admin workflow across multiple features:
 * Login -> Create Doctor -> View Users -> Navigate to Audit Trail
 * -> See audit entries for actions taken -> Export CSV
 *
 * All API routes are mocked — this tests frontend integration, not backend.
 */

const mockUsers = [
  { id: 'd-001', name: 'Dr. Anika Volpe', role: 'Doctor', status: 'Active', plan: null, lastLoginAt: '2026-05-19T08:00:00Z', isFlagged: false },
  { id: 'p-001', name: 'Maya Chen', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: '2026-05-19T09:00:00Z', isFlagged: false },
];

const auditLogs = [
  {
    id: 'al-1',
    timestamp: '2026-05-19T10:05:00Z',
    actorId: 'a-001',
    actorName: 'Lior Adler',
    action: 'CreateDoctor',
    targetId: 'd-new-001',
    metadata: '{"FirstName":"Gregory","LastName":"House","Password":"[REDACTED]"}',
    ipAddress: '172.16.0.1',
  },
  {
    id: 'al-2',
    timestamp: '2026-05-19T10:10:00Z',
    actorId: 'p-001',
    actorName: 'Maya Chen',
    action: 'BookAppointment',
    targetId: 'apt-5555',
    metadata: '{"DoctorId":"d-new-001","Reason":"Diagnostics consult","ScheduledAt":"2026-05-25T14:00:00Z"}',
    ipAddress: '192.168.1.42',
  },
  {
    id: 'al-3',
    timestamp: '2026-05-19T10:00:00Z',
    actorId: 'a-001',
    actorName: 'Lior Adler',
    action: 'LoginSuccess',
    targetId: 'a-001',
    metadata: null,
    ipAddress: '172.16.0.1',
  },
];

test.describe('Admin Journey — Create Doctor → Audit Trail', () => {
  test('admin creates doctor, views users, checks audit trail, exports CSV', async ({ page }) => {
    // ── Step 1: Login as Clinic Admin ──────────────────────────────────────

    await loginAs(page, CLINIC_ADMIN_TOKEN);
    await mockBaseRoutes(page, {
      features: [{ featureKey: 'audit_trail', isEnabled: true }],
    });
    await mockAdminRoutes(page, mockUsers);

    await page.route('**/api/v1/admin/doctors', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200, json: { doctorId: 'd-new-001' } });
      }
      return route.continue();
    });

    await page.route('**/api/v1/audit-logs/export', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          headers: { 'Content-Type': 'text/csv' },
          body: 'Timestamp,Actor,Action,Target,IP\n2026-05-19T10:05:00Z,Lior Adler,CreateDoctor,d-new-001,172.16.0.1\n2026-05-19T10:10:00Z,Maya Chen,BookAppointment,apt-5555,192.168.1.42\n',
        });
      }
      return route.continue();
    });

    await page.route('**/api/v1/audit-logs**', (route) => {
      if (route.request().url().includes('/export')) return route.fallback();
      const url = new URL(route.request().url());
      const action = url.searchParams.get('action');
      let filtered = [...auditLogs];
      if (action) filtered = filtered.filter((l) => l.action.toLowerCase().includes(action.toLowerCase()));
      return route.fulfill({
        status: 200,
        json: { logs: filtered, totalCount: filtered.length },
      });
    });

    await page.goto('/dashboard');

    // ── Step 2: Verify Users tab loads ─────────────────────────────────────

    await expect(page.getByText('Dr. Anika Volpe')).toBeVisible();
    await expect(page.getByText('Maya Chen')).toBeVisible();

    // ── Step 3: Create a new doctor ───────────────────────────────────────

    await page.getByRole('button', { name: 'Create doctor' }).click();
    await expect(page.getByText('doctor account.')).toBeVisible();

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

    // ── Step 4: Navigate to Audit Trail ───────────────────────────────────

    await page.getByRole('button', { name: 'Audit log' }).click();
    await expect(page.getByRole('heading', { name: 'Audit Trail' })).toBeVisible();

    // ── Step 5: Verify audit entries ──────────────────────────────────────

    await expect(page.getByText('CreateDoctor')).toBeVisible();
    await expect(page.getByText('BookAppointment')).toBeVisible();
    await expect(page.getByText('LoginSuccess')).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Lior Adler' }).first()).toBeVisible();

    // ── Step 6: Expand a row to see metadata ──────────────────────────────

    await page.getByText('BookAppointment').click();
    await expect(page.getByText('"Diagnostics consult"')).toBeVisible();
    await expect(page.getByText('"d-new-001"')).toBeVisible();

    // ── Step 7: Filter to booking actions ─────────────────────────────────

    await page.getByPlaceholder('Filter by action…').fill('Book');
    await expect(page.getByText('BookAppointment')).toBeVisible();
    await expect(page.getByText('CreateDoctor')).not.toBeVisible();

    // ── Step 8: Export CSV ────────────────────────────────────────────────

    await page.getByPlaceholder('Filter by action…').clear();
    await expect(page.getByText('CreateDoctor')).toBeVisible();

    let exportCalled = false;
    await page.route('**/api/v1/audit-logs/export**', (route) => {
      exportCalled = true;
      return route.fulfill({
        status: 200,
        headers: { 'Content-Type': 'text/csv' },
        body: 'Timestamp,Actor,Action\n',
      });
    });

    await page.getByRole('button', { name: /export csv/i }).click();
    await expect(page.getByRole('button', { name: /export csv/i })).toBeVisible();
    expect(exportCalled).toBe(true);
  });
});
