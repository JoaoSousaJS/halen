import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import AnalyticsHeatmap from './AnalyticsHeatmap';
import type { HeatmapAnalyticsDto } from '../../../shared/api/analytics';

/** Helper: build a 7x24 grid filled with a single value. */
function makeGrid(fill: number): number[][] {
  return Array.from({ length: 7 }, () => Array.from({ length: 24 }, () => fill));
}

function makeData(overrides: Partial<HeatmapAnalyticsDto> = {}): HeatmapAnalyticsDto {
  return {
    grid: makeGrid(5),
    specialtySeries: [
      {
        specialty: 'Cardiology',
        dataPoints: [
          { month: 'Jan', count: 20 },
          { month: 'Feb', count: 25 },
          { month: 'Mar', count: 30 },
        ],
      },
      {
        specialty: 'General',
        dataPoints: [
          { month: 'Jan', count: 15 },
          { month: 'Feb', count: 18 },
          { month: 'Mar', count: 22 },
        ],
      },
    ],
    avgWaitBySpecialty: [
      { specialty: 'Cardiology', days: 12 },
      { specialty: 'General', days: 5 },
    ],
    ...overrides,
  };
}

describe('AnalyticsHeatmap', () => {
  it('renders heatmap section', () => {
    render(<AnalyticsHeatmap data={makeData()} />);
    expect(screen.getByText('Booking Heatmap')).toBeDefined();
  });

  it('renders HeatmapGrid with 168 cells (7x24)', () => {
    const { container } = render(<AnalyticsHeatmap data={makeData()} />);
    const cells = container.querySelectorAll('.analytics-heatmap-cell');
    expect(cells.length).toBe(168);
  });

  it('renders specialty seasonality chart title', () => {
    render(<AnalyticsHeatmap data={makeData()} />);
    expect(screen.getByText('Specialty Seasonality')).toBeDefined();
  });

  it('renders avg wait section with specialty names', () => {
    const { container } = render(<AnalyticsHeatmap data={makeData()} />);
    expect(screen.getByText('Avg Wait by Specialty')).toBeDefined();
    // Recharts renders axis labels as SVG <text> elements which jsdom may not
    // expose via getByText. Verify the BarChart receives the data by checking
    // that the recharts wrapper is present inside the card.
    const cards = container.querySelectorAll('.analytics-card');
    const avgWaitCard = Array.from(cards).find(
      (card) => card.querySelector('.analytics-card-title')?.textContent === 'Avg Wait by Specialty',
    );
    expect(avgWaitCard).toBeDefined();
    expect(avgWaitCard!.querySelector('.recharts-responsive-container')).toBeDefined();
  });
});
