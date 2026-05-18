import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import KpiCard from './KpiCard';

describe('KpiCard', () => {
  it('renders label and value', () => {
    render(<KpiCard label="Total Users" value="1,234" deltaPct={5} sparkline={[1, 2, 3]} />);

    expect(screen.getByText('Total Users')).toBeDefined();
    expect(screen.getByText('1,234')).toBeDefined();
  });

  it('renders positive delta with .positive class and "+" prefix', () => {
    const { container } = render(
      <KpiCard label="Revenue" value="$5.6K" deltaPct={12} sparkline={[1, 2]} />,
    );

    const badge = container.querySelector('.positive');
    expect(badge).toBeDefined();
    expect(badge!.textContent).toBe('+12%');
  });

  it('renders negative delta with .negative class', () => {
    const { container } = render(
      <KpiCard label="Churn" value="2.3%" deltaPct={-8} sparkline={[3, 2, 1]} />,
    );

    const badge = container.querySelector('.negative');
    expect(badge).toBeDefined();
    expect(badge!.textContent).toBe('-8%');
  });

  it('renders zero delta without positive/negative class', () => {
    const { container } = render(
      <KpiCard label="Stable" value="100" deltaPct={0} sparkline={[5, 5]} />,
    );

    expect(container.querySelector('.positive')).toBeNull();
    expect(container.querySelector('.negative')).toBeNull();

    // The delta badge should still render "0%"
    const card = container.querySelector('.analytics-kpi-card');
    expect(card).toBeDefined();
    expect(card!.textContent).toContain('0%');
  });

  it('renders SparkLine with data', () => {
    const data = [10, 20, 30];
    const { container } = render(
      <KpiCard label="Sessions" value="500" deltaPct={3} sparkline={data} />,
    );

    const svg = container.querySelector('svg');
    expect(svg).toBeDefined();

    const polyline = container.querySelector('polyline');
    expect(polyline).toBeDefined();
    const points = polyline!.getAttribute('points')!.trim().split(/\s+/);
    expect(points.length).toBe(data.length);
  });
});
