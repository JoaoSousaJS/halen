import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, loginAs, mockBaseRoutes, mockMessagingRoutes } from '../helpers';

/**
 * Patient Journey E2E Test
 *
 * Simulates a realistic patient workflow across multiple features:
 * Login -> Dashboard -> Medical Records -> Add Condition -> Add Allergy
 * -> View Timeline -> View Snapshot
 *
 * All API routes are mocked — this tests frontend integration, not backend.
 */

const PATIENT_PROFILE_ID = 'pp-001';

// ── Mock Data ────────────────────────────────────────────────────────────────

const mockPatientHeader = {
  patientProfileId: PATIENT_PROFILE_ID,
  patientName: 'Maya Chen',
  city: 'Austin, TX',
  allergyChips: [],
  conditionChips: [],
};

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

const mockPrescriptions = [
  {
    id: 'rx-1',
    drugName: 'Lisinopril',
    dosage: '10mg',
    frequency: 'Once daily',
    refillsRemaining: 5,
    status: 'Active',
    pharmacyName: 'CVS Pharmacy',
    doctorName: 'Dr. House',
    patientName: 'Maya Chen',
    createdAt: new Date(Date.now() - 86_400_000).toISOString(),
  },
];

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
];

const mockThreads = [
  {
    threadId: 't-1',
    otherParticipantName: 'Dr. House',
    otherParticipantSpecialty: 'Diagnostics',
    subject: 'Annual checkup',
    lastMessagePreview: 'How are you feeling today?',
    lastMessageAt: new Date().toISOString(),
    unreadCount: 1,
    status: 'Active',
    appointmentStatus: 'Scheduled',
    appointmentId: 'appt-1',
  },
];

const mockChatMessages = [
  {
    id: 'm-1',
    senderName: 'Dr. House',
    senderRole: 'Doctor',
    senderUserId: '2',
    content: 'How are you feeling today?',
    messageType: 'Text',
    isRead: false,
    readAt: null,
    createdAt: new Date().toISOString(),
    attachments: [],
  },
];

const emptySnapshot = {
  allergies: [],
  activeConditions: [],
  activeMedications: [],
  familyHistory: [],
  latestVitals: null,
  onboardingProgress: 0,
};

const emptyTimeline = { entries: [], totalCount: 0 };

