import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import FunnelChart from './FunnelChart';

const stages = [
  { label: 'Visitors', value: 1000 },
  { label: 'Signups', value: 400 },
  { label: 'Appointments', value: 200 },
];

describe('FunnelChart', () => {
  it('renders correct number of funnel bars', () => {
    const { container } = render(<FunnelChart stages={stages} />);

    const bars = container.querySelectorAll('.analytics-funnel-bar');
    expect(bars.length).toBe(3);
  });

  it('renders stage labels with values', () => {
    render(<FunnelChart stages={stages} />);

    expect(screen.getByText('Visitors: 1000')).toBeDefined();
    expect(screen.getByText('Signups: 400')).toBeDefined();
    expect(screen.getByText('Appointments: 200')).toBeDefined();
  });

  it('first bar has 100% width', () => {
    const { container } = render(<FunnelChart stages={stages} />);

    const fills = container.querySelectorAll('.analytics-funnel-fill');
    expect(fills[0]).toBeDefined();
    expect((fills[0] as HTMLElement).style.width).toBe('100%');
  });

  it('subsequent bars have proportional widths', () => {
    const { container } = render(<FunnelChart stages={stages} />);

    const fills = container.querySelectorAll('.analytics-funnel-fill');
    expect((fills[1] as HTMLElement).style.width).toBe('40%');
    expect((fills[2] as HTMLElement).style.width).toBe('20%');
  });

  it('shows conversion percentages between stages', () => {
    render(<FunnelChart stages={stages} />);

    // 400/1000 = 40%
    expect(screen.getByText('→ 40%')).toBeDefined();
    // 200/400 = 50%
    expect(screen.getByText('→ 50%')).toBeDefined();
  });

  it('empty stages array renders empty container', () => {
    const { container } = render(<FunnelChart stages={[]} />);

    const funnel = container.querySelector('.analytics-funnel');
    expect(funnel).toBeDefined();

    const bars = container.querySelectorAll('.analytics-funnel-bar');
    expect(bars.length).toBe(0);
  });
});
