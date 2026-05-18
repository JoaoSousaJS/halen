import type { Meta, StoryObj } from '@storybook/react';
import { fn } from 'storybook/test';
import { MemoryRouter } from 'react-router-dom';
import { PostCallWrapUp } from './PostCallWrapUp';

const meta: Meta<typeof PostCallWrapUp> = {
  title: 'VideoConsultation/PostCallWrapUp',
  component: PostCallWrapUp,
  parameters: { layout: 'fullscreen' },
  args: {
    onSave: fn(),
  },
  decorators: [
    (Story) => (
      <MemoryRouter>
        <div style={{ background: '#0b0e0c', minHeight: '100vh' }}>
          <Story />
        </div>
      </MemoryRouter>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof PostCallWrapUp>;

export const PatientSummary: Story = {
  args: {
    role: 'Patient',
    doctorName: 'Dr House',
    patientName: 'Pat Ient',
    notes: '',
    elapsedSeconds: 1260,
  },
};

export const PatientShortCall: Story = {
  args: {
    role: 'Patient',
    doctorName: 'Dr House',
    patientName: 'Pat Ient',
    notes: '',
    elapsedSeconds: 180,
  },
};

export const DoctorFinalize: Story = {
  args: {
    role: 'Doctor',
    doctorName: 'Dr House',
    patientName: 'Pat Ient',
    notes: 'Patient presents with recurring headache.\nPrescribed ibuprofen 400mg.',
    elapsedSeconds: 900,
  },
};

export const DoctorFinalizeEmpty: Story = {
  args: {
    role: 'Doctor',
    doctorName: 'Dr House',
    patientName: 'Pat Ient',
    notes: '',
    elapsedSeconds: 600,
  },
};
