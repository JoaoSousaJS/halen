import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockDoctorRoutes } from './helpers';

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

const mockPrescriptions = [
  {
    id: 'rx-1',
    drugName: 'Amoxicillin',
    dosage: '500mg',
    frequency: 'Twice daily',
    refillsRemaining: 3,
    status: 'Active',
    pharmacyName: 'CVS Pharmacy',
    doctorName: 'Dr. House',
    patientName: 'Maya Chen',
    createdAt: new Date(Date.now() - 86_400_000).toISOString(),
  },
];

// ── Doctor Dashboard — Prescriptions ────────────────────────────────────────

test.describe('Doctor Dashboard — Prescriptions', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: true },
      ],
      appointments: mockAppointments,
    });
    await mockDoctorRoutes(page, undefined, undefined, [
      { patientId: 'patient-1', name: 'Maya Chen' },
    ]);
  });

  test('shows prescriptions list', async ({ page }) => {
    await page.route('**/api/v1/prescriptions', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: mockPrescriptions });
      }
      return route.continue();
    });

    await page.goto('/dashboard');

    await expect(page.getByText('Recent prescriptions')).toBeVisible();
    const rxList = page.locator('.doc-rx-list');
    await expect(rxList.getByText('Amoxicillin')).toBeVisible();
    await expect(rxList.getByText('500mg')).toBeVisible();
    await expect(rxList.getByText('Maya Chen')).toBeVisible();
  });

  test('issues a prescription successfully', async ({ page }) => {
    await page.route('**/api/v1/prescriptions', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { prescriptionId: 'new-rx-1' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');

    await expect(page.getByRole('heading', { name: 'Issue prescription' })).toBeVisible();

    await page.getByLabel('Patient').selectOption('patient-1');
    await page.getByPlaceholder('e.g. Amoxicillin').fill('Metformin');
    await page.getByPlaceholder('e.g. 500mg').fill('1000mg');
    await page.getByPlaceholder('e.g. Twice daily').fill('Once daily');
    await page.getByRole('button', { name: 'Issue prescription' }).click();

    await expect(page.getByText('Prescription issued!')).toBeVisible();
  });

  test('cancels an active prescription', async ({ page }) => {
    let getCalls = 0;
    await page.route('**/api/v1/prescriptions', (route) => {
      if (route.request().method() === 'GET') {
        getCalls++;
        if (getCalls === 1) {
          return route.fulfill({ status: 200, json: mockPrescriptions });
        }
        return route.fulfill({
          status: 200,
          json: [{ ...mockPrescriptions[0], status: 'Cancelled' }],
        });
      }
      return route.continue();
    });
    await page.route('**/api/v1/prescriptions/rx-1/cancel', (route) =>
      route.fulfill({ status: 200 }),
    );

    await page.goto('/dashboard');
    await expect(page.getByText('Recent prescriptions')).toBeVisible();
    await page.getByRole('button', { name: 'Cancel prescription for Maya Chen' }).click();

    await expect(page.getByText('Cancelled').first()).toBeVisible();
  });

  test('shows error when prescription issuance fails', async ({ page }) => {
    await page.route('**/api/v1/prescriptions', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 400, json: { error: 'Patient not found' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: 'Issue prescription' })).toBeVisible();

    await page.getByLabel('Patient').selectOption('patient-1');
    await page.getByPlaceholder('e.g. Amoxicillin').fill('Metformin');
    await page.getByPlaceholder('e.g. 500mg').fill('1000mg');
    await page.getByPlaceholder('e.g. Twice daily').fill('Once daily');
    await page.getByRole('button', { name: 'Issue prescription' }).click();

    await expect(page.getByText('Patient not found')).toBeVisible();
  });

  test('shows empty prescriptions state', async ({ page }) => {
    await page.route('**/api/v1/prescriptions', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await expect(page.getByText('No prescriptions issued yet.')).toBeVisible();
  });
});

// ── Patient Dashboard — Prescriptions ───────────────────────────────────────

test.describe('Patient Dashboard — Prescriptions', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
  });

  test('shows prescriptions from doctors', async ({ page }) => {
    await page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: mockPrescriptions }),
    );

    await page.goto('/dashboard');

    await expect(page.getByText('Your prescriptions')).toBeVisible();
    await expect(page.getByText('Amoxicillin')).toBeVisible();
    await expect(page.getByText('Prescribed by: Dr. House')).toBeVisible();
    await expect(page.getByText('Refills remaining: 3')).toBeVisible();
  });

  test('shows empty prescriptions state', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('No prescriptions yet.')).toBeVisible();
  });
});
