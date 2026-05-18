import type { Meta, StoryObj } from '@storybook/react';
import { ThreadList } from './ThreadList';
import type { ThreadSummaryDto } from '../../../shared/api/messaging';

const threads: ThreadSummaryDto[] = [
  {
    threadId: 't-1',
    otherParticipantName: 'Dr. Amelia Chen',
    otherParticipantSpecialty: 'Cardiology',
    subject: 'Persistent chest tightness',
    lastMessagePreview: 'Mostly pressure. Like a hand on my chest.',
    lastMessageAt: new Date().toISOString(),
    unreadCount: 1,
    status: 'Active',
    appointmentStatus: 'Scheduled',
    appointmentId: 'a-1',
  },
  {
    threadId: 't-2',
    otherParticipantName: 'Dr. Marcus Kim',
    otherParticipantSpecialty: 'Dermatology',
    subject: 'Migraine follow-up',
    lastMessagePreview: "I'll review the photos and get back to you.",
    lastMessageAt: new Date(Date.now() - 86400000).toISOString(),
    unreadCount: 2,
    status: 'Active',
    appointmentStatus: 'Scheduled',
    appointmentId: 'a-2',
  },
  {
    threadId: 't-3',
    otherParticipantName: 'Dr. Sofia Reyes',
    otherParticipantSpecialty: 'Pediatrics',
    subject: 'Ear infection follow-up',
    lastMessagePreview: "Antibiotic course complete — no return.",
    lastMessageAt: new Date(Date.now() - 604800000).toISOString(),
    unreadCount: 0,
    status: 'Closed',
    appointmentStatus: 'Completed',
    appointmentId: 'a-3',
  },
];

const meta: Meta<typeof ThreadList> = {
  title: 'Messaging/ThreadList',
  component: ThreadList,
  decorators: [
    (Story) => (
      <div style={{ width: 340, background: 'var(--bg, #0b0e0c)', minHeight: 400 }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof ThreadList>;

export const Default: Story = {
  args: { threads, selectedId: null, onSelect: () => {} },
};

export const WithSelection: Story = {
  args: { threads, selectedId: 't-1', onSelect: () => {} },
};

export const Empty: Story = {
  args: { threads: [], selectedId: null, onSelect: () => {} },
};
