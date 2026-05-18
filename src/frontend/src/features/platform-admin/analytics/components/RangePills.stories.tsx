import type { Meta, StoryObj } from '@storybook/react';
import { fn } from '@storybook/test';
import RangePills from './RangePills';

const meta: Meta<typeof RangePills> = {
  title: 'Analytics/RangePills',
  component: RangePills,
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
type Story = StoryObj<typeof RangePills>;

export const Default: Story = {
  args: { value: '30d', onSelect: fn() },
};

export const SevenDay: Story = {
  args: { value: '7d', onSelect: fn() },
};
