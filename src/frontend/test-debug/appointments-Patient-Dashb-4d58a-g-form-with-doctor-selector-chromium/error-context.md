# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: appointments.spec.ts >> Patient Dashboard — Appointments >> shows booking form with doctor selector
- Location: tests/appointments.spec.ts:66:3

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByText('Book an appointment.')
Expected: visible
Timeout: 5000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for getByText('Book an appointment.')

```

```yaml
- banner:
  - text: Halen care · on call Maya Chen
  - button "Sign out"
- main:
  - heading "Book an appointment." [level=1]:
    - text: Book an
    - emphasis: appointment.
  - text: Doctor
  - combobox "Doctor":
    - option "Select a doctor…" [selected]
    - option "Dr. House — Diagnostics ($150)"
  - text: Date & time
  - textbox "Date & time"
  - text: Reason for visit
  - textbox "Reason for visit":
    - /placeholder: Describe your symptoms or reason…
  - button "Book appointment"
  - heading "Your appointments" [level=2]
  - paragraph: No appointments yet — book one above.
```

# Test source

```ts
  1   | import { test, expect } from '@playwright/test';
  2   | 
  3   | function fakeJwt(payload: object): string {
  4   |   const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  5   |   const body = Buffer.from(JSON.stringify(payload)).toString('base64url');
  6   |   return `${header}.${body}.fake-sig`;
  7   | }
  8   | 
  9   | const patientToken = fakeJwt({
  10  |   sub: '1',
  11  |   email: 'patient@test.com',
  12  |   given_name: 'Maya',
  13  |   family_name: 'Chen',
  14  |   role: 'Patient',
  15  |   exp: 9_999_999_999,
  16  | });
  17  | 
  18  | const doctorToken = fakeJwt({
  19  |   sub: '2',
  20  |   email: 'doctor@test.com',
  21  |   given_name: 'Gregory',
  22  |   family_name: 'House',
  23  |   role: 'Doctor',
  24  |   exp: 9_999_999_999,
  25  | });
  26  | 
  27  | const mockDoctors = [
  28  |   { id: 'doc-1', name: 'Dr. House', specialty: 'Diagnostics', consultationFee: 150, yearsOfExperience: 20 },
  29  | ];
  30  | 
  31  | const mockAppointments = [
  32  |   {
  33  |     id: 'appt-1',
  34  |     scheduledAt: new Date(Date.now() + 86_400_000).toISOString(),
  35  |     durationMinutes: 20,
  36  |     reason: 'Annual checkup',
  37  |     status: 'Scheduled',
  38  |     notes: null,
  39  |     doctorName: 'Dr. House',
  40  |     specialty: 'Diagnostics',
  41  |     consultationFee: 150,
  42  |     patientName: 'Maya Chen',
  43  |   },
  44  | ];
  45  | 
  46  | async function loginAsPatient(page: import('@playwright/test').Page) {
  47  |   await page.addInitScript((token: string) => {
  48  |     localStorage.setItem('token', token);
  49  |   }, patientToken);
  50  | }
  51  | 
  52  | async function loginAsDoctor(page: import('@playwright/test').Page) {
  53  |   await page.addInitScript((token: string) => {
  54  |     localStorage.setItem('token', token);
  55  |   }, doctorToken);
  56  | }
  57  | 
  58  | // ── Patient Dashboard ─────────────────────────────────────────────────────
  59  | 
  60  | test.describe('Patient Dashboard — Appointments', () => {
  61  |   test.beforeEach(async ({ page }) => {
  62  |     await loginAsPatient(page);
  63  |     await page.route('**/hubs/**', (route) => route.abort());
  64  |   });
  65  | 
  66  |   test('shows booking form with doctor selector', async ({ page }) => {
  67  |     await page.route('**/api/v1/appointments/doctors', (route) =>
  68  |       route.fulfill({ status: 200, json: mockDoctors }),
  69  |     );
  70  |     await page.route('**/api/v1/appointments', (route) => {
  71  |       if (route.request().method() === 'GET') {
  72  |         return route.fulfill({ status: 200, json: [] });
  73  |       }
  74  |       return route.continue();
  75  |     });
  76  | 
  77  |     await page.goto('/dashboard');
> 78  |     await expect(page.getByText('Book an appointment.', { exact: false })).toBeVisible();
      |                                                                            ^ Error: expect(locator).toBeVisible() failed
  79  |     await expect(page.getByText('Select a doctor')).toBeVisible();
  80  |     await expect(page.getByText('No appointments yet')).toBeVisible();
  81  |   });
  82  | 
  83  |   test('books an appointment successfully', async ({ page }) => {
  84  |     await page.route('**/api/v1/appointments/doctors', (route) =>
  85  |       route.fulfill({ status: 200, json: mockDoctors }),
  86  |     );
  87  |     await page.route('**/api/v1/appointments', (route) => {
  88  |       if (route.request().method() === 'GET') {
  89  |         return route.fulfill({ status: 200, json: [] });
  90  |       }
  91  |       if (route.request().method() === 'POST') {
  92  |         return route.fulfill({ status: 201, json: { appointmentId: 'new-appt-1' } });
  93  |       }
  94  |       return route.continue();
  95  |     });
  96  | 
  97  |     await page.goto('/dashboard');
  98  | 
  99  |     await page.selectOption('select', 'doc-1');
  100 |     await page.fill('input[type="datetime-local"]', '2027-01-15T10:00');
  101 |     await page.fill('textarea', 'Regular checkup');
  102 |     await page.click('button[type="submit"]');
  103 | 
  104 |     await expect(page.getByText('Appointment booked!')).toBeVisible();
  105 |   });
  106 | 
  107 |   test('shows booking error on conflict', async ({ page }) => {
  108 |     await page.route('**/api/v1/appointments/doctors', (route) =>
  109 |       route.fulfill({ status: 200, json: mockDoctors }),
  110 |     );
  111 |     await page.route('**/api/v1/appointments', (route) => {
  112 |       if (route.request().method() === 'GET') {
  113 |         return route.fulfill({ status: 200, json: [] });
  114 |       }
  115 |       if (route.request().method() === 'POST') {
  116 |         return route.fulfill({ status: 400, json: { error: 'This time slot is not available' } });
  117 |       }
  118 |       return route.continue();
  119 |     });
  120 | 
  121 |     await page.goto('/dashboard');
  122 | 
  123 |     await page.selectOption('select', 'doc-1');
  124 |     await page.fill('input[type="datetime-local"]', '2027-01-15T10:00');
  125 |     await page.fill('textarea', 'Checkup');
  126 |     await page.click('button[type="submit"]');
  127 | 
  128 |     await expect(page.getByText('This time slot is not available')).toBeVisible();
  129 |   });
  130 | 
  131 |   test('displays existing appointments', async ({ page }) => {
  132 |     await page.route('**/api/v1/appointments/doctors', (route) =>
  133 |       route.fulfill({ status: 200, json: mockDoctors }),
  134 |     );
  135 |     await page.route('**/api/v1/appointments', (route) => {
  136 |       if (route.request().method() === 'GET') {
  137 |         return route.fulfill({ status: 200, json: mockAppointments });
  138 |       }
  139 |       return route.continue();
  140 |     });
  141 | 
  142 |     await page.goto('/dashboard');
  143 | 
  144 |     await expect(page.getByText('Dr. House')).toBeVisible();
  145 |     await expect(page.getByText('Annual checkup')).toBeVisible();
  146 |     await expect(page.getByText('Scheduled')).toBeVisible();
  147 |   });
  148 | 
  149 |   test('cancels an appointment', async ({ page }) => {
  150 |     await page.route('**/api/v1/appointments/doctors', (route) =>
  151 |       route.fulfill({ status: 200, json: mockDoctors }),
  152 |     );
  153 | 
  154 |     let getCalls = 0;
  155 |     await page.route('**/api/v1/appointments', (route) => {
  156 |       if (route.request().method() === 'GET') {
  157 |         getCalls++;
  158 |         if (getCalls === 1) {
  159 |           return route.fulfill({ status: 200, json: mockAppointments });
  160 |         }
  161 |         return route.fulfill({
  162 |           status: 200,
  163 |           json: [{ ...mockAppointments[0], status: 'Cancelled' }],
  164 |         });
  165 |       }
  166 |       return route.continue();
  167 |     });
  168 |     await page.route('**/api/v1/appointments/appt-1/cancel', (route) =>
  169 |       route.fulfill({ status: 200 }),
  170 |     );
  171 | 
  172 |     await page.goto('/dashboard');
  173 |     await page.click('button[aria-label="Cancel appointment with Dr. House"]');
  174 | 
  175 |     await expect(page.getByText('Cancelled')).toBeVisible();
  176 |   });
  177 | });
  178 | 
```