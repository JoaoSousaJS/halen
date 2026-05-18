import type { Meta, StoryObj } from '@storybook/react';
import KpiCard from './KpiCard';

const meta: Meta<typeof KpiCard> = {
  title: 'Analytics/KpiCard',
  component: KpiCard,
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
type Story = StoryObj<typeof KpiCard>;

export const PositiveDelta: Story = {
  args: {
    label: 'Appointments',
    value: '1,234',
    deltaPct: 12.5,
    sparkline: [1, 3, 2, 5, 4, 7, 6, 9],
  },
};

export const NegativeDelta: Story = {
  args: {
    label: 'No-Show Rate',
    value: '8.5%',
    deltaPct: -3.2,
    sparkline: [10, 9, 8, 7, 8, 7, 6, 5],
  },
};

export const ZeroDelta: Story = {
  args: {
    label: 'Active Users',
    value: '890',
    deltaPct: 0,
    sparkline: [5, 5, 5, 5, 5],
  },
};

export const LargeValue: Story = {
  args: {
    label: 'Revenue',
    value: '$125.0K',
    deltaPct: 22.1,
    sparkline: [80, 85, 90, 95, 100, 110, 125],
  },
};
