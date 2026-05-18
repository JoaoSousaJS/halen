import type { ThreadSummaryDto } from '../../../shared/api/messaging';

interface ThreadListProps {
  threads: ThreadSummaryDto[];
  selectedId: string | null;
  onSelect: (threadId: string) => void;
}

function formatTime(iso: string | null): string {
  if (!iso) return '';
  const d = new Date(iso);
  const now = new Date();
  if (d.toDateString() === now.toDateString()) {
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
  return d.toLocaleDateString([], { month: 'short', day: 'numeric' });
}

export function ThreadList({ threads, selectedId, onSelect }: ThreadListProps) {
  if (threads.length === 0) {
    return <div className="msg-empty">No conversations yet</div>;
  }

  return (
    <div className="msg-thread-list" role="list">
      {threads.map((t) => (
        <button
          key={t.threadId}
          className={`msg-thread-row ${t.threadId === selectedId ? 'msg-thread-selected' : ''} ${t.unreadCount > 0 ? 'msg-thread-unread' : ''}`}
          onClick={() => onSelect(t.threadId)}
          role="listitem"
          type="button"
        >
          <div className="msg-thread-avatar">
            {t.otherParticipantName.charAt(0).toUpperCase()}
          </div>
          <div className="msg-thread-info">
            <div className="msg-thread-header">
              <span className="msg-thread-name">{t.otherParticipantName}</span>
              <span className="msg-thread-time">{formatTime(t.lastMessageAt)}</span>
            </div>
            {t.otherParticipantSpecialty && (
              <span className="msg-thread-specialty">{t.otherParticipantSpecialty}</span>
            )}
            <div className="msg-thread-preview">
              {t.lastMessagePreview || t.subject}
            </div>
          </div>
          {t.unreadCount > 0 && (
            <span className="msg-thread-badge">{t.unreadCount}</span>
          )}
        </button>
      ))}
    </div>
  );
}
