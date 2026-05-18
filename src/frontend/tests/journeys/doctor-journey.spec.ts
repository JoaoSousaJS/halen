import { test, expect } from '@playwright/test';
import { DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockDoctorRoutes } from '../helpers';

/**
 * Doctor Journey E2E Test
 *
 * Simulates a realistic doctor workflow:
 * Login -> Dashboard -> View patient's medical records (via patient link)
 * -> Browse conditions, allergies, vitals, timeline
 *
 * All API routes are mocked — this tests frontend integration, not backend.
 */

const PATIENT_PROFILE_ID = 'pp-patient-1';

// ── Mock Data ────────────────────────────────────────────────────────────────

const mockAppointments = [
  {
    id: 'appt-1',
    scheduledAt: new Date(Date.now() + 86_400_000).toISOString(),
    durationMinutes: 30,
    reason: 'Follow-up on blood pressure',
    status: 'Scheduled',
    notes: null,
    doctorName: 'Dr. House',
    specialty: 'Diagnostics',
    consultationFee: 150,
    patientName: 'Maya Chen',
    patientId: 'patient-1',
    patientProfileId: PATIENT_PROFILE_ID,
    paymentStatus: 'Authorized',
    paymentAmount: 150,
  },
];

const mockPatientHeader = {
  patientProfileId: PATIENT_PROFILE_ID,
  patientName: 'Maya Chen',
  city: 'Austin, TX',
  allergyChips: ['Penicillin', 'Peanuts'],
  conditionChips: ['Hypertension', 'Asthma'],
};

const mockConditions = [
  {
    id: 'cond-1',
    icdCode: 'I10',
    icdDescription: 'Essential Hypertension',
    dateOfOnset: '2023-06-15',
    severity: 'Moderate',
    status: 'Active',
    clinicalNotes: 'Monitor blood pressure regularly.',
    addedBy: 'Dr. House',
    createdAt: '2024-01-10T10:00:00Z',
  },
  {
    id: 'cond-2',
    icdCode: 'J45',
    icdDescription: 'Asthma',
    dateOfOnset: '2024-01-12',
    severity: 'Mild',
    status: 'Active',
    clinicalNotes: null,
    addedBy: 'Maya Chen',
    createdAt: '2024-01-12T08:00:00Z',
  },
];

const mockAllergies = [
  {
    id: 'allergy-1',
    allergenName: 'Penicillin',
    reaction: 'Hives and swelling',
    severity: 'Severe',
    dateIdentified: '2022-03-10',
    isActive: true,
    addedBy: 'Dr. House',
    createdAt: '2024-01-10T10:00:00Z',
  },
  {
    id: 'allergy-2',
    allergenName: 'Peanuts',
    reaction: 'Throat swelling',
    severity: 'Severe',
    dateIdentified: null,
    isActive: true,
    addedBy: 'Maya Chen',
    createdAt: '2024-01-13T09:00:00Z',
  },
];

const mockTimeline = {
  entries: [
    {
      id: 'tl-1',
      type: 'Condition',
      occurredAt: '2024-01-12T08:00:00Z',
      title: 'Asthma',
      subtitle: 'ICD-10: J45 — Mild, Active',
      addedBy: 'Maya Chen',
    },
    {
      id: 'tl-2',
      type: 'Allergy',
      occurredAt: '2024-01-13T09:00:00Z',
      title: 'Peanuts allergy recorded',
      subtitle: 'Severe — Throat swelling',
      addedBy: 'Maya Chen',
    },
    {
      id: 'tl-3',
      type: 'Condition',
      occurredAt: '2024-01-10T10:00:00Z',
      title: 'Essential Hypertension',
      subtitle: 'ICD-10: I10 — Moderate, Active',
      addedBy: 'Dr. House',
    },
    {
      id: 'tl-4',
      type: 'Vital',
      occurredAt: '2024-01-08T14:00:00Z',
      title: 'Blood Pressure: 120/80 mmHg',
      subtitle: null,
      addedBy: 'Maya Chen',
    },
  ],
  totalCount: 4,
};

