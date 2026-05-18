import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, loginAs, mockBaseRoutes } from './helpers';

// ── Mock Data ────────────────────────────────────────────────────────────────

const PATIENT_PROFILE_ID = 'pp-001';

const mockPatientHeader = {
  patientProfileId: PATIENT_PROFILE_ID,
  patientName: 'Maya Chen',
  city: 'Austin, TX',
  allergyChips: ['Penicillin'],
  conditionChips: ['Hypertension'],
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
];

const mockTimeline = {
  entries: [
    {
      id: 'tl-1',
      type: 'Condition',
      occurredAt: '2024-01-10T10:00:00Z',
      title: 'Essential Hypertension',
      subtitle: 'ICD-10: I10 — Moderate, Active',
      addedBy: 'Dr. House',
    },
    {
      id: 'tl-2',
      type: 'Allergy',
      occurredAt: '2024-01-09T08:00:00Z',
      title: 'Penicillin allergy recorded',
      subtitle: 'Severe — Hives and swelling',
      addedBy: 'Dr. House',
    },
    {
      id: 'tl-3',
      type: 'Vital',
      occurredAt: '2024-01-08T14:00:00Z',
      title: 'Blood Pressure: 120/80 mmHg',
      subtitle: null,
      addedBy: 'Maya Chen',
    },
  ],
  totalCount: 3,
};

const mockSnapshot = {
  allergies: [{ id: 'allergy-1', allergenName: 'Penicillin', reaction: 'Hives and swelling', severity: 'Severe' }],
  activeConditions: [{ id: 'cond-1', icdDescription: 'Essential Hypertension', severity: 'Moderate' }],
  activeMedications: [{ id: 'med-1', medicationName: 'Lisinopril', dosage: '10mg', frequency: 'Once daily', startDate: '2024-01-10' }],
  familyHistory: [{ id: 'fh-1', relationship: 'Father', conditionName: 'Type 2 Diabetes' }],
  latestVitals: {
    bloodPressure: { value: 120, secondaryValue: 80, unit: 'mmHg', measuredAt: '2024-01-08T14:00:00Z' },
    heartRate: { value: 72, secondaryValue: null, unit: 'bpm', measuredAt: '2024-01-08T14:00:00Z' },
    weight: { value: 68, secondaryValue: null, unit: 'kg', measuredAt: '2024-01-07T10:00:00Z' },
    spO2: { value: 98, secondaryValue: null, unit: '%', measuredAt: '2024-01-08T14:00:00Z' },
  },
  onboardingProgress: 4,
};

const mockDocuments = [
  {
    id: 'doc-1',
    type: 'LabResult',
    title: 'Blood work results',
    description: 'Annual blood work',
    fileName: 'bloodwork-2024.pdf',
    fileSize: 245760,
    uploadedBy: 'Maya Chen',
    uploadedAt: '2024-01-05T09:00:00Z',
  },
];

const mockVitalsHistory = [
  {
    id: 'vr-1',
    value: 120,
    secondaryValue: 80,
    unit: 'mmHg',
    measuredAt: '2024-01-08T14:00:00Z',
    source: 'Manual',
    notes: null,
    addedBy: 'Maya Chen',
  },
  {
    id: 'vr-2',
    value: 118,
    secondaryValue: 78,
    unit: 'mmHg',
    measuredAt: '2024-01-01T10:00:00Z',
    source: 'Device',
    notes: null,
    addedBy: 'Maya Chen',
  },
];

// ── Route Mocking Helpers ────────────────────────────────────────────────────

