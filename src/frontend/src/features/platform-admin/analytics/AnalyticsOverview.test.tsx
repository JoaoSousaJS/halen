import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import AnalyticsOverview from './AnalyticsOverview';
import type { AnalyticsOverviewDto } from '../../../shared/api/analytics';

function makeOverviewData(overrides: Partial<AnalyticsOverviewDto> = {}): AnalyticsOverviewDto {
  return {
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
    clinicBreakdown: [
      { name: 'Lisbon Sul', value: 400 },
      { name: 'Porto Centro', value: 300 },
    ],
    specialtyMix: [
      { label: 'Cardiology', value: 500 },
      { label: 'General', value: 300 },
    ],
    ...overrides,
  };
}

describe('AnalyticsOverview', () => {
  it('renders 4 KPI cards', () => {
    render(<AnalyticsOverview data={makeOverviewData()} onNavigate={vi.fn()} />);
    const cards = document.querySelectorAll('.analytics-kpi-card');
    expect(cards.length).toBe(4);
  });

  it('renders KPI labels', () => {
    render(<AnalyticsOverview data={makeOverviewData()} onNavigate={vi.fn()} />);
    const labels = document.querySelectorAll('.analytics-kpi-label');
    const labelTexts = Array.from(labels).map((el) => el.textContent);
    expect(labelTexts).toContain('Appointments');
    expect(labelTexts).toContain('Revenue');
    expect(labelTexts).toContain('Active Users');
    expect(labelTexts).toContain('No-Show Rate');
  });

  it('renders funnel stages', () => {
    render(<AnalyticsOverview data={makeOverviewData()} onNavigate={vi.fn()} />);
    expect(screen.getByText(/Booked/)).toBeDefined();
    expect(screen.getByText(/Paid/)).toBeDefined();
  });

  it('renders clinic breakdown card', () => {
    render(<AnalyticsOverview data={makeOverviewData()} onNavigate={vi.fn()} />);
    expect(screen.getByText('Clinic Breakdown')).toBeDefined();
    expect(screen.getByText('Specialty Mix')).toBeDefined();
  });

  it('renders card-link actions that navigate to drill-ins', async () => {
    const user = userEvent.setup();
    const onNavigate = vi.fn();
    render(<AnalyticsOverview data={makeOverviewData()} onNavigate={onNavigate} />);

    const viewDetails = screen.getAllByText('View details');
    expect(viewDetails.length).toBeGreaterThan(0);

    await user.click(viewDetails[0]);
    expect(onNavigate).toHaveBeenCalled();
  });

  it('renders active users metrics', () => {
    render(<AnalyticsOverview data={makeOverviewData()} onNavigate={vi.fn()} />);
    expect(screen.getByText('DAU')).toBeDefined();
    expect(screen.getByText('WAU')).toBeDefined();
    expect(screen.getByText('MAU')).toBeDefined();
  });
});
