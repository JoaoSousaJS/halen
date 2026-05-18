import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import CohortMatrix from './CohortMatrix';

const sampleCohorts = [
  { cohortLabel: 'Jan 2026', weeks: [100, 80, 67, 55] },
  { cohortLabel: 'Feb 2026', weeks: [100, 72, 60] },
  { cohortLabel: 'Mar 2026', weeks: [100, 90] },
];

describe('CohortMatrix', () => {
  it('renders correct number of rows matching cohorts length', () => {
    const { container } = render(<CohortMatrix cohorts={sampleCohorts} />);

    // Each cohort produces one row of cells. Count distinct row sets
    // by counting cohort label elements (one per row).
    const labels = container.querySelectorAll('.analytics-cohort-label');
    expect(labels.length).toBe(3);
  });

  it('renders cohort labels', () => {
    render(<CohortMatrix cohorts={sampleCohorts} />);

    expect(screen.getByText('Jan 2026')).toBeDefined();
    expect(screen.getByText('Feb 2026')).toBeDefined();
    expect(screen.getByText('Mar 2026')).toBeDefined();
  });

  it('renders percentage values in cells', () => {
    render(<CohortMatrix cohorts={sampleCohorts} />);

    // Check specific formatted values from the first cohort
    expect(screen.getByText('80%')).toBeDefined();
    expect(screen.getByText('67%')).toBeDefined();
    expect(screen.getByText('55%')).toBeDefined();
  });

  it('Week 0 always shows "100%"', () => {
    render(<CohortMatrix cohorts={sampleCohorts} />);

    // Each cohort has week 0 = 100. So "100%" should appear 3 times.
    const allCells = screen.getAllByText('100%');
    expect(allCells.length).toBe(3);
  });

  it('empty cohorts array renders without error', () => {
    const { container } = render(<CohortMatrix cohorts={[]} />);

    const matrix = container.querySelector('.analytics-cohort');
    expect(matrix).toBeDefined();

    // No data rows
    const labels = container.querySelectorAll('.analytics-cohort-label');
    expect(labels.length).toBe(0);
  });
});
