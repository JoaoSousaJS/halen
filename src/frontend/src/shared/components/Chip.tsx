interface ChipProps {
  status: string;
  variant?: 'good' | 'danger' | 'warn';
}

export function Chip({ status, variant }: ChipProps) {
  const className = ['chip', variant && `chip-${variant}`].filter(Boolean).join(' ');
  return <span className={className}>{status}</span>;
}
