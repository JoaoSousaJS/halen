import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import HeatmapGrid from './HeatmapGrid';

/** Helper: build a 7×24 grid filled with a single value. */
function makeGrid(fill: number): number[][] {
  return Array.from({ length: 7 }, () => Array.from({ length: 24 }, () => fill));
}

describe('HeatmapGrid', () => {
  it('renders 168 cells (7 × 24) with class .analytics-heatmap-cell', () => {
    const { container } = render(<HeatmapGrid grid={makeGrid(0)} />);

    const cells = container.querySelectorAll('.analytics-heatmap-cell');
    expect(cells.length).toBe(168);
  });

  it('cell with max value has opacity 1', () => {
    const grid = makeGrid(0);
    grid[0][0] = 50; // only non-zero value → this is the max

    const { container } = render(<HeatmapGrid grid={grid} />);

    const cells = container.querySelectorAll('.analytics-heatmap-cell');
    // First data cell (Mon, hour 0) should have opacity 1
    expect(cells[0].getAttribute('style')).toContain('opacity: 1');
  });

  it('cell with zero value has low opacity (0.05)', () => {
    const grid = makeGrid(0);
    grid[3][12] = 100; // set one cell so max ≠ 0

    const { container } = render(<HeatmapGrid grid={grid} />);

    // Pick a cell we know is zero — Mon hour 0 (first cell)
    const cells = container.querySelectorAll('.analytics-heatmap-cell');
    expect(cells[0].getAttribute('style')).toContain('opacity: 0.05');
  });

  it('renders day labels (Mon, Tue, Wed, Thu, Fri, Sat, Sun)', () => {
    const { container } = render(<HeatmapGrid grid={makeGrid(0)} />);

    const labels = container.querySelectorAll('.analytics-heatmap-label');
    const labelTexts = Array.from(labels).map((el) => el.textContent);

    for (const day of ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']) {
      expect(labelTexts).toContain(day);
    }
  });

  it('empty grid (all zeros) renders without error', () => {
    const { container } = render(<HeatmapGrid grid={makeGrid(0)} />);

    const heatmap = container.querySelector('.analytics-heatmap');
    expect(heatmap).toBeDefined();

    const cells = container.querySelectorAll('.analytics-heatmap-cell');
    expect(cells.length).toBe(168);
  });
});
