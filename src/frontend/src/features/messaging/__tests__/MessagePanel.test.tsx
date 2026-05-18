import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeAll } from 'vitest';
import { MessagePanel } from '../components/MessagePanel';
import type { MessageDto } from '../../../shared/api/messaging';

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

const currentUserId = 'user-1';

const messages: MessageDto[] = [
  {
    id: 'm-1',
    senderName: 'Dr House',
    senderRole: 'Doctor',
    senderUserId: 'user-2',
    content: 'How are you feeling?',
    messageType: 'Text',
    isRead: true,
    readAt: '2026-05-18T10:01:00Z',
    createdAt: '2026-05-18T10:00:00Z',
    attachments: [],
  },
  {
    id: 'm-2',
    senderName: 'John Doe',
    senderRole: 'Patient',
    senderUserId: 'user-1',
    content: 'Much better, thanks!',
    messageType: 'Text',
    isRead: false,
    readAt: null,
    createdAt: '2026-05-18T10:02:00Z',
    attachments: [],
  },
];

const systemMessage: MessageDto = {
  id: 'm-3',
  senderName: 'System',
  senderRole: 'Patient',
  senderUserId: 'system',
  content: 'Thread created for appointment',
  messageType: 'SystemEvent',
  isRead: true,
  readAt: null,
  createdAt: '2026-05-18T09:59:00Z',
  attachments: [],
};

describe('MessagePanel', () => {
  it('renders message content', () => {
    render(
      <MessagePanel
        messages={messages}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={vi.fn()}
        typingUser={null}
        threadStatus="Active"
      />,
    );

    expect(screen.getByText('How are you feeling?')).toBeDefined();
    expect(screen.getByText('Much better, thanks!')).toBeDefined();
  });

  it('aligns own messages as sent', () => {
    const { container } = render(
      <MessagePanel
        messages={messages}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={vi.fn()}
        typingUser={null}
        threadStatus="Active"
      />,
    );

    expect(container.querySelector('.msg-bubble-sent')).toBeTruthy();
    expect(container.querySelector('.msg-bubble-received')).toBeTruthy();
  });

  it('calls onSend when submitting a message', async () => {
    const user = userEvent.setup();
    const onSend = vi.fn();

    render(
      <MessagePanel
        messages={[]}
        currentUserId={currentUserId}
        onSend={onSend}
        onTyping={vi.fn()}
        typingUser={null}
        threadStatus="Active"
      />,
    );

    const input = screen.getByPlaceholderText(/type a message/i);
    await user.type(input, 'Hello doctor');
    await user.click(screen.getByRole('button', { name: /send/i }));

    expect(onSend).toHaveBeenCalledWith('Hello doctor');
  });

  it('clears input after successful send', async () => {
    const user = userEvent.setup();

    render(
      <MessagePanel
        messages={[]}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={vi.fn()}
        typingUser={null}
        threadStatus="Active"
      />,
    );

    const input = screen.getByPlaceholderText(/type a message/i) as HTMLInputElement;
    await user.type(input, 'Hello');
    await user.click(screen.getByRole('button', { name: /send/i }));

    expect(input.value).toBe('');
  });

  it('shows typing indicator when someone is typing', () => {
    render(
      <MessagePanel
        messages={[]}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={vi.fn()}
        typingUser="Dr House"
        threadStatus="Active"
      />,
    );

    expect(screen.getByText(/Dr House is typing/i)).toBeDefined();
  });

  it('disables input when thread is closed', () => {
    render(
      <MessagePanel
        messages={[]}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={vi.fn()}
        typingUser={null}
        threadStatus="Closed"
      />,
    );

    const input = screen.getByPlaceholderText(/closed/i) as HTMLInputElement;
    expect(input.disabled).toBe(true);
  });

  it('renders system messages distinctly', () => {
    const { container } = render(
      <MessagePanel
        messages={[systemMessage]}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={vi.fn()}
        typingUser={null}
        threadStatus="Active"
      />,
    );

    expect(container.querySelector('.msg-system')).toBeTruthy();
  });

  it('calls onTyping when user types', async () => {
    const user = userEvent.setup();
    const onTyping = vi.fn();

    render(
      <MessagePanel
        messages={[]}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={onTyping}
        typingUser={null}
        threadStatus="Active"
      />,
    );

    const input = screen.getByPlaceholderText(/type a message/i);
    await user.type(input, 'H');

    expect(onTyping).toHaveBeenCalled();
  });

  it('shows attachment info in message', () => {
    const msgWithAttachment: MessageDto = {
      id: 'm-4',
      senderName: 'Dr House',
      senderRole: 'Doctor',
      senderUserId: 'user-2',
      content: '',
      messageType: 'Attachment',
      isRead: false,
      readAt: null,
      createdAt: '2026-05-18T10:05:00Z',
      attachments: [
        {
          id: 'att-1',
          fileName: 'xray.png',
          contentType: 'image/png',
          fileSizeBytes: 204800,
          attachmentType: 'Image',
        },
      ],
    };

    render(
      <MessagePanel
        messages={[msgWithAttachment]}
        currentUserId={currentUserId}
        onSend={vi.fn()}
        onTyping={vi.fn()}
        typingUser={null}
        threadStatus="Active"
      />,
    );

    expect(screen.getByText('xray.png')).toBeDefined();
  });
});
