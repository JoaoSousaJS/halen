import { test, expect } from '@playwright/test';
import { fakeJwt } from './helpers';

const patientToken = fakeJwt({
  sub: '1',
  email: 'patient@test.com',
  given_name: 'Maya',
  family_name: 'Chen',
  role: 'Patient',
  exp: 9_999_999_999,
});

const doctorToken = fakeJwt({
  sub: '2',
  email: 'doctor@test.com',
  given_name: 'Gregory',
  family_name: 'House',
  role: 'Doctor',
  exp: 9_999_999_999,
});

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

async function loginAsPatient(page: import('@playwright/test').Page) {
  await page.addInitScript((token: string) => {
    localStorage.setItem('token', token);
  }, patientToken);
}

async function loginAsDoctor(page: import('@playwright/test').Page) {
  await page.addInitScript((token: string) => {
    localStorage.setItem('token', token);
  }, doctorToken);
}

// ── Patient Dashboard ─────────────────────────────────────────────────────

test.describe('Patient Dashboard — Appointments', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsPatient(page);
    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
  });

  test('shows booking form with doctor selector', async ({ page }) => {
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: mockDoctors }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.locator('select')).toBeVisible();
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });

  test('books an appointment successfully', async ({ page }) => {
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: mockDoctors }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { appointmentId: 'new-appt-1' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');

    await page.selectOption('select', 'doc-1');
    await page.fill('input[type="datetime-local"]', '2027-01-15T10:00');
    await page.fill('textarea', 'Regular checkup');
    await page.click('button[type="submit"]');

    await expect(page.getByText('Appointment booked!')).toBeVisible();
  });

  test('shows booking error on conflict', async ({ page }) => {
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: mockDoctors }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 400, json: { error: 'This time slot is not available' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');

    await page.selectOption('select', 'doc-1');
    await page.fill('input[type="datetime-local"]', '2027-01-15T10:00');
    await page.fill('textarea', 'Checkup');
    await page.click('button[type="submit"]');

    await expect(page.getByText('This time slot is not available')).toBeVisible();
  });

  test('displays existing appointments', async ({ page }) => {
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: mockDoctors }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: mockAppointments });
      }
      return route.continue();
    });

    await page.goto('/dashboard');

    await expect(page.locator('.appt-card').getByText('Dr. House')).toBeVisible();
    await expect(page.getByText('Annual checkup')).toBeVisible();
    await expect(page.getByText('Scheduled')).toBeVisible();
  });

  test('cancels an appointment', async ({ page }) => {
    await page.route('**/api/v1/appointments/doctors', (route) =>
      route.fulfill({ status: 200, json: mockDoctors }),
    );

    let getCalls = 0;
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        getCalls++;
        if (getCalls === 1) {
          return route.fulfill({ status: 200, json: mockAppointments });
        }
        return route.fulfill({
          status: 200,
          json: [{ ...mockAppointments[0], status: 'Cancelled' }],
        });
      }
      return route.continue();
    });
    await page.route('**/api/v1/appointments/appt-1/cancel', (route) =>
      route.fulfill({ status: 200 }),
    );

    await page.goto('/dashboard');
    await page.click('button[aria-label="Cancel appointment with Dr. House"]');

    await expect(page.getByText('Cancelled')).toBeVisible();
  });
});

// ── Doctor Dashboard ──────────────────────────────────────────────────────

test.describe('Doctor Dashboard — Appointments', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDoctor(page);
    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
  });

  test('shows doctor schedule', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: mockAppointments });
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
          return route.fulfill({ status: 200, json: mockAppointments });
        }
        return route.fulfill({
          status: 200,
          json: [{ ...mockAppointments[0], status: 'Completed', notes: 'Patient is fine' }],
        });
      }
      return route.continue();
    });
    await page.route('**/api/v1/appointments/appt-1/complete', (route) =>
      route.fulfill({ status: 200 }),
    );

    await page.goto('/dashboard');
    await page.click('button:has-text("Complete")');
    await page.fill('textarea', 'Patient is fine');
    await page.click('button:has-text("Confirm")');

    await expect(page.getByText('Completed')).toBeVisible();
  });

  test('shows empty state when no appointments', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });
});
