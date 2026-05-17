import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect } from 'vitest';
import { ChatDrawer } from '../components/ChatDrawer';

const messages = [
  { from: 'Dr House', role: 'Doctor', text: 'How are you?', sentAt: '2026-05-18T10:00:00Z' },
  { from: 'Pat Ient', role: 'Patient', text: 'Feeling better', sentAt: '2026-05-18T10:01:00Z' },
];

describe('ChatDrawer', () => {
  it('renders message list', () => {
    render(<ChatDrawer messages={messages} currentUserName="Pat Ient" onSend={vi.fn()} onClose={vi.fn()} />);

    expect(screen.getByText('How are you?')).toBeDefined();
    expect(screen.getByText('Feeling better')).toBeDefined();
  });

  it('aligns own messages differently from received', () => {
    const { container } = render(
      <ChatDrawer messages={messages} currentUserName="Pat Ient" onSend={vi.fn()} onClose={vi.fn()} />,
    );

    const ownMessage = container.querySelector('.vc-chat-msg-sent');
    const otherMessage = container.querySelector('.vc-chat-msg-received');

    expect(ownMessage).toBeTruthy();
    expect(otherMessage).toBeTruthy();
  });

  it('calls onSend when submitting input', async () => {
    const user = userEvent.setup();
    const onSend = vi.fn();

    render(<ChatDrawer messages={[]} currentUserName="Pat Ient" onSend={onSend} onClose={vi.fn()} />);

    const input = screen.getByPlaceholderText(/message/i);
    await user.type(input, 'Hello doctor');
    await user.click(screen.getByRole('button', { name: /send/i }));

    expect(onSend).toHaveBeenCalledWith('Hello doctor');
  });

  it('clears input after send', async () => {
    const user = userEvent.setup();

    render(<ChatDrawer messages={[]} currentUserName="Pat Ient" onSend={vi.fn()} onClose={vi.fn()} />);

    const input = screen.getByPlaceholderText(/message/i) as HTMLInputElement;
    await user.type(input, 'Hello');
    await user.click(screen.getByRole('button', { name: /send/i }));

    expect(input.value).toBe('');
  });
});
