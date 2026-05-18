import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import IberiaMap from './IberiaMap';
import type { RegionDto } from '../../../../shared/api/analytics';

function makeRegion(overrides: Partial<RegionDto> = {}): RegionDto {
  return {
    name: 'Lisbon',
    consults: 100,
    deltaPct: 10,
    isTop: false,
    ...overrides,
  };
}

describe('IberiaMap', () => {
  it('renders SVG map element', () => {
    render(<IberiaMap regions={[makeRegion()]} />);
    const svg = document.querySelector('.analytics-map');
    expect(svg).toBeDefined();
    expect(svg?.tagName).toBe('svg');
  });

  it('renders circle bubbles for known cities', () => {
    render(
      <IberiaMap
        regions={[
          makeRegion({ name: 'Lisbon', consults: 50 }),
          makeRegion({ name: 'Porto', consults: 30 }),
        ]}
      />,
    );
    const circles = document.querySelectorAll('.analytics-map-bubble');
    expect(circles.length).toBe(2);
  });

  it('renders region list for unknown cities', () => {
    render(
      <IberiaMap
        regions={[makeRegion({ name: 'UnknownCity', consults: 20 })]}
      />,
    );
    expect(screen.getByText('UnknownCity')).toBeDefined();
    expect(screen.getByText('20')).toBeDefined();
  });

  it('scales bubble radius by consults relative to max', () => {
    render(
      <IberiaMap
        regions={[
          makeRegion({ name: 'Lisbon', consults: 100 }),
          makeRegion({ name: 'Porto', consults: 50 }),
        ]}
      />,
    );
    const circles = document.querySelectorAll('.analytics-map-bubble');
    const r1 = Number(circles[0].getAttribute('r'));
    const r2 = Number(circles[1].getAttribute('r'));
    expect(r1).toBeGreaterThan(r2);
  });

  it('renders empty state without error', () => {
    render(<IberiaMap regions={[]} />);
    const svg = document.querySelector('.analytics-map');
    expect(svg).toBeDefined();
  });
});
