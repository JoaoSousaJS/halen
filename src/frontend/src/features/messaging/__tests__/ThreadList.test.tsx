import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect } from 'vitest';
import { ThreadList } from '../components/ThreadList';
import type { ThreadSummaryDto } from '../../../shared/api/messaging';

const threads: ThreadSummaryDto[] = [
  {
    threadId: 't-1',
    otherParticipantName: 'Dr House',
    otherParticipantSpecialty: 'Diagnostics',
    subject: 'Headache consult',
    lastMessagePreview: 'Take aspirin daily',
    lastMessageAt: '2026-05-18T10:00:00Z',
    unreadCount: 2,
    status: 'Active',
    appointmentStatus: 'Scheduled',
    appointmentId: 'a-1',
  },
  {
    threadId: 't-2',
    otherParticipantName: 'Dr Wilson',
    otherParticipantSpecialty: 'Oncology',
    subject: 'Follow-up',
    lastMessagePreview: null,
    lastMessageAt: null,
    unreadCount: 0,
    status: 'Closed',
    appointmentStatus: 'Completed',
    appointmentId: 'a-2',
  },
];

describe('ThreadList', () => {
  it('renders thread rows with participant names', () => {
    render(<ThreadList threads={threads} selectedId={null} onSelect={vi.fn()} />);

    expect(screen.getByText('Dr House')).toBeDefined();
    expect(screen.getByText('Dr Wilson')).toBeDefined();
  });

  it('shows last message preview', () => {
    render(<ThreadList threads={threads} selectedId={null} onSelect={vi.fn()} />);

    expect(screen.getByText('Take aspirin daily')).toBeDefined();
  });

  it('shows unread badge when count > 0', () => {
    render(<ThreadList threads={threads} selectedId={null} onSelect={vi.fn()} />);

    expect(screen.getByText('2')).toBeDefined();
  });

  it('calls onSelect with thread id when clicked', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(<ThreadList threads={threads} selectedId={null} onSelect={onSelect} />);

    await user.click(screen.getByText('Dr House'));

    expect(onSelect).toHaveBeenCalledWith('t-1');
  });

  it('highlights selected thread', () => {
    const { container } = render(
      <ThreadList threads={threads} selectedId="t-1" onSelect={vi.fn()} />,
    );

    const selected = container.querySelector('.msg-thread-selected');
    expect(selected).toBeTruthy();
  });

  it('shows empty state when no threads', () => {
    render(<ThreadList threads={[]} selectedId={null} onSelect={vi.fn()} />);

    expect(screen.getByText(/no conversations/i)).toBeDefined();
  });
});