const mockSnapshot = {
  allergies: [
    { id: 'allergy-1', allergenName: 'Penicillin', reaction: 'Hives and swelling', severity: 'Severe' },
    { id: 'allergy-2', allergenName: 'Peanuts', reaction: 'Throat swelling', severity: 'Severe' },
  ],
  activeConditions: [
    { id: 'cond-1', icdDescription: 'Essential Hypertension', severity: 'Moderate' },
    { id: 'cond-2', icdDescription: 'Asthma', severity: 'Mild' },
  ],
  activeMedications: [
    { id: 'med-1', medicationName: 'Lisinopril', dosage: '10mg', frequency: 'Once daily', startDate: '2024-01-10' },
  ],
  familyHistory: [],
  latestVitals: {
    bloodPressure: { value: 120, secondaryValue: 80, unit: 'mmHg', measuredAt: '2024-01-08T14:00:00Z' },
    heartRate: { value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2024-01-08T14:00:00Z' },
    weight: null,
    spO2: { value: 98, secondaryValue: null, unit: '%', measuredAt: '2024-01-08T14:00:00Z' },
  },
  onboardingProgress: 4,
};

test.describe('Doctor Journey — View Patient Medical Records', () => {
  test('doctor logs in, views dashboard, then navigates to patient medical records', async ({ page }) => {
    // ── Step 1: Login as Doctor ────────────────────────────────────────────

    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({ status: 200, json: { token: DOCTOR_TOKEN } }),
    );
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'kyc', isEnabled: true },
        { featureKey: 'medical_records', isEnabled: true },
      ],
      appointments: mockAppointments,
    });
    await mockDoctorRoutes(page);

    // Mock prescriptions for the dashboard
    await page.route('**/api/v1/prescriptions', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });

    await page.goto('/login');
    await page.getByLabel('Email').fill('doctor@test.com');
    await page.getByLabel('Password').fill('Test1234!');
    await page.getByRole('button', { name: 'Sign in' }).click();

    // Verify doctor dashboard loads
    await expect(page.getByText('Follow-up on blood pressure')).toBeVisible();

    // ── Step 2: Navigate to Patient Medical Records ───────────────────────

    // Set up medical records routes for the patient
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/header`, (route) =>
      route.fulfill({ status: 200, json: mockPatientHeader }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/conditions`, (route) =>
      route.fulfill({ status: 200, json: mockConditions }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/allergies`, (route) =>
      route.fulfill({ status: 200, json: mockAllergies }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/timeline**`, (route) =>
      route.fulfill({ status: 200, json: mockTimeline }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/snapshot`, (route) =>
      route.fulfill({ status: 200, json: mockSnapshot }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/vitals/**`, (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/medications`, (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/family-history`, (route) =>
      route.fulfill({ status: 200, json: [] }),
    );
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/documents**`, (route) =>
      route.fulfill({ status: 200, json: [] }),
    );

    // Navigate to patient's medical records page
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);

    // ── Step 3: Verify Patient Header ─────────────────────────────────────

    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();
    await expect(page.getByText('Austin, TX')).toBeVisible();
    // Allergy and condition chips in the header
    const header = page.getByLabel('Patient header');
    await expect(header.getByText('Penicillin')).toBeVisible();
    await expect(header.getByText('Hypertension')).toBeVisible();

    // ── Step 4: Browse Timeline ───────────────────────────────────────────

    // Timeline is the default tab
    await expect(page.getByText('Essential Hypertension')).toBeVisible();
    await expect(page.getByText('Peanuts allergy recorded')).toBeVisible();
    await expect(page.getByText('Blood Pressure: 120/80 mmHg')).toBeVisible();

    // ── Step 5: Review Conditions ─────────────────────────────────────────

    await page.getByRole('tab', { name: 'Conditions' }).click();
    await expect(page.getByText('I10')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Essential Hypertension' })).toBeVisible();
    await expect(page.getByText('J45')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Asthma' })).toBeVisible();
    await expect(page.getByText('Monitor blood pressure regularly.')).toBeVisible();

    // ── Step 6: Review Allergies ──────────────────────────────────────────

    await page.getByRole('tab', { name: 'Allergies' }).click();
    await expect(page.getByRole('heading', { name: 'Penicillin' })).toBeVisible();
    await expect(page.getByText('Hives and swelling')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Peanuts' })).toBeVisible();
    await expect(page.getByText('Throat swelling')).toBeVisible();

    // ── Step 7: Check Snapshot Overview ───────────────────────────────────

    await page.getByRole('tab', { name: 'Snapshot' }).click();
    await expect(page.getByText('4 of 6')).toBeVisible();
    await expect(page.getByText('Active Conditions')).toBeVisible();
    await expect(page.getByText('Current Medications')).toBeVisible();
    await expect(page.getByText('Lisinopril')).toBeVisible();
    await expect(page.getByText('Latest Vitals')).toBeVisible();
    await expect(page.getByText('120/80')).toBeVisible();
    await expect(page.getByText('72')).toBeVisible();
  });
});
