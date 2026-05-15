import { test, expect } from '@playwright/test';
import { fakeJwt } from './helpers';

const doctorToken = fakeJwt({
  sub: '2',
  email: 'doctor@test.com',
  given_name: 'Anika',
  family_name: 'Volpe',
  role: 'Doctor',
  exp: 9_999_999_999,
});

const adminToken = fakeJwt({
  sub: 'a-001',
  email: 'admin@test.com',
  given_name: 'Lior',
  family_name: 'Adler',
  role: 'Admin',
  exp: 9_999_999_999,
});

const kycNotSubmitted = {
  status: 'NotSubmitted',
  submittedAt: null,
  lastRejectionReason: null,
  documents: [],
};

const kycSubmitted = {
  status: 'Submitted',
  submittedAt: new Date(Date.now() - 86400000).toISOString(),
  lastRejectionReason: null,
  documents: [
    { id: 'doc-1', documentType: 'LicensePhoto', fileName: 'license.jpg', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
    { id: 'doc-2', documentType: 'MedicalCertificate', fileName: 'cert.pdf', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
    { id: 'doc-3', documentType: 'IdentityProof', fileName: 'id.png', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
  ],
};

const kycRejected = {
  status: 'Rejected',
  submittedAt: new Date(Date.now() - 172800000).toISOString(),
  lastRejectionReason: 'The license photo is too blurry to verify.',
  documents: [],
};

const kycApproved = {
  status: 'Approved',
  submittedAt: new Date(Date.now() - 604800000).toISOString(),
  lastRejectionReason: null,
  documents: [],
};

const adminKycDetails = {
  doctorProfileId: 'dp-001',
  doctorName: 'Dr. Anika Volpe',
  specialty: 'Cardiology',
  licenseNumber: 'MED-29481',
  status: 'Submitted',
  submittedAt: new Date(Date.now() - 86400000).toISOString(),
  documents: [
    { id: 'doc-1', documentType: 'LicensePhoto', fileName: 'license.jpg', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
    { id: 'doc-2', documentType: 'MedicalCertificate', fileName: 'cert.pdf', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
    { id: 'doc-3', documentType: 'IdentityProof', fileName: 'id.png', uploadedAt: new Date(Date.now() - 86400000).toISOString() },
  ],
  reviewHistory: [],
};

const mockUsers = [
  { id: 'd-022', name: 'Dr. Anika Volpe', role: 'Doctor', status: 'PendingReview', plan: null, lastLoginAt: new Date().toISOString(), isFlagged: true, doctorProfileId: 'dp-001' },
  { id: 'p-044', name: 'Wesley Tanaka', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: new Date().toISOString(), isFlagged: false },
];

// ── Doctor KYC Flow ─────────────────────────────────────────────────────────

function mockDoctorBase(page: import('@playwright/test').Page) {
  return Promise.all([
    page.route('**/hubs/**', (route) => route.abort()),
    page.route('**/api/v1/appointments', (route) =>
      route.fulfill({ status: 200, json: [] }),
    ),
    page.route('**/api/v1/prescriptions', (route) =>
      route.fulfill({ status: 200, json: [] }),
    ),
  ]);
}

test.describe('Doctor KYC — Not Submitted', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, doctorToken);

    await mockDoctorBase(page);
    await page.route('**/api/v1/doctor/kyc/status', (route) =>
      route.fulfill({ status: 200, json: kycNotSubmitted }),
    );
  });

  test('shows KYC upload form when not submitted', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Complete your KYC verification')).toBeVisible();
    await expect(page.getByText('License photo')).toBeVisible();
    await expect(page.getByText('Medical certificate')).toBeVisible();
    await expect(page.getByText('Identity proof')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Submit for review' })).toBeVisible();
  });

  test('submit button is disabled until all files are selected', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('button', { name: 'Submit for review' })).toBeDisabled();
  });

  test('submits documents and transitions to submitted state', async ({ page }) => {
    let callCount = 0;
    await page.route('**/api/v1/doctor/kyc/status', (route) => {
      callCount++;
      if (callCount <= 1) {
        return route.fulfill({ status: 200, json: kycNotSubmitted });
      }
      return route.fulfill({ status: 200, json: kycSubmitted });
    });
    await page.route('**/api/v1/doctor/kyc/documents', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 200, json: { message: 'KYC documents submitted successfully' } });
      }
      return route.continue();
    });

    await page.goto('/dashboard');
    await expect(page.getByText('Complete your KYC verification')).toBeVisible();

    const fileInputs = page.locator('input[type="file"]');
    const fakeFile = {
      name: 'test.jpg',
      mimeType: 'image/jpeg',
      buffer: Buffer.from('fake-content'),
    };
    await fileInputs.nth(0).setInputFiles(fakeFile);
    await fileInputs.nth(1).setInputFiles({ ...fakeFile, name: 'cert.pdf', mimeType: 'application/pdf' });
    await fileInputs.nth(2).setInputFiles({ ...fakeFile, name: 'id.png', mimeType: 'image/png' });

    await expect(page.getByRole('button', { name: 'Submit for review' })).toBeEnabled();
    await page.getByRole('button', { name: 'Submit for review' }).click();

    await expect(page.getByText('KYC documents submitted')).toBeVisible();
    await expect(page.getByText('Your documents are under review')).toBeVisible();
  });
});

