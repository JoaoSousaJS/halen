import type { Meta, StoryObj } from '@storybook/react';
import { Chip } from './Chip';

const meta: Meta<typeof Chip> = {
  title: 'Shared/Chip',
  component: Chip,
  parameters: { layout: 'centered' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, minHeight: 200, background: '#0b0e0c' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof Chip>;

export const Default: Story = {
  args: { status: 'Pending' },
};

export const Good: Story = {
  args: { status: 'Active', variant: 'good' },
};

export const Danger: Story = {
  args: { status: 'Cancelled', variant: 'danger' },
};

export const Warn: Story = {
  args: { status: 'Expiring soon', variant: 'warn' },
};

export const AllVariants: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
      <Chip status="Scheduled" />
      <Chip status="Completed" variant="good" />
      <Chip status="Cancelled" variant="danger" />
      <Chip status="Pending review" variant="warn" />
    </div>
  ),
};

export const LongText: Story = {
  args: { status: 'Awaiting doctor confirmation', variant: 'warn' },
};

export const NoVariant: Story = {
  args: { status: 'Unknown status' },
};
