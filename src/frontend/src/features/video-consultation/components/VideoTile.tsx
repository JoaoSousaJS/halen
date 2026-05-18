import { MicOffGlyph } from '../glyphs';
import { getInitials } from '../utils';
import '../video-consultation.css';

function hashHue(name: string): number {
  let h = 0;
  for (let i = 0; i < name.length; i++) {
    h = (h * 31 + name.charCodeAt(i)) >>> 0;
  }
  return h % 360;
}

export function VideoTile({
  name,
  size,
  isMuted,
  isSpeaking,
}: {
  name: string;
  size: 'lg' | 'sm' | 'pip';
  isMuted?: boolean;
  isSpeaking?: boolean;
}) {
  const hue = hashHue(name);
  const initials = getInitials(name);

  return (
    <div
      className={`vc-tile vc-tile-${size}${isSpeaking ? ' vc-tile--speaking' : ''}`}
      style={{
        background: `linear-gradient(180deg, oklch(0.28 0.07 ${hue}), oklch(0.18 0.05 ${hue}))`,
      }}
    >
      <span className="vc-tile__initials" style={{ mixBlendMode: 'screen', opacity: 0.92 }}>
        {initials}
      </span>

      {isMuted && (
        <span aria-label="Muted" className="vc-tile__muted">
          <MicOffGlyph size={14} />
        </span>
      )}

      <span className="vc-tile__label">{name}</span>
    </div>
  );
}
