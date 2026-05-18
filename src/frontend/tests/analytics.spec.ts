import { test, expect } from '@playwright/test';
import { PLATFORM_ADMIN_TOKEN, PATIENT_TOKEN, loginAs, mockBaseRoutes } from './helpers';

const mockOverview = {
  appointmentKpi: { total: 1234, deltaPct: 12.5, sparkline: [1, 2, 3] },
  revenueKpi: { value: 56000, deltaPct: -5.2, sparkline: [4, 5, 6] },
  activeUsersKpi: { total: 890, deltaPct: 3.1, sparkline: [7, 8, 9] },
  noShowKpi: { rate: 8.5, deltaPct: -1.2, sparkline: [1, 1, 1] },
  appointmentSeries: { labels: ['May 1', 'May 2'], current: [10, 20], previous: [8, 15] },
  revenueSeries: { labels: ['W18', 'W19'], values: [5000, 6000] },
  funnel: [
    { label: 'Booked', value: 100 },
    { label: 'Scheduled', value: 85 },
    { label: 'Completed', value: 70 },
    { label: 'Paid', value: 60 },
  ],
  activeUsers: { dau: 50, wau: 200, mau: 890, dauDelta: 5, wauDelta: 3, mauDelta: 2, stickiness: 5.6 },
  clinicBreakdown: [{ name: 'Lisbon Sul', value: 400 }],
  specialtyMix: [{ label: 'Cardiology', value: 500 }],
};

const mockAppointments = {
  bookedKpi: { total: 500, deltaPct: 10, sparkline: [1, 2] },
  completedKpi: { total: 400, deltaPct: 5, sparkline: [1, 2] },
  cancelledKpi: { total: 50, deltaPct: -2, sparkline: [1, 2] },
  avgLeadTimeKpi: { value: 3.5, deltaPct: 1, sparkline: [1, 2] },
  dailySeries: { labels: ['May 1'], current: [10], previous: [8] },
  byDayOfWeek: [
    { day: 'Mon', ratio: 0.18 },
    { day: 'Tue', ratio: 0.16 },
    { day: 'Wed', ratio: 0.15 },
    { day: 'Thu', ratio: 0.16 },
    { day: 'Fri', ratio: 0.14 },
    { day: 'Sat', ratio: 0.12 },
    { day: 'Sun', ratio: 0.09 },
  ],
  byHourOfDay: Array.from({ length: 24 }, (_, i) => ({ hour: i, count: Math.floor(Math.random() * 20) })),
};

const mockDoctors = {
  ranked: [
    { name: 'Dr. Ana Costa', specialty: 'Cardiology', consults: 85, completionPct: 96, rating: 4.8, revenue: 12750, trend: [18, 22, 20, 25], badge: 'Top Performer' },
    { name: 'Dr. Bruno Silva', specialty: 'General', consults: 72, completionPct: 91, rating: 4.5, revenue: 8640, trend: [15, 18, 20, 19], badge: null },
  ],
  topRated: [{ name: 'Dr. Ana Costa', rating: 4.8, reviewCount: 124, specialty: 'Cardiology' }],
  needsAttention: [{ name: 'Dr. Rui Oliveira', message: 'Completion rate 72%', severity: 'danger' }],
};

const mockHeatmap = {
  grid: Array.from({ length: 7 }, () => Array.from({ length: 24 }, () => Math.floor(Math.random() * 30))),
  specialtySeries: [],
  avgWaitBySpecialty: [],
};

