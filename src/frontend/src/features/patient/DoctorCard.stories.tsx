import type { Meta, StoryObj } from '@storybook/react';
import DoctorCard from './DoctorCard';

const meta: Meta<typeof DoctorCard> = {
  title: 'Patient/DoctorCard',
  component: DoctorCard,
  parameters: { layout: 'centered' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, maxWidth: 400, background: '#0b0e0c' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof DoctorCard>;

const baseDoctor = {
  id: 'doc-1',
  name: 'Dr. Silva',
  specialty: 'Cardiology',
  consultationFee: 150,
  yearsOfExperience: 10,
  languages: ['English', 'Portuguese'],
  nextAvailableSlot: { startUtc: '2026-05-19T09:00:00Z', dayOfWeek: 'Monday' },
};

export const Default: Story = {
  args: {
    doctor: baseDoctor,
    onSelect: () => {},
  },
};

export const NoAvailability: Story = {
  args: {
    doctor: { ...baseDoctor, nextAvailableSlot: null },
    onSelect: () => {},
  },
};

export const HighFee: Story = {
  args: {
    doctor: {
      ...baseDoctor,
      name: 'Dr. Andrade',
      specialty: 'Neurosurgery',
      consultationFee: 500,
      yearsOfExperience: 25,
      languages: ['English', 'Spanish', 'French'],
    },
    onSelect: () => {},
  },
};

export const LowFee: Story = {
  args: {
    doctor: {
      ...baseDoctor,
      name: 'Dr. Souza',
      specialty: 'General Practice',
      consultationFee: 50,
      yearsOfExperience: 2,
      languages: ['Portuguese'],
    },
    onSelect: () => {},
  },
};

export const MultipleCards: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
      <DoctorCard doctor={baseDoctor} onSelect={() => {}} />
      <DoctorCard
        doctor={{
          ...baseDoctor,
          id: 'doc-2',
          name: 'Dr. Costa',
          specialty: 'Dermatology',
          consultationFee: 120,
          yearsOfExperience: 7,
          languages: ['English'],
          nextAvailableSlot: null,
        }}
        onSelect={() => {}}
      />
      <DoctorCard
        doctor={{
          ...baseDoctor,
          id: 'doc-3',
          name: 'Dr. Mendes',
          specialty: 'Psychiatry',
          consultationFee: 200,
          yearsOfExperience: 15,
          languages: ['English', 'Portuguese', 'Spanish'],
          nextAvailableSlot: { startUtc: '2026-05-20T14:00:00Z', dayOfWeek: 'Tuesday' },
        }}
        onSelect={() => {}}
      />
    </div>
  ),
};
