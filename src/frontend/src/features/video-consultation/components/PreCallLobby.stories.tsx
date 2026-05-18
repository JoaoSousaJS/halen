import type { Meta, StoryObj } from '@storybook/react';
import { fn } from 'storybook/test';
import { PreCallLobby } from './PreCallLobby';

const meta: Meta<typeof PreCallLobby> = {
  title: 'VideoConsultation/PreCallLobby',
  component: PreCallLobby,
  parameters: { layout: 'fullscreen' },
  args: {
    doctorName: 'Dr House',
    patientName: 'Pat Ient',
    reason: 'Recurring headache',
    onJoin: fn(),
  },
  decorators: [
    (Story) => (
      <div style={{ background: '#0b0e0c', minHeight: '100vh' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof PreCallLobby>;

export const PatientWaiting: Story = {
  args: {
    role: 'Patient',
    participants: [],
  },
};

export const PatientDoctorReady: Story = {
  args: {
    role: 'Patient',
    participants: [{ name: 'Dr House', role: 'Doctor' }],
  },
};

export const DoctorWaiting: Story = {
  args: {
    role: 'Doctor',
    participants: [],
  },
};

export const DoctorPatientReady: Story = {
  args: {
    role: 'Doctor',
    participants: [{ name: 'Pat Ient', role: 'Patient' }],
  },
};
