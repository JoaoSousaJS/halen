import type { Meta, StoryObj } from '@storybook/react';
import { fn } from 'storybook/test';
import { ControlPill } from './ControlPill';

const meta: Meta<typeof ControlPill> = {
  title: 'VideoConsultation/ControlPill',
  component: ControlPill,
  parameters: { layout: 'fullscreen' },
  args: {
    elapsedSeconds: 754,
    onToggleMic: fn(),
    onToggleCam: fn(),
    onToggleChat: fn(),
    onToggleSidebar: fn(),
    onEndCall: fn(),
  },
  decorators: [
    (Story) => (
      <div style={{ background: '#0b0e0c', minHeight: 200, position: 'relative' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof ControlPill>;

export const DoctorAllOn: Story = {
  args: {
    role: 'Doctor',
    controls: { mic: true, cam: true, chatOpen: false, sidebarOpen: false },
  },
};

export const DoctorMicOff: Story = {
  args: {
    role: 'Doctor',
    controls: { mic: false, cam: true, chatOpen: false, sidebarOpen: false },
  },
};

export const DoctorChatAndSidebar: Story = {
  args: {
    role: 'Doctor',
    controls: { mic: true, cam: true, chatOpen: true, sidebarOpen: true },
  },
};

export const PatientView: Story = {
  args: {
    role: 'Patient',
    controls: { mic: true, cam: true, chatOpen: false, sidebarOpen: false },
  },
};

export const PatientMuted: Story = {
  args: {
    role: 'Patient',
    controls: { mic: false, cam: false, chatOpen: false, sidebarOpen: false },
  },
};
