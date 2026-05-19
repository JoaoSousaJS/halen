import type { Meta, StoryObj } from '@storybook/react';
import { ToggleSwitch } from './ToggleSwitch';

const meta: Meta<typeof ToggleSwitch> = {
  title: 'Shared/ToggleSwitch',
  component: ToggleSwitch,
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

type Story = StoryObj<typeof ToggleSwitch>;

export const Default: Story = {
  args: { checked: false, label: 'Off' },
};

export const Checked: Story = {
  args: { checked: true, label: 'Active' },
};

export const Loading: Story = {
  args: { checked: true, loading: true, label: 'Saving...' },
};

export const Disabled: Story = {
  args: { checked: false, disabled: true, label: 'Disabled' },
};

export const DisabledChecked: Story = {
  args: { checked: true, disabled: true, label: 'Locked on' },
};

export const NoLabel: Story = {
  args: { checked: true },
};

export const AllStates: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <ToggleSwitch checked={false} onChange={() => {}} label="Unchecked" />
      <ToggleSwitch checked={true} onChange={() => {}} label="Checked" />
      <ToggleSwitch checked={false} onChange={() => {}} loading label="Loading (off)" />
      <ToggleSwitch checked={true} onChange={() => {}} loading label="Loading (on)" />
      <ToggleSwitch checked={false} onChange={() => {}} disabled label="Disabled (off)" />
      <ToggleSwitch checked={true} onChange={() => {}} disabled label="Disabled (on)" />
    </div>
  ),
};
