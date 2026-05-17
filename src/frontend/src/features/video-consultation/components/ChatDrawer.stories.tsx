import type { Meta, StoryObj } from '@storybook/react';
import { fn } from '@storybook/test';
import { ChatDrawer } from './ChatDrawer';

const meta: Meta<typeof ChatDrawer> = {
  title: 'VideoConsultation/ChatDrawer',
  component: ChatDrawer,
  parameters: { layout: 'fullscreen' },
  args: {
    currentUserName: 'Dr House',
    onSend: fn(),
    onClose: fn(),
  },
};
export default meta;

type Story = StoryObj<typeof ChatDrawer>;

export const Empty: Story = {
  args: { messages: [] },
};

export const WithMessages: Story = {
  args: {
    messages: [
      { from: 'Pat Ient', role: 'Patient', text: 'Hi Doctor, I have a headache.', sentAt: '2026-05-18T10:00:00Z' },
      { from: 'Dr House', role: 'Doctor', text: 'How long has this been going on?', sentAt: '2026-05-18T10:00:15Z' },
      { from: 'Pat Ient', role: 'Patient', text: 'About 3 days now. It gets worse in the afternoon.', sentAt: '2026-05-18T10:00:30Z' },
      { from: 'Dr House', role: 'Doctor', text: 'Any other symptoms? Nausea, sensitivity to light?', sentAt: '2026-05-18T10:00:45Z' },
      { from: 'Pat Ient', role: 'Patient', text: 'Some light sensitivity, yes.', sentAt: '2026-05-18T10:01:00Z' },
    ],
  },
};

export const PatientView: Story = {
  args: {
    currentUserName: 'Pat Ient',
    messages: [
      { from: 'Dr House', role: 'Doctor', text: 'Good morning! How are you feeling today?', sentAt: '2026-05-18T10:00:00Z' },
      { from: 'Pat Ient', role: 'Patient', text: 'Not great, doctor.', sentAt: '2026-05-18T10:00:10Z' },
    ],
  },
};
