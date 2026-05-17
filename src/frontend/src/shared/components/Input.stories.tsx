import type { Meta, StoryObj } from '@storybook/react';
import { Input } from './Input';

const meta: Meta<typeof Input> = {
  title: 'Shared/Input',
  component: Input,
  parameters: { layout: 'centered' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, minHeight: 200, background: '#0b0e0c', width: 320 }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof Input>;

export const Default: Story = {
  args: { placeholder: 'Type something...' },
};

export const WithValue: Story = {
  args: { defaultValue: 'joao@example.com' },
};

export const Password: Story = {
  args: { type: 'password', placeholder: 'Enter password' },
};

export const Email: Story = {
  args: { type: 'email', placeholder: 'you@example.com' },
};

export const Number: Story = {
  args: { type: 'number', placeholder: '0', min: 0, max: 100 },
};

export const Disabled: Story = {
  args: { placeholder: 'Cannot edit', disabled: true },
};

export const ReadOnly: Story = {
  args: { defaultValue: 'Read-only value', readOnly: true },
};

export const WithCustomClass: Story = {
  args: { placeholder: 'Extra class', className: 'input-wide' },
};
