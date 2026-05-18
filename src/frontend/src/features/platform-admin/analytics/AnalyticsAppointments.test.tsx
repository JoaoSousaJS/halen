import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import AnalyticsAppointments from './AnalyticsAppointments';
import type { AppointmentAnalyticsDto } from '../../../shared/api/analytics';

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

function makeAppointmentData(
  overrides: Partial<AppointmentAnalyticsDto> = {},
): AppointmentAnalyticsDto {
  return {
    bookedKpi: { total: 520, deltaPct: 8.3, sparkline: [10, 12, 14] },
    completedKpi: { total: 480, deltaPct: 5.1, sparkline: [9, 11, 13] },
    cancelledKpi: { total: 40, deltaPct: -2.0, sparkline: [3, 2, 1] },
    avgLeadTimeKpi: { value: 3.2, deltaPct: -10.5, sparkline: [4, 3, 3] },
    dailySeries: {
      labels: ['May 1', 'May 2', 'May 3'],
      current: [15, 20, 18],
      previous: [12, 17, 14],
    },
    byDayOfWeek: [
      { day: 'Mon', ratio: 0.18 },
      { day: 'Tue', ratio: 0.16 },
      { day: 'Wed', ratio: 0.15 },
      { day: 'Thu', ratio: 0.14 },
      { day: 'Fri', ratio: 0.17 },
      { day: 'Sat', ratio: 0.12 },
      { day: 'Sun', ratio: 0.08 },
    ],
    byHourOfDay: [
      { hour: 8, count: 30 },
      { hour: 9, count: 55 },
      { hour: 10, count: 70 },
      { hour: 14, count: 60 },
    ],
    ...overrides,
  };
}

describe('AnalyticsAppointments', () => {
  it('renders 4 KPI cards', () => {
    render(<AnalyticsAppointments data={makeAppointmentData()} />);
    const cards = document.querySelectorAll('.analytics-kpi-card');
    expect(cards.length).toBe(4);
  });

  it('renders KPI labels', () => {
    render(<AnalyticsAppointments data={makeAppointmentData()} />);
    expect(screen.getByText('Booked')).toBeDefined();
    expect(screen.getByText('Completed')).toBeDefined();
    expect(screen.getByText('Cancelled')).toBeDefined();
    expect(screen.getByText('Avg Lead Time')).toBeDefined();
  });

  it('renders byDayOfWeek day labels', () => {
    render(<AnalyticsAppointments data={makeAppointmentData()} />);
    expect(screen.getByText(/Mon/)).toBeDefined();
    expect(screen.getByText(/Tue/)).toBeDefined();
    expect(screen.getByText(/Wed/)).toBeDefined();
    expect(screen.getByText(/Thu/)).toBeDefined();
    expect(screen.getByText(/Fri/)).toBeDefined();
    expect(screen.getByText(/Sat/)).toBeDefined();
    expect(screen.getByText(/Sun/)).toBeDefined();
  });

  it('renders byHourOfDay data', () => {
    render(<AnalyticsAppointments data={makeAppointmentData()} />);
    expect(screen.getByText(/8:00/)).toBeDefined();
    expect(screen.getByText(/9:00/)).toBeDefined();
    expect(screen.getByText(/10:00/)).toBeDefined();
    expect(screen.getByText(/14:00/)).toBeDefined();
  });
});
