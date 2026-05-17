import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockDoctorRoutes } from './helpers';

const mockDoctors = [
  { id: 'doc-1', name: 'Dr. House', specialty: 'Diagnostics', consultationFee: 150, yearsOfExperience: 20 },
];

const mockAppointments = [
  {
    id: 'appt-1',
    scheduledAt: new Date(Date.now() + 86_400_000).toISOString(),
    durationMinutes: 20,
    reason: 'Annual checkup',
    status: 'Scheduled',
    notes: null,
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'Maya Chen',
    patientId: 'patient-1',
  },
];

// ── Patient Dashboard ─────────────────────────────────────────────────────

test.describe('Patient Dashboard — Appointments', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, { doctors: mockDoctors });
  });

  test('shows booking form with doctor selector', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByLabel('Doctor')).toBeVisible();
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });

  test('shows empty state when no appointments', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });

  test('books an appointment successfully', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { appointmentId: 'new-appt-1' } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await page.getByLabel('Doctor').selectOption('doc-1');
    await page.getByLabel('Date & time').fill('2027-01-15T10:00');
    await page.getByLabel('Reason for visit').fill('Regular checkup');
    await page.getByRole('button', { name: 'Book appointment' }).click();

    await expect(page.getByText('Appointment booked!')).toBeVisible();
  });

  test('shows booking error on conflict', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 400, json: { error: 'This time slot is not available' } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await page.getByLabel('Doctor').selectOption('doc-1');
    await page.getByLabel('Date & time').fill('2027-01-15T10:00');
    await page.getByLabel('Reason for visit').fill('Checkup');
    await page.getByRole('button', { name: 'Book appointment' }).click();

    await expect(page.getByText('This time slot is not available')).toBeVisible();
  });

  test('displays existing appointments', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().url().includes('/appointments/')) return route.fallback();
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: { appointments: mockAppointments, totalCount: mockAppointments.length } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await expect(page.getByText('Dr. House', { exact: true })).toBeVisible();
    await expect(page.getByText('Annual checkup')).toBeVisible();
    await expect(page.getByText('Scheduled')).toBeVisible();
  });

  test('cancels an appointment', async ({ page }) => {
    let getCalls = 0;
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().url().includes('/appointments/')) return route.fallback();
      if (route.request().method() === 'GET') {
        getCalls++;
        if (getCalls === 1) {
          return route.fulfill({ status: 200, json: { appointments: mockAppointments, totalCount: mockAppointments.length } });
        }
        return route.fulfill({
          status: 200,
          json: { appointments: [{ ...mockAppointments[0], status: 'Cancelled' }], totalCount: 1 },
        });
      }
      return route.fallback();
    });
    await page.route('**/api/v1/appointments/appt-1/cancel', (route) =>
      route.fulfill({ status: 200 }),
    );

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Cancel appointment with Dr. House' }).click();

    await expect(page.getByText('Cancelled')).toBeVisible();
  });
});

// ── Doctor Dashboard ──────────────────────────────────────────────────────

test.describe('Doctor Dashboard — Appointments', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: true },
      ],
    });
    await mockDoctorRoutes(page);
  });

  test('shows doctor schedule', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: { appointments: mockAppointments, totalCount: mockAppointments.length } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /your.*schedule/i })).toBeVisible();
    await expect(page.locator('.appt-card').getByText('Maya Chen')).toBeVisible();
    await expect(page.getByText('Annual checkup')).toBeVisible();
  });

  test('completes an appointment with notes', async ({ page }) => {
    let getCalls = 0;
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        getCalls++;
        if (getCalls === 1) {
          return route.fulfill({ status: 200, json: { appointments: mockAppointments, totalCount: mockAppointments.length } });
        }
        return route.fulfill({
          status: 200,
          json: { appointments: [{ ...mockAppointments[0], status: 'Completed', notes: 'Patient is fine' }], totalCount: 1 },
        });
      }
      return route.continue();
    });
    await page.route('**/api/v1/appointments/appt-1/complete', (route) =>
      route.fulfill({ status: 200 }),
    );

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Complete appointment with Maya Chen' }).click();
    await page.getByPlaceholder('Session notes (optional)').fill('Patient is fine');
    await page.getByRole('button', { name: 'Confirm' }).click();

    await expect(page.getByText('Completed')).toBeVisible();
  });

  test('shows empty state when no appointments', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });
});
