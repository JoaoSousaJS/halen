import type { Page } from '@playwright/test';

// ── JWT Helper ────────────────────────────────────────────────────────────────

export function fakeJwt(payload: object): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64');
  const body = Buffer.from(JSON.stringify(payload)).toString('base64');
  return `${header}.${body}.fake-sig`;
}

// ── Pre-built Tokens ──────────────────────────────────────────────────────────

export const PATIENT_TOKEN = fakeJwt({
  sub: '1',
  email: 'patient@test.com',
  given_name: 'Maya',
  family_name: 'Chen',
  role: 'Patient',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

export const DOCTOR_TOKEN = fakeJwt({
  sub: '2',
  email: 'doctor@test.com',
  given_name: 'Gregory',
  family_name: 'House',
  role: 'Doctor',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

export const CLINIC_ADMIN_TOKEN = fakeJwt({
  sub: 'a-001',
  email: 'admin@test.com',
  given_name: 'Lior',
  family_name: 'Adler',
  role: 'ClinicAdmin',
  clinic_id: 'c-001',
  exp: 9_999_999_999,
});

export const PLATFORM_ADMIN_TOKEN = fakeJwt({
  sub: 'pa-001',
  email: 'platform@halen.dev',
  given_name: 'Platform',
  family_name: 'Admin',
  role: 'PlatformAdmin',
  clinic_id: 'c-root',
  exp: 9_999_999_999,
});

// ── Login Helper ──────────────────────────────────────────────────────────────

export async function loginAs(page: Page, token: string): Promise<void> {
  await page.addInitScript((t: string) => {
    localStorage.setItem('token', t);
  }, token);
}

// ── Route Mocking Options ─────────────────────────────────────────────────────

export interface MockBaseOptions {
  /** Features returned by /me/features. Defaults to prescriptions enabled. */
  features?: Array<{ featureKey: string; isEnabled: boolean }>;
  /** Appointments returned by GET /appointments. Defaults to []. */
  appointments?: unknown[];
  /** Prescriptions returned by GET /prescriptions. Defaults to []. */
  prescriptions?: unknown[];
  /** Doctors returned by GET /appointments/doctors. Defaults to []. */
  doctors?: unknown[];
  /** Doctors returned by GET /doctors/search. Defaults to []. */
  searchDoctors?: unknown[];
  /** Specialties returned by GET /doctors/specialties. Defaults to []. */
  specialties?: string[];
  /** Doctor reviews returned by GET /doctor/reviews. Defaults to undefined (not mocked). */
  myReviews?: unknown;
  /** Moderation queue returned by GET /admin/reviews/moderation. Defaults to undefined (not mocked). */
  moderationQueue?: unknown;
}

/**
 * Intercepts common routes that virtually every dashboard test needs:
 * - hubs/** (SignalR) — aborted
 * - /api/v1/me/features
 * - /api/v1/appointments (GET only)
 * - /api/v1/appointments/doctors
 * - /api/v1/prescriptions (GET only)
 */
export async function mockBaseRoutes(page: Page, options: MockBaseOptions = {}): Promise<void> {
  const {
    features = [{ featureKey: 'prescriptions', isEnabled: true }],
    appointments = [],
    prescriptions = [],
    doctors = [],
    searchDoctors = [],
    specialties = [],
    myReviews,
    moderationQueue,
  } = options;

  await page.route('**/hubs/**', (route) => route.abort());
  await page.route('**/api/v1/me/features', (route) =>
    route.fulfill({ status: 200, json: features }),
  );
  await page.route('**/api/v1/appointments/doctors', (route) =>
    route.fulfill({ status: 200, json: { doctors, totalCount: doctors.length } }),
  );
  await page.route('**/api/v1/doctors/search**', (route) =>
    route.fulfill({ status: 200, json: { doctors: searchDoctors, totalCount: searchDoctors.length } }),
  );
  await page.route('**/api/v1/doctors/specialties', (route) =>
    route.fulfill({ status: 200, json: { specialties } }),
  );
  await page.route('**/api/v1/appointments', (route) => {
    // Skip if this is actually /appointments/doctors (glob matches too broadly)
    if (route.request().url().includes('/appointments/doctors')) {
      return route.fallback();
    }
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, json: { appointments, totalCount: appointments.length } });
    }
    return route.continue();
  });
  if (myReviews !== undefined) {
    await page.route('**/api/v1/doctor/reviews**', (route) =>
      route.fulfill({ status: 200, json: myReviews }),
    );
  }

  if (moderationQueue !== undefined) {
    await page.route('**/api/v1/admin/reviews/moderation**', (route) =>
      route.fulfill({ status: 200, json: moderationQueue }),
    );
  }

  await page.route('**/api/v1/prescriptions', (route) => {
    // Skip if this is a sub-path like /prescriptions/rx-1/cancel
    if (/\/prescriptions\/[^?]/.test(route.request().url())) {
      return route.fallback();
    }
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, json: prescriptions });
    }
    return route.continue();
  });
}

/**
 * Adds doctor-specific mocks on top of base routes:
 * - /api/v1/doctor/kyc/status
 *
 * Call mockBaseRoutes first, then this.
 */
export async function mockDoctorRoutes(
  page: Page,
  kycStatus: object = { status: 'Approved', submittedAt: null, lastRejectionReason: null, documents: [] },
): Promise<void> {
  await page.route('**/api/v1/doctor/kyc/status', (route) =>
    route.fulfill({ status: 200, json: kycStatus }),
  );
}

/**
 * Adds admin-specific mocks on top of base routes:
 * - /api/v1/admin/users**
 *
 * Call mockBaseRoutes first, then this.
 */
export async function mockAdminRoutes(
  page: Page,
  users: unknown[] = [],
): Promise<void> {
  await page.route('**/api/v1/admin/users**', (route) =>
    route.fulfill({ status: 200, json: { users, totalCount: users.length } }),
  );
}
