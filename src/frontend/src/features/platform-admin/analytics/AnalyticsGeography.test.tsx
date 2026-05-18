import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import AnalyticsGeography from './AnalyticsGeography';
import type { GeographyAnalyticsDto } from '../../../shared/api/analytics';

function makeData(
  overrides: Partial<GeographyAnalyticsDto> = {},
): GeographyAnalyticsDto {
  return {
    regions: [
      { name: 'Lisbon', consults: 100, deltaPct: 15, isTop: true },
      { name: 'Porto', consults: 60, deltaPct: -5, isTop: false },
    ],
    retention: {
      cohorts: [
        { cohortLabel: 'May 5', weeks: [100, 66.7, 33.3] },
        { cohortLabel: 'May 12', weeks: [100, 50] },
      ],
    },
    ...overrides,
  };
}

describe('AnalyticsGeography', () => {
  it('renders Geography card title', () => {
    render(<AnalyticsGeography data={makeData()} />);
    expect(screen.getByText('Geography')).toBeDefined();
  });

  it('renders region names in list', () => {
    render(<AnalyticsGeography data={makeData()} />);
    expect(screen.getByText('Lisbon')).toBeDefined();
    expect(screen.getByText('Porto')).toBeDefined();
  });

  it('shows "Top" badge for isTop region', () => {
    render(<AnalyticsGeography data={makeData()} />);
    const topBadges = document.querySelectorAll('.analytics-region-top');
    expect(topBadges.length).toBe(1);
    expect(topBadges[0].textContent).toBe('Top');
  });

  it('renders Cohort Retention card title', () => {
    render(<AnalyticsGeography data={makeData()} />);
    expect(screen.getByText('Cohort Retention')).toBeDefined();
  });

  it('empty regions renders without error', () => {
    const data = makeData({ regions: [] });
    expect(() => render(<AnalyticsGeography data={data} />)).not.toThrow();
  });
});
