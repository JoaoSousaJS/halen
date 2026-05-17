import type { Meta, StoryObj } from '@storybook/react';
import { Button } from './Button';

const meta: Meta<typeof Button> = {
  title: 'Shared/Button',
  component: Button,
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

type Story = StoryObj<typeof Button>;

export const Default: Story = {
  args: { children: 'Button' },
};

export const Primary: Story = {
  args: { variant: 'primary', children: 'Save changes' },
};

export const Danger: Story = {
  args: { variant: 'danger', children: 'Delete account' },
};

export const Ghost: Story = {
  args: { variant: 'ghost', children: 'Cancel' },
};

export const Small: Story = {
  args: { size: 'sm', children: 'Small button' },
};

export const SmallPrimary: Story = {
  args: { variant: 'primary', size: 'sm', children: 'Submit' },
};

export const Block: Story = {
  args: { variant: 'primary', block: true, children: 'Full width' },
  decorators: [
    (Story) => (
      <div style={{ width: 320 }}>
        <Story />
      </div>
    ),
  ],
};

export const Disabled: Story = {
  args: { variant: 'primary', disabled: true, children: 'Cannot click' },
};

export const DisabledDanger: Story = {
  args: { variant: 'danger', disabled: true, children: 'Cannot delete' },
};

export const WithAriaLabel: Story = {
  args: { variant: 'ghost', ariaLabel: 'Close dialog', children: '×' },
};

export const AllVariants: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
      <Button>Default</Button>
      <Button variant="primary">Primary</Button>
      <Button variant="danger">Danger</Button>
      <Button variant="ghost">Ghost</Button>
      <Button size="sm">Small</Button>
      <Button variant="primary" size="sm">Small Primary</Button>
      <Button disabled>Disabled</Button>
    </div>
  ),
};
