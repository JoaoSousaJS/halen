import type { Meta, StoryObj } from '@storybook/react';
import FunnelChart from './FunnelChart';

const meta: Meta<typeof FunnelChart> = {
  title: 'Analytics/FunnelChart',
  component: FunnelChart,
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
type Story = StoryObj<typeof FunnelChart>;

export const Default: Story = {
  args: {
    stages: [
      { label: 'Booked', value: 1200 },
      { label: 'Scheduled', value: 1020 },
      { label: 'Completed', value: 850 },
      { label: 'Paid', value: 720 },
    ],
  },
};

export const SingleStage: Story = {
  args: {
    stages: [{ label: 'Booked', value: 500 }],
  },
};

export const Empty: Story = {
  args: {
    stages: [],
  },
};
