import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockDoctorRoutes } from './helpers';

const mockSearchDoctors = [
  {
    id: 'doc-1',
    name: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    yearsOfExperience: 20,
    languages: ['English'],
    nextAvailableSlot: { startUtc: '2027-01-15T09:00:00Z', dayOfWeek: 'Thursday' },
  },
  {
    id: 'doc-2',
    name: 'Dr. Grey',
    specialty: 'Surgery',
    consultationFee: 200,
    yearsOfExperience: 8,
    languages: ['English', 'Spanish'],
    nextAvailableSlot: null,
  },
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
    paymentStatus: 'Authorized',
    paymentAmount: 150,
  },
];

// ── Patient Dashboard ─────────────────────────────────────────────────────

test.describe('Patient Dashboard — Appointments', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, {
      searchDoctors: mockSearchDoctors,
      specialties: ['Diagnostics', 'Surgery'],
    });
  });

  test('shows booking form with doctor search', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByPlaceholder('Search doctors...')).toBeVisible();
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });

  test('shows empty state when no appointments', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('No appointments yet')).toBeVisible();
  });

  test('searches and selects a doctor', async ({ page }) => {
    await page.goto('/dashboard');

    await expect(page.getByText('Dr. House')).toBeVisible();
    await page.getByRole('button', { name: 'Select Dr. House' }).click();

    await expect(page.getByText('Dr. House — Diagnostics')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Change' })).toBeVisible();
  });

  test('filters doctors by specialty', async ({ page }) => {
    await page.route('**/api/v1/doctors/search**', (route) => {
      const url = new URL(route.request().url());
      const specialty = url.searchParams.get('specialty');
      const filtered = specialty
        ? mockSearchDoctors.filter((d) => d.specialty === specialty)
        : mockSearchDoctors;
      return route.fulfill({
        status: 200,
        json: { doctors: filtered, totalCount: filtered.length },
      });
    });

    await page.goto('/dashboard');
    await expect(page.getByText('Dr. House')).toBeVisible();
    await expect(page.getByText('Dr. Grey')).toBeVisible();

    await page.getByRole('button', { name: /all specialties/i }).click();
    await page.getByRole('option', { name: 'Surgery' }).click();

    await expect(page.getByText('Dr. Grey')).toBeVisible();
    await expect(page.getByText('Dr. House')).not.toBeVisible();
  });

  test('changes selected doctor', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Select Dr. House' }).click();
    await expect(page.getByText('Dr. House — Diagnostics')).toBeVisible();

    await page.getByRole('button', { name: 'Change' }).click();

    await expect(page.getByPlaceholder('Search doctors...')).toBeVisible();
    await expect(page.getByText('Dr. House — Diagnostics')).not.toBeVisible();
  });

  test('books an appointment with payment', async ({ page }) => {
    await page.route('**/api/v1/availability/doc-1', (route) => {
      if (!route.request().url().includes('/slots')) {
        return route.fulfill({ status: 200, json: { windows: [{ id: 'w1', dayOfWeek: 'Thursday', startTime: '09:00', endTime: '12:00', slotDurationMinutes: 20 }] } });
      }
      return route.fallback();
    });
    await page.route('**/api/v1/availability/doc-1/slots**', (route) =>
      route.fulfill({ status: 200, json: { slots: [
        { startUtc: '2027-01-15T09:00:00Z', startLocal: '09:00', isAvailable: true },
        { startUtc: '2027-01-15T09:20:00Z', startLocal: '09:20', isAvailable: true },
      ] } }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { appointmentId: 'new-appt-1', paymentStatus: 'Authorized' } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await page.getByRole('button', { name: 'Select Dr. House' }).click();
    await page.getByLabel('Date').fill('2027-01-15');
    await page.getByRole('button', { name: /select time slot 09:00/i }).click();
    await page.getByLabel('Reason for visit').fill('Regular checkup');

    await expect(page.getByTestId('payment-summary')).toContainText('$150');
    await expect(page.getByRole('button', { name: /confirm & pay \$150/i })).toBeVisible();

    await page.getByRole('button', { name: /confirm & pay/i }).click();

    await expect(page.getByText(/appointment booked/i)).toBeVisible();
  });

  test('shows booking error on conflict', async ({ page }) => {
    await page.route('**/api/v1/availability/doc-1', (route) => {
      if (!route.request().url().includes('/slots')) {
        return route.fulfill({ status: 200, json: { windows: [{ id: 'w1', dayOfWeek: 'Thursday', startTime: '09:00', endTime: '12:00', slotDurationMinutes: 20 }] } });
      }
      return route.fallback();
    });
    await page.route('**/api/v1/availability/doc-1/slots**', (route) =>
      route.fulfill({ status: 200, json: { slots: [
        { startUtc: '2027-01-15T09:00:00Z', startLocal: '09:00', isAvailable: true },
      ] } }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 400, json: { error: 'This time slot is not available' } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await page.getByRole('button', { name: 'Select Dr. House' }).click();
    await page.getByLabel('Date').fill('2027-01-15');
    await page.getByRole('button', { name: /select time slot 09:00/i }).click();
    await page.getByLabel('Reason for visit').fill('Checkup');
    await page.getByRole('button', { name: /confirm & pay/i }).click();

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

    await expect(page.locator('.appt-card').getByText('Dr. House')).toBeVisible();
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
          json: {
            appointments: [{ ...mockAppointments[0], status: 'Cancelled', paymentStatus: 'Refunded', paymentAmount: 150 }],
            totalCount: 1,
          },
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
    await expect(page.getByText(/refunded/i)).toBeVisible();
  });
});

// ── Payment Status Badges ───────────────────────────────────────────────────

test.describe('Patient Dashboard — Payment Statuses', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, { searchDoctors: mockSearchDoctors });
  });

  test('shows "Payment held" badge for authorized payments', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().url().includes('/appointments/')) return route.fallback();
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          json: {
            appointments: [{ ...mockAppointments[0], paymentStatus: 'Authorized', paymentAmount: 150 }],
            totalCount: 1,
          },
        });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');
    await expect(page.getByText(/payment held.*\$150/i)).toBeVisible();
  });

  test('shows "Paid" badge for captured payments', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().url().includes('/appointments/')) return route.fallback();
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          json: {
            appointments: [{ ...mockAppointments[0], status: 'Completed', paymentStatus: 'Captured', paymentAmount: 150 }],
            totalCount: 1,
          },
        });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');
    await expect(page.getByText(/paid.*\$150/i)).toBeVisible();
  });

  test('shows "Refunded" badge for refunded payments', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().url().includes('/appointments/')) return route.fallback();
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          json: {
            appointments: [{ ...mockAppointments[0], status: 'Cancelled', paymentStatus: 'Refunded', paymentAmount: 150 }],
            totalCount: 1,
          },
        });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');
    await expect(page.getByText(/refunded.*\$150/i)).toBeVisible();
  });

  test('shows "Payment failed" badge for failed payments', async ({ page }) => {
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().url().includes('/appointments/')) return route.fallback();
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          json: {
            appointments: [{ ...mockAppointments[0], paymentStatus: 'Failed', paymentAmount: null }],
            totalCount: 1,
          },
        });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');
    await expect(page.getByText(/payment failed/i)).toBeVisible();
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
