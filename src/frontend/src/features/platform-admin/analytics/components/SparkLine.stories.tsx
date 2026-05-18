import type { Meta, StoryObj } from '@storybook/react';
import SparkLine from './SparkLine';

const meta: Meta<typeof SparkLine> = {
  title: 'Analytics/SparkLine',
  component: SparkLine,
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
type Story = StoryObj<typeof SparkLine>;

export const Uptrend: Story = {
  args: { data: [2, 5, 3, 8, 7, 12, 15] },
};

export const Downtrend: Story = {
  args: { data: [20, 18, 15, 10, 8, 5, 2] },
};

export const Flat: Story = {
  args: { data: [5, 5, 5, 5, 5] },
};

export const SinglePoint: Story = {
  args: { data: [10] },
};
