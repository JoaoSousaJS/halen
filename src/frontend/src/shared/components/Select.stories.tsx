import type { Meta, StoryObj } from '@storybook/react';
import { Select } from './Select';

const roleOptions = [
  { value: 'patient', label: 'Patient' },
  { value: 'doctor', label: 'Doctor' },
  { value: 'admin', label: 'Admin' },
];

const timeSlots = [
  { value: '09:00', label: '09:00 AM' },
  { value: '10:00', label: '10:00 AM' },
  { value: '11:00', label: '11:00 AM' },
  { value: '14:00', label: '02:00 PM' },
  { value: '15:00', label: '03:00 PM' },
  { value: '16:00', label: '04:00 PM' },
];

const meta: Meta<typeof Select> = {
  title: 'Shared/Select',
  component: Select,
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

type Story = StoryObj<typeof Select>;

export const Default: Story = {
  args: { options: roleOptions },
};

export const WithPlaceholder: Story = {
  args: {
    options: roleOptions,
    placeholder: 'Select a role...',
  },
};

export const TimeSlotPicker: Story = {
  args: {
    options: timeSlots,
    placeholder: 'Choose a time slot',
  },
};

export const Disabled: Story = {
  args: {
    options: roleOptions,
    placeholder: 'Select a role...',
    disabled: true,
  },
};

export const SingleOption: Story = {
  args: {
    options: [{ value: 'only', label: 'Only option' }],
  },
};

export const ManyOptions: Story = {
  args: {
    options: Array.from({ length: 20 }, (_, i) => ({
      value: `opt-${i + 1}`,
      label: `Option ${i + 1}`,
    })),
    placeholder: 'Pick one...',
  },
};
