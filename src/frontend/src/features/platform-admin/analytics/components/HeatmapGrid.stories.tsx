import type { Meta, StoryObj } from '@storybook/react';
import HeatmapGrid from './HeatmapGrid';

/** Generate a 7x24 grid with realistic peaks during business hours on weekdays. */
function generateRealisticGrid(): number[][] {
  const grid: number[][] = [];

  for (let day = 0; day < 7; day++) {
    const row: number[] = [];
    const isWeekday = day < 5;

    for (let hour = 0; hour < 24; hour++) {
      const isBusinessHour = hour >= 9 && hour <= 17;

      let base: number;
      if (isWeekday && isBusinessHour) {
        base = 25 + Math.floor(Math.random() * 25); // 25-49
      } else if (isWeekday) {
        base = Math.floor(Math.random() * 10); // 0-9
      } else if (isBusinessHour) {
        base = 5 + Math.floor(Math.random() * 15); // 5-19
      } else {
        base = Math.floor(Math.random() * 5); // 0-4
      }

      row.push(base);
    }

    grid.push(row);
  }

  return grid;
}

function generateEmptyGrid(): number[][] {
  return Array.from({ length: 7 }, () => Array.from({ length: 24 }, () => 0));
}

const meta: Meta<typeof HeatmapGrid> = {
  title: 'Analytics/HeatmapGrid',
  component: HeatmapGrid,
  parameters: { layout: 'centered' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, maxWidth: 600, background: '#0b0e0c' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;
type Story = StoryObj<typeof HeatmapGrid>;

export const Default: Story = {
  args: { grid: generateRealisticGrid() },
};

export const Empty: Story = {
  args: { grid: generateEmptyGrid() },
};
