import React, { useState } from 'react';

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
    <div
      className="vc-chat-drawer"
      style={{
        position: 'fixed',
        top: 0,
        right: 0,
        bottom: 0,
        width: '340px',
        display: 'flex',
        flexDirection: 'column',
        background: '#fff',
        boxShadow: '-2px 0 8px rgba(0,0,0,0.15)',
        zIndex: 1000,
      }}
    >
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '12px 16px',
          borderBottom: '1px solid #e5e5e5',
        }}
      >
        <span style={{ fontWeight: 600, fontSize: '1rem' }}>Chat</span>
        <button onClick={onClose} aria-label="Close chat">
          ✕
        </button>
      </div>

      {/* Messages */}
      <div
        style={{
          flex: 1,
          overflowY: 'auto',
          padding: '12px 16px',
          display: 'flex',
          flexDirection: 'column',
          gap: '8px',
        }}
      >
        {messages.map((msg, idx) => {
          const isSent = msg.from === currentUserName;
          return (
            <div
              key={idx}
              className={isSent ? 'vc-chat-msg-sent' : 'vc-chat-msg-received'}
              style={{
                alignSelf: isSent ? 'flex-end' : 'flex-start',
                maxWidth: '80%',
              }}
            >
              <div style={{ fontSize: '0.75rem', color: '#666', marginBottom: '2px' }}>
                {msg.from}
              </div>
              <div>{msg.text}</div>
            </div>
          );
        })}
      </div>

      {/* Input */}
      <div
        style={{
          display: 'flex',
          padding: '8px 12px',
          borderTop: '1px solid #e5e5e5',
          gap: '8px',
        }}
      >
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a message..."
          style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
        />
        <button onClick={handleSend} aria-label="Send">
          Send
        </button>
      </div>
    </div>
  );
}
