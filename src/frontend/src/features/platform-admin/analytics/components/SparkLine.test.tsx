import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import SparkLine from './SparkLine';

describe('SparkLine', () => {
  it('renders SVG element with correct dimensions', () => {
    const { container } = render(<SparkLine data={[1, 2, 3]} width={100} height={30} />);

    const svg = container.querySelector('svg');
    expect(svg).toBeDefined();
    expect(svg!.getAttribute('width')).toBe('100');
    expect(svg!.getAttribute('height')).toBe('30');
  });

  it('renders polyline with correct number of coordinate pairs', () => {
    const data = [10, 20, 30, 40];
    const { container } = render(<SparkLine data={data} />);

    const polyline = container.querySelector('polyline');
    expect(polyline).toBeDefined();

    const points = polyline!.getAttribute('points')!;
    const pairs = points.trim().split(/\s+/);
    expect(pairs.length).toBe(data.length);
  });

  it('uses default color when none specified', () => {
    const { container } = render(<SparkLine data={[1, 2]} />);

    const polyline = container.querySelector('polyline');
    expect(polyline).toBeDefined();
    expect(polyline!.getAttribute('stroke')).toBe('var(--accent)');
  });

  it('empty data renders SVG with no polyline', () => {
    const { container } = render(<SparkLine data={[]} />);

    const svg = container.querySelector('svg');
    expect(svg).toBeDefined();

    const polyline = container.querySelector('polyline');
    expect(polyline).toBeNull();
  });
});