test.describe('Doctor KYC — Submitted', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, doctorToken);

    await mockDoctorBase(page);
    await page.route('**/api/v1/doctor/kyc/status', (route) =>
      route.fulfill({ status: 200, json: kycSubmitted }),
    );
  });

  test('shows submitted status with document list', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('KYC documents submitted')).toBeVisible();
    await expect(page.getByText('Your documents are under review')).toBeVisible();
    await expect(page.getByText('LicensePhoto: license.jpg')).toBeVisible();
    await expect(page.getByText('MedicalCertificate: cert.pdf')).toBeVisible();
    await expect(page.getByText('IdentityProof: id.png')).toBeVisible();
  });

  test('does not show the upload form or schedule', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('KYC documents submitted')).toBeVisible();
    await expect(page.getByText('Complete your KYC verification')).not.toBeVisible();
    await expect(page.getByText(/your.*schedule/i)).not.toBeVisible();
  });
});

test.describe('Doctor KYC — Rejected', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, doctorToken);

    await mockDoctorBase(page);
    await page.route('**/api/v1/doctor/kyc/status', (route) =>
      route.fulfill({ status: 200, json: kycRejected }),
    );
  });

  test('shows rejection reason and upload form for resubmission', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Your previous submission was rejected:')).toBeVisible();
    await expect(page.getByText('The license photo is too blurry to verify.')).toBeVisible();
    await expect(page.getByText('Complete your KYC verification')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Submit for review' })).toBeVisible();
  });
});

test.describe('Doctor KYC — Approved', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, doctorToken);

    await mockDoctorBase(page);
    await page.route('**/api/v1/doctor/kyc/status', (route) =>
      route.fulfill({ status: 200, json: kycApproved }),
    );
  });

  test('shows the doctor schedule instead of KYC form', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText(/schedule/i).first()).toBeVisible();
    await expect(page.getByText('Complete your KYC verification')).not.toBeVisible();
    await expect(page.getByText('KYC documents submitted')).not.toBeVisible();
  });
});

// ── Admin KYC Review Flow ───────────────────────────────────────────────────

