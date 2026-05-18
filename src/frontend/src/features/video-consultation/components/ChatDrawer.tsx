import { useState } from 'react';
import '../video-consultation.css';

export interface ChatMessage {
  from: string;
  role: string;
  text: string;
  sentAt: string;
}

export function ChatDrawer({
  messages,
  currentUserName,
  onSend,
  onClose,
}: {
  messages: ChatMessage[];
  currentUserName: string;
  onSend: (text: string) => void;
  onClose: () => void;
}) {
  const [input, setInput] = useState('');

  const handleSend = () => {
    const trimmed = input.trim();
    if (!trimmed) return;
    onSend(trimmed);
    setInput('');
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSend();
    }
  };

  return (
    <div className="vc-chat-drawer">
      <div className="vc-chat-drawer__header">
        <div>
          <div className="vc-chat-drawer__header-title">Chat</div>
          <div className="vc-chat-drawer__header-sub">End-to-end encrypted</div>
        </div>
        <button onClick={onClose} aria-label="Close chat">
          ×
        </button>
      </div>

      <div className="vc-chat-drawer__messages">
        {messages.map((msg, idx) => {
          const isSent = msg.from === currentUserName;
          const showSender =
            !isSent && (idx === 0 || messages[idx - 1].from !== msg.from);
          return (
            <div key={idx}>
              {showSender && (
                <div className="vc-chat-msg__from">{msg.from}</div>
              )}
              <div className={isSent ? 'vc-chat-msg-sent' : 'vc-chat-msg-received'}>
                {msg.text}
              </div>
            </div>
          );
        })}
      </div>

      <div className="vc-chat-drawer__input">
        <div className="vc-chat-drawer__input-row">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Type a message…"
          />
          <button onClick={handleSend} aria-label="Send">
            Send
          </button>
        </div>
      </div>
    </div>
  );
}
