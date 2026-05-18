import type { Meta, StoryObj } from '@storybook/react';
import CohortMatrix from './CohortMatrix';

const meta: Meta<typeof CohortMatrix> = {
  title: 'Analytics/CohortMatrix',
  component: CohortMatrix,
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
type Story = StoryObj<typeof CohortMatrix>;

export const Default: Story = {
  args: {
    cohorts: [
      { cohortLabel: 'Week 1', weeks: [100, 65, 42, 30] },
      { cohortLabel: 'Week 2', weeks: [100, 58, 38] },
      { cohortLabel: 'Week 3', weeks: [100, 70] },
      { cohortLabel: 'Week 4', weeks: [100] },
    ],
  },
};

export const Sparse: Story = {
  args: {
    cohorts: [
      { cohortLabel: 'Week 1', weeks: [100, 10, 2] },
      { cohortLabel: 'Week 2', weeks: [100, 5] },
    ],
  },
};