test.describe('Patient Journey — Dashboard to Medical Records', () => {
  test('patient logs in, views dashboard, navigates to medical records, adds data, and reviews timeline', async ({ page }) => {
    // ── Step 1: Login ──────────────────────────────────────────────────────

    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({ status: 200, json: { token: PATIENT_TOKEN } }),
    );
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'medical_records', isEnabled: true },
        { featureKey: 'messaging', isEnabled: true },
      ],
      appointments: mockAppointments,
      prescriptions: mockPrescriptions,
      searchDoctors: mockSearchDoctors,
      specialties: ['Diagnostics'],
    });

    await page.goto('/login');
    await page.getByLabel('Email').fill('patient@test.com');
    await page.getByLabel('Password').fill('Test1234!');
    await page.getByRole('button', { name: 'Sign in' }).click();

    // Verify dashboard loads
    await expect(page.getByRole('heading', { name: /book an/i })).toBeVisible();
    await expect(page.getByText('Annual checkup')).toBeVisible();
    await expect(page.getByText('Lisinopril')).toBeVisible();

    // ── Step 2: Navigate to Medical Records ────────────────────────────────

    // Track condition and allergy mutations for state progression
    let conditionAdded = false;
    let allergyAdded = false;

    // Mock all medical records routes
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/header`, (route) =>
      route.fulfill({ status: 200, json: mockPatientHeader }),
    );

    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/conditions`, (route) => {
      if (route.request().method() === 'POST') {
        conditionAdded = true;
        return route.fulfill({ status: 201, json: { conditionId: 'cond-new' } });
      }
      if (route.request().method() === 'GET') {
        if (conditionAdded) {
          return route.fulfill({
            status: 200,
            json: [
              {
                id: 'cond-new',
                icdCode: 'J45',
                icdDescription: 'Asthma',
                dateOfOnset: null,
                severity: 'Mild',
                status: 'Active',
                clinicalNotes: 'Mild intermittent asthma',
                addedBy: 'Maya Chen',
                createdAt: new Date().toISOString(),
              },
            ],
          });
        }
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });

    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/allergies`, (route) => {
      if (route.request().method() === 'POST') {
        allergyAdded = true;
        return route.fulfill({ status: 201, json: { allergyId: 'allergy-new' } });
      }
      if (route.request().method() === 'GET') {
        if (allergyAdded) {
          return route.fulfill({
            status: 200,
            json: [
              {
                id: 'allergy-new',
                allergenName: 'Peanuts',
                reaction: 'Throat swelling',
                severity: 'Severe',
                dateIdentified: null,
                isActive: true,
                addedBy: 'Maya Chen',
                createdAt: new Date().toISOString(),
              },
            ],
          });
        }
        return route.fulfill({ status: 200, json: [] });
      }
      return route.continue();
    });

    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/timeline**`, (route) => {
      const entries = [];
      if (conditionAdded) {
        entries.push({
          id: 'tl-1',
          type: 'Condition',
          occurredAt: new Date().toISOString(),
          title: 'Asthma',
          subtitle: 'ICD-10: J45 — Mild, Active',
          addedBy: 'Maya Chen',
        });
      }
      if (allergyAdded) {
        entries.push({
          id: 'tl-2',
          type: 'Allergy',
          occurredAt: new Date().toISOString(),
          title: 'Peanuts allergy recorded',
          subtitle: 'Severe — Throat swelling',
          addedBy: 'Maya Chen',
        });
      }
      return route.fulfill({
        status: 200,
        json: { entries, totalCount: entries.length },
      });
    });

    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/snapshot`, (route) => {
      const snapshot = { ...emptySnapshot };
      if (conditionAdded) {
        snapshot.activeConditions = [{ id: 'cond-new', icdDescription: 'Asthma', severity: 'Mild' }];
        snapshot.onboardingProgress = 1;
      }
      if (allergyAdded) {
        snapshot.allergies = [{ id: 'allergy-new', allergenName: 'Peanuts', reaction: 'Throat swelling', severity: 'Severe' }];
        snapshot.onboardingProgress = conditionAdded ? 2 : 1;
      }
      return route.fulfill({ status: 200, json: snapshot });
    });

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

    // Navigate directly to medical records
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);

    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();
    await expect(page.getByText('Austin, TX')).toBeVisible();

    // ── Step 3: Add a Condition ────────────────────────────────────────────

    await page.getByRole('tab', { name: 'Conditions' }).click();
    await expect(page.getByText('No conditions recorded yet.')).toBeVisible();

    await page.getByRole('button', { name: 'Add condition' }).click();
    await page.getByLabel('ICD Code').fill('J45');
    await page.getByLabel('Description').fill('Asthma');
    await page.getByLabel('Severity').selectOption('Mild');
    await page.getByLabel('Status').selectOption('Active');
    await page.getByLabel('Clinical Notes').fill('Mild intermittent asthma');
    await page.getByRole('button', { name: 'Save' }).click();

    // Verify the condition appears in the list
    await expect(page.getByText('Asthma')).toBeVisible();
    await expect(page.getByText('J45')).toBeVisible();

    // ── Step 4: Add an Allergy ─────────────────────────────────────────────

    await page.getByRole('tab', { name: 'Allergies' }).click();
    await expect(page.getByText('No allergies recorded yet.')).toBeVisible();

    await page.getByRole('button', { name: 'Add allergy' }).click();
    await page.getByLabel('Allergen Name').fill('Peanuts');
    await page.getByLabel('Reaction').fill('Throat swelling');
    await page.getByLabel('Severity').selectOption('Severe');
    await page.getByRole('button', { name: 'Save' }).click();

    // Verify the allergy appears
    await expect(page.getByText('Peanuts')).toBeVisible();
    await expect(page.getByText('Throat swelling')).toBeVisible();

    // ── Step 5: View Timeline ──────────────────────────────────────────────

    await page.getByRole('tab', { name: 'Timeline' }).click();

    // Both entries should now appear in the timeline
    await expect(page.getByText('Asthma')).toBeVisible();
    await expect(page.getByText('Peanuts allergy recorded')).toBeVisible();

    // ── Step 6: View Snapshot ──────────────────────────────────────────────

    await page.getByRole('tab', { name: 'Snapshot' }).click();

    // Snapshot should reflect the newly added data
    await expect(page.getByText('Active Conditions')).toBeVisible();
    await expect(page.getByText('Asthma')).toBeVisible();
    await expect(page.getByText('Peanuts')).toBeVisible();
    await expect(page.getByText('2 of 6')).toBeVisible();

    // ── Step 7: Navigate to Messages and Send a Reply ─────────────────────

    let messageSent = false;

    await mockMessagingRoutes(page, mockThreads, mockChatMessages);

    await page.route(/\/api\/v1\/messaging\/threads\/[^/]+\/messages/, (route) => {
      if (route.request().method() === 'POST') {
        messageSent = true;
        return route.fulfill({ status: 201, json: { messageId: 'msg-new' } });
      }
      if (route.request().method() === 'GET') {
        const msgs = [...mockChatMessages];
        if (messageSent) {
          msgs.push({
            id: 'msg-new',
            senderName: 'Maya Chen',
            senderRole: 'Patient',
            senderUserId: '1',
            content: 'Feeling much better after the medication',
            messageType: 'Text',
            isRead: false,
            readAt: null,
            createdAt: new Date().toISOString(),
            attachments: [],
          });
        }
        return route.fulfill({ status: 200, json: { messages: msgs, totalCount: msgs.length } });
      }
      return route.fallback();
    });

    await page.goto('/messages');

    await expect(page.getByText('Dr. House')).toBeVisible();
    await expect(page.getByText('How are you feeling today?')).toBeVisible();

    await page.getByText('Dr. House').click();

    await expect(page.getByText('How are you feeling today?')).toBeVisible();

    const messageInput = page.getByPlaceholder(/type a message/i);
    await messageInput.fill('Feeling much better after the medication');
    await page.getByRole('button', { name: /send/i }).click();

    await expect(messageInput).toHaveValue('');
  });
});
