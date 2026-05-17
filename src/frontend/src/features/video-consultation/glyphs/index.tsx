import React from "react";

interface GlyphProps {
  className?: string;
  size?: number;
}

interface MicGlyphProps extends GlyphProps {
  off?: boolean;
}

const svgBase = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.5,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
  "aria-hidden": true as const,
};

export function MicGlyph({ className, size = 20, off }: MicGlyphProps) {
  return (
    <svg
      {...svgBase}
      viewBox="0 0 24 24"
      width={size}
      height={size}
      className={className}
    >
      <rect x={9} y={3} width={6} height={12} rx={3} />
      <path d="M5 11a7 7 0 0 0 14 0" />
      <path d="M12 18v3" />
      {off && <line x1={3} y1={3} x2={21} y2={21} />}
    </svg>
  );
}

export function MicOffGlyph({ className, size = 20 }: GlyphProps) {
  return <MicGlyph className={className} size={size} off />;
}

export function CamGlyph({ className, size = 20 }: GlyphProps) {
  return (
    <svg
      {...svgBase}
      viewBox="0 0 24 24"
      width={size}
      height={size}
      className={className}
    >
      <rect x={3} y={6} width={14} height={12} rx={2} />
      <path d="M17 10l4-2v8l-4-2z" />
    </svg>
  );
}

export function ShareGlyph({ className, size = 20 }: GlyphProps) {
  return (
    <svg
      {...svgBase}
      viewBox="0 0 24 24"
      width={size}
      height={size}
      className={className}
    >
      <rect x={3} y={4} width={18} height={13} rx={2} />
      <path d="M9 21h6" />
      <path d="M12 17v4" />
      <path d="M9 11l3-3 3 3" />
      <path d="M12 8v5" />
    </svg>
  );
}

export function ChatGlyph({ className, size = 20 }: GlyphProps) {
  return (
    <svg
      {...svgBase}
      viewBox="0 0 24 24"
      width={size}
      height={size}
      className={className}
    >
      <path d="M21 12a8 8 0 1 1-3.6-6.7L21 4l-1 4.2A8 8 0 0 1 21 12z" />
    </svg>
  );
}

export function EndCallGlyph({ className, size = 20 }: GlyphProps) {
  return (
    <svg
      {...svgBase}
      strokeWidth={1.8}
      viewBox="0 0 24 24"
      width={size}
      height={size}
      className={className}
    >
      <path
        d="M3 12c4-4 14-4 18 0l-2 3-3-1v-2a8 8 0 0 0-8 0v2l-3 1z"
        transform="rotate(135 12 12)"
      />
    </svg>
  );
}

export function PanelGlyph({ className, size = 20 }: GlyphProps) {
  return (
    <svg
      {...svgBase}
      viewBox="0 0 24 24"
      width={size}
      height={size}
      className={className}
    >
      <rect x={3} y={4} width={18} height={16} rx={2} />
      <line x1={14} y1={4} x2={14} y2={20} />
    </svg>
  );
}

export function SignalGlyph({ className, size = 20 }: GlyphProps) {
  return (
    <svg
      {...svgBase}
      viewBox="0 0 16 12"
      width={size}
      height={size}
      className={className}
    >
      <rect x={1} y={9} width={3} height={3} rx={0.5} fill="currentColor" />
      <rect x={6} y={5} width={3} height={7} rx={0.5} fill="currentColor" />
      <rect x={11} y={1} width={3} height={11} rx={0.5} fill="currentColor" />
    </svg>
  );
}
