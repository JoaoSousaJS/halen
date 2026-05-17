import type { Meta, StoryObj } from '@storybook/react';
import { fn } from '@storybook/test';
import { ClinicalSidebar } from './ClinicalSidebar';

const meta: Meta<typeof ClinicalSidebar> = {
  title: 'VideoConsultation/ClinicalSidebar',
  component: ClinicalSidebar,
  parameters: { layout: 'fullscreen' },
  args: {
    onUpdateNotes: fn(),
    onClose: fn(),
  },
};
export default meta;

type Story = StoryObj<typeof ClinicalSidebar>;

export const Default: Story = {
  args: {
    patientName: 'Pat Ient',
    notes: '',
  },
};

export const WithNotes: Story = {
  args: {
    patientName: 'Pat Ient',
    notes: 'Patient presents with recurring headache, 3 days duration.\nLight sensitivity reported.\nNo history of migraines.\n\nPlan: Order CT scan, prescribe ibuprofen 400mg.',
  },
};

export const LongName: Story = {
  args: {
    patientName: 'Maria Fernanda da Silva Santos',
    notes: '',
  },
};
