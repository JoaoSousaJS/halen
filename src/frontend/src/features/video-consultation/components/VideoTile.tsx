import React from 'react';

function hashName(name: string): number {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return Math.abs(hash);
}

function getInitials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .map((word) => word[0].toUpperCase())
    .join('');
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
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        position: 'relative',
        background: `linear-gradient(135deg, oklch(0.55 0.15 ${hue}), oklch(0.40 0.12 ${hue + 30}))`,
        borderRadius: '8px',
        color: '#fff',
        fontSize: size === 'lg' ? '3rem' : size === 'sm' ? '1.5rem' : '1rem',
        fontWeight: 600,
        overflow: 'hidden',
      }}
    >
      {initials}
      {isMuted && (
        <span
          aria-label="Muted"
          style={{
            position: 'absolute',
            bottom: '8px',
            right: '8px',
            fontSize: '0.75rem',
            background: 'rgba(0,0,0,0.6)',
            borderRadius: '50%',
            width: '24px',
            height: '24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          🔇
        </span>
      )}
    </div>
  );
}