async function mockMedicalRecordsRoutes(
  page: import('@playwright/test').Page,
  overrides: {
    header?: object;
    conditions?: unknown[];
    allergies?: unknown[];
    timeline?: object;
    snapshot?: object;
    documents?: unknown[];
    vitalsHistory?: unknown[];
  } = {},
) {
  const {
    header = mockPatientHeader,
    conditions = mockConditions,
    allergies = mockAllergies,
    timeline = mockTimeline,
    snapshot = mockSnapshot,
    documents = mockDocuments,
    vitalsHistory = mockVitalsHistory,
  } = overrides;

  // Patient header
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/header`, (route) =>
    route.fulfill({ status: 200, json: header }),
  );

  // Conditions
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/conditions`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, json: conditions });
    }
    if (route.request().method() === 'POST') {
      return route.fulfill({ status: 201, json: { conditionId: 'cond-new' } });
    }
    return route.continue();
  });

  // Allergies
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/allergies`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, json: allergies });
    }
    if (route.request().method() === 'POST') {
      return route.fulfill({ status: 201, json: { allergyId: 'allergy-new' } });
    }
    return route.continue();
  });

  // Timeline
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/timeline**`, (route) =>
    route.fulfill({ status: 200, json: timeline }),
  );

  // Snapshot
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/snapshot`, (route) =>
    route.fulfill({ status: 200, json: snapshot }),
  );

  // Vitals history
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/vitals/**`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, json: vitalsHistory });
    }
    if (route.request().method() === 'POST') {
      return route.fulfill({ status: 201, json: { vitalId: 'vr-new' } });
    }
    return route.continue();
  });

  // Vitals POST on the base vitals route
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/vitals`, (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ status: 201, json: { vitalId: 'vr-new' } });
    }
    return route.fallback();
  });

  // Documents
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/documents**`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, json: documents });
    }
    if (route.request().method() === 'POST') {
      return route.fulfill({ status: 201, json: { documentId: 'doc-new' } });
    }
    return route.continue();
  });

  // Medications (for snapshot)
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/medications`, (route) =>
    route.fulfill({ status: 200, json: [] }),
  );

  // Family history (for snapshot)
  await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/family-history`, (route) =>
    route.fulfill({ status: 200, json: [] }),
  );
}

// ── Test Suite ────────────────────────────────────────────────────────────────

