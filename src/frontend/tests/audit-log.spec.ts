import { test, expect } from '@playwright/test';
import { CLINIC_ADMIN_TOKEN, PATIENT_TOKEN, loginAs, mockBaseRoutes, mockAdminRoutes } from './helpers';

const sampleLogs = [
  {
    id: 'a1',
    timestamp: '2026-05-19T10:00:00Z',
    actorId: 'u-1',
    actorName: 'Maya Chen',
    action: 'BookAppointment',
    targetId: 'apt-1234-5678',
    metadata: '{"DoctorId":"d-001","Reason":"Annual checkup","ScheduledAt":"2026-05-20T10:00:00Z"}',
    ipAddress: '192.168.1.42',
  },
  {
    id: 'a2',
    timestamp: '2026-05-19T09:30:00Z',
    actorId: 'u-2',
    actorName: 'Dr. Anika Volpe',
    action: 'IssuePrescription',
    targetId: 'rx-9876-5432',
    metadata: '{"DrugName":"Amoxicillin","Dosage":"500mg","PatientId":"[REDACTED]"}',
    ipAddress: '10.0.0.15',
  },
  {
    id: 'a3',
    timestamp: '2026-05-19T09:00:00Z',
    actorId: 'a-1',
    actorName: 'Lior Adler',
    action: 'CreateDoctor',
    targetId: 'd-002',
    metadata: '{"FirstName":"Tomás","LastName":"Reyes","Password":"[REDACTED]"}',
    ipAddress: '172.16.0.1',
  },
  {
    id: 'a4',
    timestamp: '2026-05-18T15:00:00Z',
    actorId: 'u-3',
    actorName: 'Elena Kowalski',
    action: 'CancelAppointment',
    targetId: 'apt-2222-3333',
    metadata: null,
    ipAddress: '192.168.1.100',
  },
];

async function mockAuditRoutes(
  page: import('@playwright/test').Page,
  logs = sampleLogs,
  totalCount?: number,
) {
  await page.route('**/api/v1/audit-logs/export', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({
        status: 200,
        headers: { 'Content-Type': 'text/csv' },
        body: 'Timestamp,Actor,Action,Target,IP\n2026-05-19T10:00:00Z,Maya Chen,BookAppointment,apt-1234-5678,192.168.1.42\n',
      });
    }
    return route.continue();
  });

  await page.route('**/api/v1/audit-logs**', (route) => {
    if (route.request().url().includes('/export')) return route.fallback();
    const url = new URL(route.request().url());
    const action = url.searchParams.get('action');
    let filtered = [...logs];
    if (action) filtered = filtered.filter((l) => l.action.toLowerCase().includes(action.toLowerCase()));
    return route.fulfill({
      status: 200,
      json: { logs: filtered, totalCount: totalCount ?? filtered.length },
    });
  });
}

test.describe('Audit Log Page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, CLINIC_ADMIN_TOKEN);
    await mockBaseRoutes(page, {
      features: [{ featureKey: 'audit_trail', isEnabled: true }],
    });
    await mockAdminRoutes(page);
    await mockAuditRoutes(page);
  });

  test('admin navigates to audit log page and sees table', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Audit log' }).click();

    await expect(page.getByRole('heading', { name: 'Audit Trail' })).toBeVisible();
    await expect(page.getByText('Maya Chen')).toBeVisible();
    await expect(page.getByText('BookAppointment')).toBeVisible();
    await expect(page.getByText('Dr. Anika Volpe')).toBeVisible();
    await expect(page.getByText('IssuePrescription')).toBeVisible();
    await expect(page.getByText('192.168.1.42')).toBeVisible();
  });

  test('action filter narrows results', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Audit log' }).click();
    await expect(page.getByText('Maya Chen')).toBeVisible();

    await page.getByPlaceholder('Filter by action…').fill('Cancel');
    await expect(page.getByText('CancelAppointment')).toBeVisible();
    await expect(page.getByText('BookAppointment')).not.toBeVisible();
  });

  test('date filters are present and functional', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Audit log' }).click();
    await expect(page.getByText('Maya Chen')).toBeVisible();

    const fromInput = page.getByLabel('From date');
    const toInput = page.getByLabel('To date');
    await expect(fromInput).toBeVisible();
    await expect(toInput).toBeVisible();

    await fromInput.fill('2026-05-19');
    await expect(page.getByText('Maya Chen')).toBeVisible();
  });

  test('clicking row expands metadata panel', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Audit log' }).click();
    await expect(page.getByText('BookAppointment')).toBeVisible();

    await page.getByText('BookAppointment').click();
    await expect(page.getByText('"DoctorId"')).toBeVisible();
    await expect(page.getByText('"Annual checkup"')).toBeVisible();
  });

  test('export CSV calls export endpoint', async ({ page }) => {
    let exportCalled = false;
    await page.route('**/api/v1/audit-logs/export**', (route) => {
      exportCalled = true;
      return route.fulfill({
        status: 200,
        headers: { 'Content-Type': 'text/csv' },
        body: 'Timestamp,Actor,Action\n',
      });
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Audit log' }).click();
    await expect(page.getByText('Maya Chen')).toBeVisible();

    await page.getByRole('button', { name: /export csv/i }).click();
    await expect(page.getByRole('button', { name: /export csv/i })).toBeVisible();
    expect(exportCalled).toBe(true);
  });

  test('non-admin user cannot see audit log tab', async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await page.goto('/dashboard');

    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Audit log' })).not.toBeVisible();
  });
});
