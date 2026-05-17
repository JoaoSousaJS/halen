import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockDoctorRoutes } from './helpers';

const APPOINTMENT_ID = 'apt-e2e-1';

const mockRoom = {
  id: 'room-1',
  appointmentId: APPOINTMENT_ID,
  roomCode: 'VC-TEST',
  status: 'Waiting',
  doctorName: 'Gregory House',
  patientName: 'Maya Chen',
  reason: 'Recurring headache',
  durationMinutes: 20,
  notes: null,
  startedAt: null,
  endedAt: null,
  doctorJoinedAt: null,
  patientJoinedAt: null,
};

async function mockConsultationRoutes(page: import('@playwright/test').Page) {
  await page.route(`**/api/v1/consultations/${APPOINTMENT_ID}`, (route) =>
    route.fulfill({ status: 200, json: mockRoom }),
  );
  await page.route('**/hubs/consultation**', (route) => route.abort());
}

// ── Patient Pre-Call Lobby ──────────────────────────────────────────────

test.describe('Video Consultation — Patient Lobby', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await mockConsultationRoutes(page);
  });

  test('shows lobby with appointment details', async ({ page }) => {
    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.getByRole('heading', { name: /appointment brief/i })).toBeVisible();
    await expect(page.getByText('Doctor: Gregory House')).toBeVisible();
    await expect(page.getByText('Patient: Maya Chen')).toBeVisible();
    await expect(page.getByText('Reason: Recurring headache')).toBeVisible();
  });

  test('shows "Join consult" button for patient', async ({ page }) => {
    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.getByRole('button', { name: /join consult/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /admit/i })).not.toBeVisible();
  });

  test('shows self-preview tile with patient name', async ({ page }) => {
    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.locator('.vc-tile').getByText('MC')).toBeVisible();
  });
});

// ── Doctor Pre-Call Lobby ────────────────────────────────────────────────

test.describe('Video Consultation — Doctor Lobby', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page);
    await mockDoctorRoutes(page);
    await mockConsultationRoutes(page);
  });

  test('shows "Admit & start consult" for doctor', async ({ page }) => {
    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.getByRole('button', { name: /admit & start consult/i })).toBeVisible();
  });

  test('shows self-preview tile with doctor initials', async ({ page }) => {
    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.locator('.vc-tile').getByText('GH')).toBeVisible();
  });
});

// ── Post-Call Wrap Up ───────────────────────────────────────────────────

test.describe('Video Consultation — Post-Call', () => {
  test('patient sees summary after consultation ends', async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await page.route(`**/api/v1/consultations/${APPOINTMENT_ID}`, (route) =>
      route.fulfill({
        status: 200,
        json: {
          ...mockRoom,
          status: 'Ended',
          endedAt: '2026-05-18T11:00:00Z',
          startedAt: '2026-05-18T10:40:00Z',
        },
      }),
    );
    await page.route('**/hubs/consultation**', (route) => route.abort());

    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.getByText(/consult complete/i)).toBeVisible();
    await expect(page.getByText('Gregory House')).toBeVisible();
    await expect(page.getByRole('button', { name: /done/i })).toBeVisible();
  });

  test('doctor sees finalize form after consultation ends', async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page);
    await mockDoctorRoutes(page);
    await page.route(`**/api/v1/consultations/${APPOINTMENT_ID}`, (route) =>
      route.fulfill({
        status: 200,
        json: {
          ...mockRoom,
          status: 'Ended',
          notes: 'Patient has recurring headache',
          endedAt: '2026-05-18T11:00:00Z',
          startedAt: '2026-05-18T10:40:00Z',
        },
      }),
    );
    await page.route('**/hubs/consultation**', (route) => route.abort());

    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.getByText(/save your consult/i)).toBeVisible();
    await expect(page.getByRole('textbox')).toBeVisible();
    await expect(page.getByRole('button', { name: /save/i })).toBeVisible();
  });
});

// ── Error State ──────────────────────────────────────────────────────────

test.describe('Video Consultation — Error Handling', () => {
  test('shows error when room fails to load', async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await page.route(`**/api/v1/consultations/${APPOINTMENT_ID}`, (route) =>
      route.fulfill({ status: 404, json: { error: 'Not found' } }),
    );
    await page.route('**/hubs/consultation**', (route) => route.abort());

    await page.goto(`/consultation/${APPOINTMENT_ID}`);

    await expect(page.getByText(/unable to load/i)).toBeVisible();
  });

  test('redirects to login when not authenticated', async ({ page }) => {
    await page.goto(`/consultation/${APPOINTMENT_ID}`);
    await expect(page).toHaveURL(/\/login/);
  });
});
