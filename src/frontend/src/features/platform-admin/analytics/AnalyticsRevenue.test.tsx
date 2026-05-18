import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import AnalyticsRevenue from './AnalyticsRevenue';
import type { RevenueAnalyticsDto } from '../../../shared/api/analytics';

// Recharts ResponsiveContainer renders at 0 width in jsdom, so charts produce
// no SVG children.  Mock it to give children a fixed width/height.
vi.mock('recharts', async () => {
  const actual = await vi.importActual<typeof import('recharts')>('recharts');
  return {
    ...actual,
    ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
      <div style={{ width: 500, height: 300 }}>{children}</div>
    ),
  };
});

function makeRevenueData(
  overrides: Partial<RevenueAnalyticsDto> = {},
): RevenueAnalyticsDto {
  return {
    grossKpi: { value: 125000, deltaPct: 12.3, sparkline: [100, 110, 125] },
    netKpi: { value: 98000, deltaPct: 8.7, sparkline: [80, 90, 98] },
    refundsKpi: { value: 4500, deltaPct: -3.2, sparkline: [6, 5, 4] },
    arpuKpi: { value: 85, deltaPct: 2.1, sparkline: [80, 82, 85] },
    weeklyBySpecialty: [
      {
        week: 'W18',
        segments: [
          { specialty: 'General', amount: 5000 },
          { specialty: 'Cardiology', amount: 8000 },
        ],
      },
      {
        week: 'W19',
        segments: [
          { specialty: 'General', amount: 5500 },
          { specialty: 'Cardiology', amount: 9000 },
        ],
      },
    ],
    paymentStatusBreakdown: [
      { label: 'Paid', amount: 90000, percentage: 72 },
      { label: 'Pending', amount: 25000, percentage: 20 },
      { label: 'Refunded', amount: 10000, percentage: 8 },
    ],
    clinicRevenue: [
      { name: 'Lisbon Sul', consults: 320, arpu: 95, revenue: 30400, deltaPct: 5.2 },
      { name: 'Porto Centro', consults: 280, arpu: 88, revenue: 24640, deltaPct: -1.8 },
    ],
    ...overrides,
  };
}

describe('AnalyticsRevenue', () => {
  it('renders 4 KPI cards', () => {
    render(<AnalyticsRevenue data={makeRevenueData()} />);
    const cards = document.querySelectorAll('.analytics-kpi-card');
    expect(cards.length).toBe(4);
  });

  it('renders KPI labels', () => {
    render(<AnalyticsRevenue data={makeRevenueData()} />);
    const kpiGrid = document.querySelector('.analytics-kpi-grid')!;
    expect(kpiGrid).toBeDefined();
    expect(kpiGrid.textContent).toContain('Gross Revenue');
    expect(kpiGrid.textContent).toContain('Net Revenue');
    expect(kpiGrid.textContent).toContain('Refunds');
    expect(kpiGrid.textContent).toContain('ARPU');
  });

  it('renders clinic revenue table with correct column headers', () => {
    render(<AnalyticsRevenue data={makeRevenueData()} />);
    const table = document.querySelector('.analytics-table')!;
    expect(table).toBeDefined();
    const headers = table.querySelectorAll('th');
    const headerTexts = Array.from(headers).map((th) => th.textContent);
    expect(headerTexts).toContain('Clinic');
    expect(headerTexts).toContain('Consults');
    expect(headerTexts).toContain('ARPU');
    expect(headerTexts).toContain('Revenue');
    expect(headerTexts).toContain('Trend');
  });

  it('renders clinic names in table rows', () => {
    render(<AnalyticsRevenue data={makeRevenueData()} />);
    expect(screen.getByText('Lisbon Sul')).toBeDefined();
    expect(screen.getByText('Porto Centro')).toBeDefined();
  });
});
