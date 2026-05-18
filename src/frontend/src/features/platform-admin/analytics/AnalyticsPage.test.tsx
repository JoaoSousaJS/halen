import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi } from 'vitest';

vi.mock('../../../shared/api/analytics', () => ({
  getAnalyticsOverview: () =>
    Promise.resolve({
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
    }),
  getAppointmentAnalytics: () =>
    Promise.resolve({
      bookedKpi: { total: 0, deltaPct: 0, sparkline: [] },
      completedKpi: { total: 0, deltaPct: 0, sparkline: [] },
      cancelledKpi: { total: 0, deltaPct: 0, sparkline: [] },
      avgLeadTimeKpi: { value: 0, deltaPct: 0, sparkline: [] },
      dailySeries: { labels: [], current: [], previous: [] },
      byDayOfWeek: [],
      byHourOfDay: [],
    }),
  getRevenueAnalytics: () =>
    Promise.resolve({
      grossKpi: { value: 0, deltaPct: 0, sparkline: [] },
      netKpi: { value: 0, deltaPct: 0, sparkline: [] },
      refundsKpi: { value: 0, deltaPct: 0, sparkline: [] },
      arpuKpi: { value: 0, deltaPct: 0, sparkline: [] },
      weeklyBySpecialty: [],
      paymentStatusBreakdown: [],
      clinicRevenue: [],
    }),
  getHeatmapAnalytics: () =>
    Promise.resolve({
      grid: Array.from({ length: 7 }, () => Array(24).fill(0)),
      specialtySeries: [],
      avgWaitBySpecialty: [],
    }),
  getDoctorAnalytics: () =>
    Promise.resolve({
      ranked: [],
      topRated: [],
      needsAttention: [],
    }),
  getGeographyAnalytics: () =>
    Promise.resolve({
      regions: [],
      retention: { cohorts: [] },
    }),
}));

import AnalyticsPage from './AnalyticsPage';

function renderPage(
  activeView: 'overview' | 'appointments' | 'revenue' | 'heatmap' | 'doctors' | 'geography' = 'overview',
  onNavigate = vi.fn(),
) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return {
    onNavigate,
    ...render(
      <QueryClientProvider client={client}>
        <AnalyticsPage activeView={activeView} onNavigate={onNavigate} />
      </QueryClientProvider>,
    ),
  };
}

describe('AnalyticsPage', () => {
  it('renders period pills', () => {
    renderPage();
    expect(screen.getByRole('button', { name: '30d' })).toBeDefined();
  });

  it('renders overview when activeView is overview', async () => {
    renderPage('overview');
    expect(await screen.findByText('Appointments')).toBeDefined();
  });

  it('renders breadcrumb on drill-in views', async () => {
    renderPage('appointments');
    expect(await screen.findByText('Analytics')).toBeDefined();
  });

  it('clicking breadcrumb navigates back to overview', async () => {
    const user = userEvent.setup();
    const { onNavigate } = renderPage('appointments');
    const breadcrumb = await screen.findByText('Analytics');
    await user.click(breadcrumb);
    expect(onNavigate).toHaveBeenCalledWith('overview');
  });
});
