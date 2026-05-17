import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockDoctorRoutes } from './helpers';

// ── Mock Data ─────────────────────────────────────────────────────────────────

const mockDoctors = [
  { id: 'doc-1', name: 'Dr. House', specialty: 'Diagnostics', consultationFee: 150, yearsOfExperience: 20 },
];

const mockWindows = [
  { id: 'w-1', dayOfWeek: 'Monday', startTime: '09:00', endTime: '12:00', slotDurationMinutes: 20 },
  { id: 'w-2', dayOfWeek: 'Wednesday', startTime: '14:00', endTime: '17:00', slotDurationMinutes: 20 },
];

const mockSlots = [
  { startUtc: '2027-01-15T09:00:00Z', startLocal: '09:00', isAvailable: true },
  { startUtc: '2027-01-15T09:20:00Z', startLocal: '09:20', isAvailable: true },
  { startUtc: '2027-01-15T09:40:00Z', startLocal: '09:40', isAvailable: false },
  { startUtc: '2027-01-15T10:00:00Z', startLocal: '10:00', isAvailable: true },
];

// ── Doctor Availability Management ────────────────────────────────────────────

test.describe('Doctor — Availability Management', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: true },
      ],
    });
    await mockDoctorRoutes(page); // KYC approved by default
  });

  test('doctor sees availability editor on dashboard when KYC is approved', async ({ page }) => {
    await page.route('**/api/v1/availability/mine', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: { windows: mockWindows } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await expect(page.getByRole('heading', { name: /your availability/i })).toBeVisible();
    // Verify day cards are rendered
    await expect(page.getByRole('heading', { name: 'Monday' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Wednesday' })).toBeVisible();
    // Verify existing windows are shown
    await expect(page.getByText('09:00 - 12:00')).toBeVisible();
    await expect(page.getByText('14:00 - 17:00')).toBeVisible();
    // Verify save button is present
    await expect(page.getByRole('button', { name: 'Save availability' })).toBeVisible();
  });

  test('doctor adds a Monday 9-12 window and saves', async ({ page }) => {
    let putCalled = false;
    await page.route('**/api/v1/availability/mine', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: { windows: [] } });
      }
      if (route.request().method() === 'PUT') {
        putCalled = true;
        return route.fulfill({ status: 200, json: {} });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await expect(page.getByRole('heading', { name: /your availability/i })).toBeVisible();

    // Find the Monday card and click "+ Add window"
    const mondayCard = page.locator('.avail-day-card').filter({ hasText: 'Monday' });
    await mondayCard.getByRole('button', { name: '+ Add window' }).click();

    // Fill in start and end time
    await mondayCard.getByLabel('Start').fill('09:00');
    await mondayCard.getByLabel('End').fill('12:00');

    // Click "Add" to add the window
    await mondayCard.getByRole('button', { name: 'Add' }).click();

    // Verify the new chip appears
    await expect(mondayCard.getByText('09:00 - 12:00')).toBeVisible();

    // Save
    await page.getByRole('button', { name: 'Save availability' }).click();

    expect(putCalled).toBe(true);
  });

  test('doctor removes an existing window', async ({ page }) => {
    await page.route('**/api/v1/availability/mine', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: { windows: mockWindows } });
      }
      if (route.request().method() === 'PUT') {
        return route.fulfill({ status: 200, json: {} });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    await expect(page.getByRole('heading', { name: /your availability/i })).toBeVisible();

    // Verify the Monday window exists
    await expect(page.getByText('09:00 - 12:00')).toBeVisible();

    // Remove the Monday 09:00-12:00 window
    await page.getByRole('button', { name: 'Remove 09:00 - 12:00 on Monday' }).click();

    // Verify the chip is gone
    await expect(page.getByText('09:00 - 12:00')).not.toBeVisible();

    // Wednesday window should still be present
    await expect(page.getByText('14:00 - 17:00')).toBeVisible();
  });
});

// ── Patient Slot Booking ──────────────────────────────────────────────────────

test.describe('Patient — Slot Booking', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, { doctors: mockDoctors });
  });

  test('patient sees "hasn\'t set up schedule" when doctor has no availability', async ({ page }) => {
    await page.route('**/api/v1/availability/doc-1', (route) => {
      if (!route.request().url().includes('/slots')) {
        return route.fulfill({ status: 200, json: { windows: [] } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    // Select the doctor
    await page.getByLabel('Doctor').selectOption('doc-1');

    // Should show the "hasn't set up schedule" message
    await expect(page.getByText("This doctor hasn't set up their schedule yet.")).toBeVisible();

    // Date picker should NOT be visible
    await expect(page.getByLabel('Date')).not.toBeVisible();
  });

  test('patient selects doctor with availability, picks date, sees slots', async ({ page }) => {
    await page.route('**/api/v1/availability/doc-1', (route) => {
      if (!route.request().url().includes('/slots')) {
        return route.fulfill({ status: 200, json: { windows: mockWindows } });
      }
      return route.fallback();
    });
    await page.route('**/api/v1/availability/doc-1/slots**', (route) =>
      route.fulfill({ status: 200, json: { slots: mockSlots } }),
    );

    await page.goto('/dashboard');

    // Select doctor
    await page.getByLabel('Doctor').selectOption('doc-1');

    // Date field should appear
    await expect(page.getByLabel('Date')).toBeVisible();

    // Pick a date
    await page.getByLabel('Date').fill('2027-01-15');

    // Available slots should appear (only isAvailable === true)
    await expect(page.getByRole('button', { name: '09:00' })).toBeVisible();
    await expect(page.getByRole('button', { name: '09:20' })).toBeVisible();
    await expect(page.getByRole('button', { name: '10:00' })).toBeVisible();
    // Slot 09:40 is not available, so it should not appear
    await expect(page.getByRole('button', { name: '09:40' })).not.toBeVisible();
  });

  test('patient picks a slot and books successfully', async ({ page }) => {
    await page.route('**/api/v1/availability/doc-1', (route) => {
      if (!route.request().url().includes('/slots')) {
        return route.fulfill({ status: 200, json: { windows: mockWindows } });
      }
      return route.fallback();
    });
    await page.route('**/api/v1/availability/doc-1/slots**', (route) =>
      route.fulfill({ status: 200, json: { slots: mockSlots } }),
    );
    await page.route('**/api/v1/appointments', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { appointmentId: 'new-appt-1' } });
      }
      return route.fallback();
    });

    await page.goto('/dashboard');

    // Select doctor
    await page.getByLabel('Doctor').selectOption('doc-1');

    // Pick date
    await page.getByLabel('Date').fill('2027-01-15');

    // Pick a slot
    await page.getByRole('button', { name: '09:20' }).click();

    // Fill reason
    await page.getByLabel('Reason for visit').fill('Follow-up checkup');

    // Book
    await page.getByRole('button', { name: 'Book appointment' }).click();

    // Success message
    await expect(page.getByText('Appointment booked!')).toBeVisible();
  });
});