test.describe('Medical Records', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page, {
      features: [
        { featureKey: 'prescriptions', isEnabled: true },
        { featureKey: 'medical_records', isEnabled: true },
      ],
    });
    await mockMedicalRecordsRoutes(page);
  });

  // ── Page Load & Patient Header ─────────────────────────────────────────

  test('displays patient header with name, city, and chips', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);

    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();
    await expect(page.getByText('Austin, TX')).toBeVisible();
    await expect(page.getByText('Penicillin')).toBeVisible();
    await expect(page.getByText('Hypertension')).toBeVisible();
  });

  test('shows loading state while fetching header', async ({ page }) => {
    // Override header route with a delayed response
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/header`, async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 500));
      await route.fulfill({ status: 200, json: mockPatientHeader });
    });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByText('Loading patient records...')).toBeVisible();
    // Eventually loads
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();
  });

  test('shows error state when header fetch fails', async ({ page }) => {
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/header`, (route) =>
      route.fulfill({ status: 500, json: { error: 'Internal error' } }),
    );

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByText('Failed to load patient data.')).toBeVisible();
  });

  // ── Tab Navigation ─────────────────────────────────────────────────────

  test('renders all tabs and defaults to Timeline', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    const tablist = page.getByRole('tablist', { name: 'Medical record sections' });
    await expect(tablist).toBeVisible();

    const expectedTabs = ['Timeline', 'Snapshot', 'Conditions', 'Allergies', 'Vitals', 'Medications', 'Family History', 'Documents'];
    for (const tabName of expectedTabs) {
      await expect(tablist.getByRole('tab', { name: tabName })).toBeVisible();
    }

    // Timeline tab is selected by default
    await expect(tablist.getByRole('tab', { name: 'Timeline' })).toHaveAttribute('aria-selected', 'true');
  });

  test('switches between tabs', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    // Switch to Conditions tab
    await page.getByRole('tab', { name: 'Conditions' }).click();
    await expect(page.getByRole('tab', { name: 'Conditions' })).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByRole('tab', { name: 'Timeline' })).toHaveAttribute('aria-selected', 'false');
    await expect(page.getByText('Essential Hypertension')).toBeVisible();

    // Switch to Allergies tab
    await page.getByRole('tab', { name: 'Allergies' }).click();
    await expect(page.getByRole('tab', { name: 'Allergies' })).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByText('Penicillin')).toBeVisible();

    // Switch to Snapshot tab
    await page.getByRole('tab', { name: 'Snapshot' }).click();
    await expect(page.getByRole('tab', { name: 'Snapshot' })).toHaveAttribute('aria-selected', 'true');
  });

  // ── Timeline ───────────────────────────────────────────────────────────

  test('displays timeline entries', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    // Default tab is Timeline — entries should be visible
    const timelineList = page.getByRole('list', { name: 'Timeline entries' });
    await expect(timelineList).toBeVisible();
    await expect(page.getByText('Essential Hypertension')).toBeVisible();
    await expect(page.getByText('Penicillin allergy recorded')).toBeVisible();
    await expect(page.getByText('Blood Pressure: 120/80 mmHg')).toBeVisible();
  });

  test('filters timeline by type', async ({ page }) => {
    // Track the last timeline request to verify filter params
    let lastTimelineUrl = '';
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/timeline**`, (route) => {
      lastTimelineUrl = route.request().url();
      return route.fulfill({
        status: 200,
        json: {
          entries: mockTimeline.entries.filter((e) => e.type === 'Condition'),
          totalCount: 1,
        },
      });
    });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    // Uncheck the Allergy filter
    const allergyCheckbox = page.getByRole('group', { name: 'Filter by type' }).getByLabel('Allergy');
    await allergyCheckbox.uncheck();

    // After unchecking, only Condition entries should show
    await expect(page.getByText('Essential Hypertension')).toBeVisible();
  });

  test('shows empty timeline state', async ({ page }) => {
    await mockMedicalRecordsRoutes(page, {
      timeline: { entries: [], totalCount: 0 },
    });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();
    await expect(page.getByText('No medical events recorded yet.')).toBeVisible();
  });

  // ── Conditions ─────────────────────────────────────────────────────────

  test('displays existing conditions', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Conditions' }).click();

    await expect(page.getByText('I10')).toBeVisible();
    await expect(page.getByText('Essential Hypertension')).toBeVisible();
    await expect(page.getByText('Moderate')).toBeVisible();
    await expect(page.getByText('Monitor blood pressure regularly.')).toBeVisible();
  });

  test('adds a condition successfully', async ({ page }) => {
    let postCalled = false;
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/conditions`, (route) => {
      if (route.request().method() === 'POST') {
        postCalled = true;
        return route.fulfill({ status: 201, json: { conditionId: 'cond-new' } });
      }
      if (route.request().method() === 'GET') {
        // After mutation, return updated list
        if (postCalled) {
          return route.fulfill({
            status: 200,
            json: [
              ...mockConditions,
              {
                id: 'cond-new',
                icdCode: 'E11',
                icdDescription: 'Type 2 Diabetes Mellitus',
                dateOfOnset: null,
                severity: 'Mild',
                status: 'Active',
                clinicalNotes: null,
                addedBy: 'Maya Chen',
                createdAt: new Date().toISOString(),
              },
            ],
          });
        }
        return route.fulfill({ status: 200, json: mockConditions });
      }
      return route.continue();
    });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Conditions' }).click();
    await page.getByRole('button', { name: 'Add condition' }).click();

    // Fill the form
    await page.getByLabel('ICD Code').fill('E11');
    await page.getByLabel('Description').fill('Type 2 Diabetes Mellitus');
    await page.getByLabel('Severity').selectOption('Mild');
    await page.getByLabel('Status').selectOption('Active');

    await page.getByRole('button', { name: 'Save' }).click();

    // After save, the new condition should appear
    await expect(page.getByText('Type 2 Diabetes Mellitus')).toBeVisible();
  });

  test('shows validation error when ICD code is empty', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Conditions' }).click();
    await page.getByRole('button', { name: 'Add condition' }).click();

    // Fill only description, leave ICD code empty
    await page.getByLabel('Description').fill('Some condition');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText('ICD code is required')).toBeVisible();
  });

  test('shows empty conditions state', async ({ page }) => {
    await mockMedicalRecordsRoutes(page, { conditions: [] });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Conditions' }).click();
    await expect(page.getByText('No conditions recorded yet.')).toBeVisible();
  });

  // ── Allergies ──────────────────────────────────────────────────────────

  test('displays existing allergies', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Allergies' }).click();

    await expect(page.getByText('Penicillin')).toBeVisible();
    await expect(page.getByText('Hives and swelling')).toBeVisible();
    await expect(page.getByText('Severe')).toBeVisible();
  });

  test('adds an allergy successfully', async ({ page }) => {
    let postCalled = false;
    await page.route(`**/api/v1/medical-records/${PATIENT_PROFILE_ID}/allergies`, (route) => {
      if (route.request().method() === 'POST') {
        postCalled = true;
        return route.fulfill({ status: 201, json: { allergyId: 'allergy-new' } });
      }
      if (route.request().method() === 'GET') {
        if (postCalled) {
          return route.fulfill({
            status: 200,
            json: [
              ...mockAllergies,
              {
                id: 'allergy-new',
                allergenName: 'Shellfish',
                reaction: 'Anaphylaxis',
                severity: 'Severe',
                dateIdentified: null,
                isActive: true,
                addedBy: 'Maya Chen',
                createdAt: new Date().toISOString(),
              },
            ],
          });
        }
        return route.fulfill({ status: 200, json: mockAllergies });
      }
      return route.continue();
    });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Allergies' }).click();
    await page.getByRole('button', { name: 'Add allergy' }).click();

    await page.getByLabel('Allergen Name').fill('Shellfish');
    await page.getByLabel('Reaction').fill('Anaphylaxis');
    await page.getByLabel('Severity').selectOption('Severe');

    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText('Shellfish')).toBeVisible();
    await expect(page.getByText('Anaphylaxis')).toBeVisible();
  });

  test('shows validation error when allergen name is empty', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Allergies' }).click();
    await page.getByRole('button', { name: 'Add allergy' }).click();

    // Submit without filling allergen name
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText('Allergen name is required')).toBeVisible();
  });

  test('shows empty allergies state', async ({ page }) => {
    await mockMedicalRecordsRoutes(page, { allergies: [] });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Allergies' }).click();
    await expect(page.getByText('No allergies recorded yet.')).toBeVisible();
  });

  // ── Vitals ─────────────────────────────────────────────────────────────

  test('displays latest vitals summary', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Vitals' }).click();

    // Latest vitals summary shows BP, HR, weight, SpO2
    await expect(page.getByText('120/80')).toBeVisible();
    await expect(page.getByText('72')).toBeVisible();
    await expect(page.getByText('68')).toBeVisible();
    await expect(page.getByText('98')).toBeVisible();
  });

  test('adds a vital reading successfully', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Vitals' }).click();
    await page.getByRole('button', { name: 'Add vital' }).click();

    // Fill the vital form — default type is Blood Pressure
    await page.getByLabel('Value').fill('125');
    await page.getByLabel('Diastolic value').fill('82');

    await page.getByRole('button', { name: 'Save' }).click();

    // The form should close after successful submission (button text goes back to Add Vital)
    await expect(page.getByRole('button', { name: 'Add vital' })).toBeVisible();
  });

  test('shows empty vitals history state', async ({ page }) => {
    await mockMedicalRecordsRoutes(page, { vitalsHistory: [] });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Vitals' }).click();
    await expect(page.getByText('No history for this vital type yet.')).toBeVisible();
  });

  // ── Snapshot ────────────────────────────────────────────────────────────

  test('displays snapshot with all cards', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Snapshot' }).click();

    // Onboarding progress
    await expect(page.getByText('4 of 6')).toBeVisible();

    // Snapshot cards
    await expect(page.getByText('Active Conditions')).toBeVisible();
    await expect(page.getByText('Essential Hypertension')).toBeVisible();

    await expect(page.getByText('Allergies').first()).toBeVisible();
    await expect(page.getByText('Penicillin')).toBeVisible();

    await expect(page.getByText('Current Medications')).toBeVisible();
    await expect(page.getByText('Lisinopril')).toBeVisible();

    await expect(page.getByText('Family History')).toBeVisible();
    await expect(page.getByText('Type 2 Diabetes')).toBeVisible();

    await expect(page.getByText('Latest Vitals')).toBeVisible();
    await expect(page.getByText('120/80')).toBeVisible();
  });

  test('displays snapshot empty state cards', async ({ page }) => {
    await mockMedicalRecordsRoutes(page, {
      snapshot: {
        allergies: [],
        activeConditions: [],
        activeMedications: [],
        familyHistory: [],
        latestVitals: null,
        onboardingProgress: 0,
      },
    });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Snapshot' }).click();

    await expect(page.getByText('0 of 6')).toBeVisible();
    await expect(page.getByText('Get started by adding your first condition.')).toBeVisible();
    await expect(page.getByText('Get started by adding your first allergy.')).toBeVisible();
    await expect(page.getByText('Get started by adding your first medication.')).toBeVisible();
    await expect(page.getByText('Get started by adding your first family history entry.')).toBeVisible();
    await expect(page.getByText('Get started by adding your first vital reading.')).toBeVisible();
  });

  // ── Documents ──────────────────────────────────────────────────────────

  test('displays existing documents', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Documents' }).click();

    await expect(page.getByText('Blood work results')).toBeVisible();
    await expect(page.getByText('bloodwork-2024.pdf')).toBeVisible();
    await expect(page.getByText('Uploaded by: Maya Chen')).toBeVisible();
  });

  test('shows upload document form', async ({ page }) => {
    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Documents' }).click();
    await page.getByRole('button', { name: 'Upload Document' }).click();

    // Upload dialog form should appear
    await expect(page.getByText('Upload Document')).toBeVisible();
    await expect(page.getByLabel('Choose file')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Upload' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Cancel' })).toBeVisible();
  });

  test('shows empty documents state', async ({ page }) => {
    await mockMedicalRecordsRoutes(page, { documents: [] });

    await page.goto(`/medical-records/${PATIENT_PROFILE_ID}`);
    await expect(page.getByRole('heading', { name: 'Maya Chen' })).toBeVisible();

    await page.getByRole('tab', { name: 'Documents' }).click();
    await expect(page.getByText('No documents uploaded.')).toBeVisible();
  });
});
