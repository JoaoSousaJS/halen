import React, { useState } from 'react';
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
      {/* Header */}
      <div className="vc-chat-drawer__header">
        <span>Chat</span>
        <button onClick={onClose} aria-label="Close chat">
          ✕
        </button>
      </div>

      {/* Messages */}
      <div className="vc-chat-drawer__messages">
        {messages.map((msg, idx) => {
          const isSent = msg.from === currentUserName;
          return (
            <div
              key={idx}
              className={isSent ? 'vc-chat-msg-sent' : 'vc-chat-msg-received'}
            >
              <div className="vc-chat-msg__from">{msg.from}</div>
              <div>{msg.text}</div>
            </div>
          );
        })}
      </div>

      {/* Input */}
      <div className="vc-chat-drawer__input">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a message..."
        />
        <button onClick={handleSend} aria-label="Send">
          Send
        </button>
      </div>
    </div>
  );
}
