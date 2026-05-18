import { MicGlyph, CamGlyph, ShareGlyph, ChatGlyph, PanelGlyph, EndCallGlyph } from '../glyphs';
import { formatElapsed } from '../utils';
import '../video-consultation.css';

export function ControlPill({
  role,
  controls,
  elapsedSeconds,
  onToggleMic,
  onToggleCam,
  onToggleChat,
  onToggleSidebar,
  onEndCall,
}: {
  role: string;
  controls: { mic: boolean; cam: boolean; chatOpen: boolean; sidebarOpen: boolean };
  elapsedSeconds: number;
  onToggleMic: () => void;
  onToggleCam: () => void;
  onToggleChat: () => void;
  onToggleSidebar: () => void;
  onEndCall: () => void;
}) {
  const isDoctor = role.toLowerCase() === 'doctor';

  return (
    <div className="vc-control-pill">
      <div className="vc-control-pill__timer">
        <span className="vc-control-pill__timer-dot" />
        <span>{formatElapsed(elapsedSeconds)}</span>
      </div>

      <button
        className="vc-control-pill__btn"
        aria-label="Mic"
        data-active={controls.mic}
        onClick={onToggleMic}
      >
        <MicGlyph size={18} off={!controls.mic} />
        <span className="vc-control-pill__btn-label">Mute</span>
      </button>

      <button
        className="vc-control-pill__btn"
        aria-label="Cam"
        data-active={controls.cam}
        onClick={onToggleCam}
      >
        <CamGlyph size={18} />
        <span className="vc-control-pill__btn-label">Camera</span>
      </button>

      <button className="vc-control-pill__btn" aria-label="Share">
        <ShareGlyph size={18} />
        <span className="vc-control-pill__btn-label">Share</span>
      </button>

      <button
        className="vc-control-pill__btn"
        aria-label="Chat"
        data-active={controls.chatOpen}
        onClick={onToggleChat}
      >
        <ChatGlyph size={18} />
        <span className="vc-control-pill__btn-label">Chat</span>
      </button>

      {isDoctor && (
        <>
          <button
            className={`vc-control-pill__btn${!controls.sidebarOpen ? ' vc-control-pill__btn--dim' : ''}`}
            aria-label="Sidebar"
            data-active={controls.sidebarOpen}
            onClick={onToggleSidebar}
          >
            <PanelGlyph size={18} />
            <span className="vc-control-pill__btn-label">Panel</span>
          </button>

          <button
            className="vc-control-pill__btn vc-control-pill__btn--end"
            aria-label="End"
            onClick={onEndCall}
          >
            <EndCallGlyph size={18} />
            <span className="vc-control-pill__btn-label">End</span>
          </button>
        </>
      )}
    </div>
  );
}
