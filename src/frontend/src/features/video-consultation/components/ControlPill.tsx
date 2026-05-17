import React from 'react';

export function ControlPill({
  role,
  controls,
  onToggleMic,
  onToggleCam,
  onToggleChat,
  onToggleSidebar,
  onEndCall,
}: {
  role: string;
  controls: { mic: boolean; cam: boolean; chatOpen: boolean; sidebarOpen: boolean };
  onToggleMic: () => void;
  onToggleCam: () => void;
  onToggleChat: () => void;
  onToggleSidebar: () => void;
  onEndCall: () => void;
}) {
  const isDoctor = role.toLowerCase() === 'doctor';

  return (
    <div
      className="vc-control-pill"
      style={{
        position: 'fixed',
        bottom: '24px',
        left: '50%',
        transform: 'translateX(-50%)',
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        padding: '8px 16px',
        borderRadius: '9999px',
        background: 'rgba(30, 30, 30, 0.75)',
        backdropFilter: 'blur(12px)',
        WebkitBackdropFilter: 'blur(12px)',
        zIndex: 900,
      }}
    >
      <button
        aria-label="Mic"
        data-active={controls.mic}
        onClick={onToggleMic}
      >
        Mic
      </button>

      <button
        aria-label="Cam"
        data-active={controls.cam}
        onClick={onToggleCam}
      >
        Cam
      </button>

      <button aria-label="Share">
        Share
      </button>

      <button
        aria-label="Chat"
        data-active={controls.chatOpen}
        onClick={onToggleChat}
      >
        Chat
      </button>

      {isDoctor && (
        <>
          <button
            aria-label="Sidebar"
            data-active={controls.sidebarOpen}
            onClick={onToggleSidebar}
          >
            Sidebar
          </button>

          <button
            aria-label="End call"
            onClick={onEndCall}
            style={{ color: '#ff4444' }}
          >
            End
          </button>
        </>
      )}
    </div>
  );
}