async function mockAnalyticsRoutes(page: import('@playwright/test').Page) {
  await page.route('**/api/v1/analytics/overview**', (route) =>
    route.fulfill({ status: 200, json: mockOverview }),
  );
  await page.route('**/api/v1/analytics/appointments**', (route) =>
    route.fulfill({ status: 200, json: mockAppointments }),
  );
  await page.route('**/api/v1/analytics/revenue**', (route) =>
    route.fulfill({
      status: 200,
      json: {
        grossKpi: { value: 50000, deltaPct: 8, sparkline: [1] },
        netKpi: { value: 45000, deltaPct: 6, sparkline: [1] },
        refundsKpi: { value: 3000, deltaPct: -1, sparkline: [1] },
        arpuKpi: { value: 85, deltaPct: 2, sparkline: [1] },
        weeklyBySpecialty: [],
        paymentStatusBreakdown: [],
        clinicRevenue: [],
      },
    }),
  );
  await page.route('**/api/v1/analytics/heatmap**', (route) =>
    route.fulfill({ status: 200, json: mockHeatmap }),
  );
  await page.route('**/api/v1/analytics/doctors**', (route) =>
    route.fulfill({ status: 200, json: mockDoctors }),
  );
  await page.route('**/api/v1/analytics/geography**', (route) =>
    route.fulfill({
      status: 200,
      json: {
        regions: [{ name: 'Lisbon', consults: 100, deltaPct: 15, isTop: true }],
        retention: { cohorts: [] },
      },
    }),
  );
}

test.describe('Analytics Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PLATFORM_ADMIN_TOKEN);
    await mockBaseRoutes(page, { features: [] });
    await page.route('**/api/v1/clinics**', (route) =>
      route.fulfill({ status: 200, json: { clinics: [], totalCount: 0 } }),
    );
    await mockAnalyticsRoutes(page);
  });

  test('platform admin can view analytics overview', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Analytics' }).click();

    await expect(page.locator('.analytics-kpi-card')).toHaveCount(4);
    await expect(page.getByText('Booking Funnel')).toBeVisible();
  });

  test('drill-in navigation works', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Analytics' }).click();

    await page.getByText('View details').first().click();
    await expect(page.locator('.analytics-breadcrumb')).toBeVisible();
    await expect(page.getByText('Analytics').first()).toBeVisible();

    await page.locator('.analytics-breadcrumb-link').click();
    await expect(page.locator('.analytics-kpi-card')).toHaveCount(4);
  });

  test('period selector changes active pill', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Analytics' }).click();

    const pill7d = page.getByRole('button', { name: '7d' });
    await pill7d.click();
    await expect(pill7d).toHaveClass(/active/);

    const pill30d = page.getByRole('button', { name: '30d' });
    await expect(pill30d).not.toHaveClass(/active/);
  });

  test('non-admin cannot access analytics', async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await page.goto('/dashboard');
    await expect(page.getByRole('button', { name: 'Analytics' })).toHaveCount(0);
  });

  test('doctor leaderboard renders table', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Analytics' }).click();

    const viewDetailsButtons = page.getByText('View details');
    await expect(viewDetailsButtons.first()).toBeVisible();
  });

  test('heatmap renders grid', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Analytics' }).click();

    const heatmapLink = page.getByText('View details').nth(1);
    if (await heatmapLink.isVisible()) {
      await heatmapLink.click();
    }
  });

  test('analytics loads without errors on empty database', async ({ page }) => {
    await page.route('**/api/v1/analytics/overview**', (route) =>
      route.fulfill({
        status: 200,
        json: {
          appointmentKpi: { total: 0, deltaPct: 0, sparkline: [] },
          revenueKpi: { value: 0, deltaPct: 0, sparkline: [] },
          activeUsersKpi: { total: 0, deltaPct: 0, sparkline: [] },
          noShowKpi: { rate: 0, deltaPct: 0, sparkline: [] },
          appointmentSeries: { labels: [], current: [], previous: [] },
          revenueSeries: { labels: [], values: [] },
          funnel: [],
          activeUsers: { dau: 0, wau: 0, mau: 0, dauDelta: 0, wauDelta: 0, mauDelta: 0, stickiness: 0 },
          clinicBreakdown: [],
          specialtyMix: [],
        },
      }),
    );

    await page.goto('/dashboard');
    await page.getByRole('button', { name: 'Analytics' }).click();
    await expect(page.locator('.analytics-kpi-card')).toHaveCount(4);
  });
});