test.describe('Admin KYC Review', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((token: string) => {
      localStorage.setItem('token', token);
    }, adminToken);

    await page.route('**/hubs/**', (route) => route.abort());
    await page.route('**/api/v1/admin/users**', (route) =>
      route.fulfill({ status: 200, json: { users: mockUsers, totalCount: mockUsers.length } }),
    );
  });

  test('clicking Review navigates to KYC review page', async ({ page }) => {
    await page.route('**/api/v1/admin/doctors/dp-001/kyc', (route) =>
      route.fulfill({ status: 200, json: adminKycDetails }),
    );

    await page.goto('/dashboard');
    await expect(page.getByText('Dr. Anika Volpe')).toBeVisible();

    await page.getByRole('button', { name: 'Review', exact: true }).click();
    await expect(page.getByText('KYC Review')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Dr. Anika Volpe' })).toBeVisible();
    await expect(page.getByText('Cardiology · License: MED-29481')).toBeVisible();
  });

  test('shows uploaded documents on review page', async ({ page }) => {
    await page.route('**/api/v1/admin/doctors/dp-001/kyc', (route) =>
      route.fulfill({ status: 200, json: adminKycDetails }),
    );

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Review', exact: true }).click();

    await expect(page.getByText('Uploaded documents')).toBeVisible();
    await expect(page.getByText('LicensePhoto')).toBeVisible();
    await expect(page.getByText('MedicalCertificate')).toBeVisible();
    await expect(page.getByText('IdentityProof')).toBeVisible();
  });

  test('approve button sends approval and navigates back', async ({ page }) => {
    let reviewPayload: unknown = null;
    await page.route('**/api/v1/admin/doctors/dp-001/kyc', (route) =>
      route.fulfill({ status: 200, json: adminKycDetails }),
    );
    await page.route('**/api/v1/admin/doctors/dp-001/kyc/review', async (route) => {
      reviewPayload = JSON.parse(route.request().postData() || '{}');
      return route.fulfill({ status: 200, json: { message: 'KYC review recorded' } });
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Review', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Dr. Anika Volpe' })).toBeVisible();

    await page.getByRole('button', { name: 'Approve' }).click();
    await expect(page.getByText('Users.')).toBeVisible();
    expect(reviewPayload).toEqual({ decision: 'Approved' });
  });

  test('reject flow shows reason textarea and sends rejection', async ({ page }) => {
    let reviewPayload: unknown = null;
    await page.route('**/api/v1/admin/doctors/dp-001/kyc', (route) =>
      route.fulfill({ status: 200, json: adminKycDetails }),
    );
    await page.route('**/api/v1/admin/doctors/dp-001/kyc/review', async (route) => {
      reviewPayload = JSON.parse(route.request().postData() || '{}');
      return route.fulfill({ status: 200, json: { message: 'KYC review recorded' } });
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Review', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Dr. Anika Volpe' })).toBeVisible();

    await page.getByRole('button', { name: 'Reject' }).click();
    await expect(page.getByPlaceholder('Explain why the documents are being rejected…')).toBeVisible();

    const confirmBtn = page.getByRole('button', { name: 'Confirm rejection' });
    await expect(confirmBtn).toBeDisabled();

    await page.getByPlaceholder('Explain why the documents are being rejected…').fill('Blurry photo');
    await expect(confirmBtn).toBeEnabled();
    await confirmBtn.click();

    await expect(page.getByText('Users.')).toBeVisible();
    expect(reviewPayload).toEqual({ decision: 'Rejected', rejectionReason: 'Blurry photo' });
  });

  test('back button returns to users list', async ({ page }) => {
    await page.route('**/api/v1/admin/doctors/dp-001/kyc', (route) =>
      route.fulfill({ status: 200, json: adminKycDetails }),
    );

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Review', exact: true }).click();
    await expect(page.getByText('KYC Review')).toBeVisible();

    await page.getByRole('button', { name: '← Back to users' }).click();
    await expect(page.getByText('Users.')).toBeVisible();
  });

  test('cancel button on reject form hides the form', async ({ page }) => {
    await page.route('**/api/v1/admin/doctors/dp-001/kyc', (route) =>
      route.fulfill({ status: 200, json: adminKycDetails }),
    );

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Review', exact: true }).click();

    await page.getByRole('button', { name: 'Reject' }).click();
    await expect(page.getByPlaceholder('Explain why the documents are being rejected…')).toBeVisible();

    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.getByPlaceholder('Explain why the documents are being rejected…')).not.toBeVisible();
    await expect(page.getByRole('button', { name: 'Approve' })).toBeVisible();
  });
});
