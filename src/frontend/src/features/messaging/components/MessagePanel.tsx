import { useState, useRef, useEffect } from 'react';
import type { MessageDto, ThreadStatus } from '../../../shared/api/messaging';

interface MessagePanelProps {
  messages: MessageDto[];
  currentUserId: string;
  onSend: (content: string) => void;
  onTyping: () => void;
  typingUser: string | null;
  threadStatus: ThreadStatus;
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

export function MessagePanel({
  messages,
  currentUserId,
  onSend,
  onTyping,
  typingUser,
  threadStatus,
}: MessagePanelProps) {
  const [input, setInput] = useState('');
  const bottomRef = useRef<HTMLDivElement>(null);
  const isClosed = threadStatus === 'Closed';

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages.length]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = input.trim();
    if (!trimmed || isClosed) return;
    onSend(trimmed);
    setInput('');
  };

  return (
    <div className="msg-panel">
      <div className="msg-messages">
        {messages.map((m) => {
          if (m.messageType === 'SystemEvent') {
            return (
              <div key={m.id} className="msg-system">
                {m.content}
              </div>
            );
          }

          const isSent = m.senderUserId === currentUserId;
          return (
            <div key={m.id} className={`msg-bubble ${isSent ? 'msg-bubble-sent' : 'msg-bubble-received'}`}>
              {!isSent && <span className="msg-sender">{m.senderName}</span>}
              {m.content && <p className="msg-text">{m.content}</p>}
              {m.attachments.map((a) => (
                <div key={a.id} className="msg-attachment">
                  <span className="msg-attachment-icon">
                    {a.attachmentType === 'Image' ? '🖼' : '📎'}
                  </span>
                  <span className="msg-attachment-name">{a.fileName}</span>
                  <span className="msg-attachment-size">{formatFileSize(a.fileSizeBytes)}</span>
                </div>
              ))}
              <span className="msg-time">{formatTime(m.createdAt)}</span>
            </div>
          );
        })}
        {typingUser && (
          <div className="msg-typing">{typingUser} is typing…</div>
        )}
        <div ref={bottomRef} />
      </div>
      <form className="msg-compose" onSubmit={handleSubmit}>
        <input
          className="msg-input"
          type="text"
          placeholder={isClosed ? 'This conversation is closed' : 'Type a message…'}
          value={input}
          onChange={(e) => {
            setInput(e.target.value);
            if (e.target.value) onTyping();
          }}
          disabled={isClosed}
        />
        <button
          className="msg-send-btn"
          type="submit"
          disabled={isClosed || !input.trim()}
          aria-label="Send"
        >
          Send
        </button>
      </form>
    </div>
  );
}
