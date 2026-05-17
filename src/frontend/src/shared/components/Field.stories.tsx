import type { Meta, StoryObj } from '@storybook/react';
import { Field } from './Field';
import { Input } from './Input';

const meta: Meta<typeof Field> = {
  title: 'Shared/Field',
  component: Field,
  parameters: { layout: 'centered' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, minHeight: 200, background: '#0b0e0c', width: 360 }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof Field>;

export const Default: Story = {
  args: {
    label: 'Email address',
    children: <Input type="email" placeholder="you@example.com" />,
  },
};

export const WithHint: Story = {
  args: {
    label: 'Password',
    hint: 'Must be at least 8 characters',
    children: <Input type="password" />,
  },
};

export const Inline: Story = {
  args: {
    label: 'Remember me',
    inline: true,
    children: <input type="checkbox" />,
  },
};

export const Row: Story = {
  args: {
    label: 'Actions',
    row: true,
    children: (
      <>
        <button className="btn btn-primary">Save</button>
        <button className="btn btn-ghost">Cancel</button>
      </>
    ),
  },
};

export const MultipleFields: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Field label="First name">
        <Input placeholder="John" />
      </Field>
      <Field label="Last name">
        <Input placeholder="Doe" />
      </Field>
      <Field label="Email" hint="We will never share your email">
        <Input type="email" placeholder="john@example.com" />
      </Field>
      <Field label="Receive notifications" inline>
        <input type="checkbox" />
      </Field>
    </div>
  ),
};

export const LongHint: Story = {
  args: {
    label: 'Phone number',
    hint: 'Include country code. This number will be used for appointment reminders and two-factor authentication.',
    children: <Input type="tel" placeholder="+55 11 99999-0000" />,
  },
};
