import { MicOffGlyph } from '../glyphs';
import { getInitials } from '../utils';
import '../video-consultation.css';

function hashName(name: string): number {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return Math.abs(hash);
}

export function VideoTile({
  name,
  size,
  isMuted,
}: {
  name: string;
  size: 'lg' | 'sm' | 'pip';
  isMuted?: boolean;
}) {
  const hue = hashName(name) % 360;
  const initials = getInitials(name);

  return (
    <div
      className={`vc-tile vc-tile-${size}`}
      style={{
        background: `linear-gradient(135deg, oklch(0.28 0.07 ${hue}), oklch(0.18 0.05 ${hue + 30}))`,
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
