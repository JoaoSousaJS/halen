import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect } from 'vitest';
import AnalyticsDoctors from './AnalyticsDoctors';
import type { DoctorAnalyticsDto } from '../../../shared/api/analytics';

function makeData(overrides: Partial<DoctorAnalyticsDto> = {}): DoctorAnalyticsDto {
  return {
    ranked: [
      { name: 'Dr. Ana', specialty: 'Cardiology', consults: 50, completionPct: 92, rating: 4.5, revenue: 5000, trend: [10, 12, 15, 18], badge: 'Top Performer' },
      { name: 'Dr. Bruno', specialty: 'General', consults: 30, completionPct: 88, rating: 4.2, revenue: 3000, trend: [8, 9, 10, 11], badge: null },
    ],
    topRated: [{ name: 'Dr. Ana', rating: 4.5, reviewCount: 80, specialty: 'Cardiology' }],
    needsAttention: [{ name: 'Dr. Carlos', message: 'Completion rate 72% (below 85%)', severity: 'danger' }],
    ...overrides,
  };
}

describe('AnalyticsDoctors', () => {
  it('renders leaderboard rows for ranked doctors', () => {
    const { container } = render(<AnalyticsDoctors data={makeData()} />);
    const rows = container.querySelectorAll('.analytics-leaderboard-row');
    expect(rows.length).toBe(2);
  });

  it('renders doctor names in leaderboard', () => {
    const { container } = render(<AnalyticsDoctors data={makeData()} />);
    const names = container.querySelectorAll('.analytics-leaderboard-name');
    const nameTexts = Array.from(names).map((el) => el.textContent);
    expect(nameTexts).toContain('Dr. Ana');
    expect(nameTexts).toContain('Dr. Bruno');
  });

  it('renders specialty filter pills including "All"', () => {
    const { container } = render(<AnalyticsDoctors data={makeData()} />);
    const pills = container.querySelectorAll('.analytics-filter-pill');
    const pillTexts = Array.from(pills).map((el) => el.textContent);
    expect(pillTexts).toContain('All');
    expect(pillTexts).toContain('Cardiology');
    expect(pillTexts).toContain('General');
  });

  it('clicking specialty filter shows only that specialty\'s doctors', async () => {
    const user = userEvent.setup();
    const { container } = render(<AnalyticsDoctors data={makeData()} />);

    // Click 'Cardiology' filter pill
    const pills = container.querySelectorAll('.analytics-filter-pill');
    const cardiologyPill = Array.from(pills).find((el) => el.textContent === 'Cardiology')!;
    await user.click(cardiologyPill);

    const rows = container.querySelectorAll('.analytics-leaderboard-row');
    expect(rows.length).toBe(1);
    const names = container.querySelectorAll('.analytics-leaderboard-name');
    const nameTexts = Array.from(names).map((el) => el.textContent);
    expect(nameTexts).toContain('Dr. Ana');
    expect(nameTexts).not.toContain('Dr. Bruno');
  });

  it('renders needs attention alerts with correct severity classes', () => {
    const { container } = render(<AnalyticsDoctors data={makeData()} />);
    expect(screen.getByText('Dr. Carlos')).toBeDefined();
    const alert = container.querySelector('.analytics-alert.danger');
    expect(alert).not.toBeNull();
  });

  it('empty ranked renders no leaderboard rows', () => {
    const { container } = render(<AnalyticsDoctors data={makeData({ ranked: [] })} />);
    const rows = container.querySelectorAll('.analytics-leaderboard-row');
    expect(rows.length).toBe(0);
  });
});
