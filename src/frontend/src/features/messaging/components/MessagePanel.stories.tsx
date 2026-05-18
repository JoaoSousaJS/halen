import type { Meta, StoryObj } from '@storybook/react';
import { MessagePanel } from './MessagePanel';
import type { MessageDto } from '../../../shared/api/messaging';

const messages: MessageDto[] = [
  {
    id: 'm-0',
    senderName: 'System',
    senderRole: 'Patient',
    senderUserId: 'system',
    content: 'Thread created for appointment — May 19, 9:00 AM',
    messageType: 'SystemEvent',
    isRead: true,
    readAt: null,
    createdAt: '2026-05-19T08:55:00Z',
    attachments: [],
  },
  {
    id: 'm-1',
    senderName: 'Dr. Amelia Chen',
    senderRole: 'Doctor',
    senderUserId: 'doc-1',
    content: 'Hi Maya — can you describe the chest tightness in more detail? Pressure, sharp, or burning?',
    messageType: 'Text',
    isRead: true,
    readAt: '2026-05-19T09:03:00Z',
    createdAt: '2026-05-19T09:02:00Z',
    attachments: [],
  },
  {
    id: 'm-2',
    senderName: 'Maya Chen',
    senderRole: 'Patient',
    senderUserId: 'pat-1',
    content: 'Mostly pressure. Like a hand on my chest, mainly when I run longer than 5km.',
    messageType: 'Text',
    isRead: true,
    readAt: '2026-05-19T09:05:00Z',
    createdAt: '2026-05-19T09:04:00Z',
    attachments: [],
  },
  {
    id: 'm-3',
    senderName: 'Dr. Amelia Chen',
    senderRole: 'Doctor',
    senderUserId: 'doc-1',
    content: 'Does the pain radiate anywhere — jaw, left arm, back?',
    messageType: 'Text',
    isRead: true,
    readAt: null,
    createdAt: '2026-05-19T09:05:30Z',
    attachments: [],
  },
  {
    id: 'm-4',
    senderName: 'Maya Chen',
    senderRole: 'Patient',
    senderUserId: 'pat-1',
    content: '',
    messageType: 'Attachment',
    isRead: false,
    readAt: null,
    createdAt: '2026-05-19T09:09:00Z',
    attachments: [
      {
        id: 'att-1',
        fileName: 'running-watch-data.png',
        contentType: 'image/png',
        fileSizeBytes: 1468006,
        attachmentType: 'Image',
      },
    ],
  },
];

const meta: Meta<typeof MessagePanel> = {
  title: 'Messaging/MessagePanel',
  component: MessagePanel,
  decorators: [
    (Story) => (
      <div style={{ height: 500, background: 'var(--bg, #0b0e0c)', display: 'flex', flexDirection: 'column' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof MessagePanel>;

export const ActiveThread: Story = {
  args: {
    messages,
    currentUserId: 'pat-1',
    onSend: () => {},
    onTyping: () => {},
    typingUser: null,
    threadStatus: 'Active',
  },
};

export const WithTypingIndicator: Story = {
  args: {
    messages,
    currentUserId: 'pat-1',
    onSend: () => {},
    onTyping: () => {},
    typingUser: 'Dr. Amelia Chen',
    threadStatus: 'Active',
  },
};

export const ClosedThread: Story = {
  args: {
    messages,
    currentUserId: 'pat-1',
    onSend: () => {},
    onTyping: () => {},
    typingUser: null,
    threadStatus: 'Closed',
  },
};

export const Empty: Story = {
  args: {
    messages: [],
    currentUserId: 'pat-1',
    onSend: () => {},
    onTyping: () => {},
    typingUser: null,
    threadStatus: 'Active',
  },
};
