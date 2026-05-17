import type { ReactNode } from 'react';

interface FieldProps {
  label: string;
  hint?: string;
  children: ReactNode;
  inline?: boolean;
  row?: boolean;
}

export function Field({ label, hint, children, inline, row }: FieldProps) {
  const className = [
    'field',
    inline && 'field-inline',
  ]
    .filter(Boolean)
    .join(' ');

  if (row) {
    return <div className="field-row">{children}</div>;
  }

  if (inline) {
    return (
      <label className={className}>
        {children}
        <span>{label}</span>
      </label>
    );
  }

  return (
    <label className={className}>
      <span>{label}</span>
      {children}
      {hint && <span className="field-hint">{hint}</span>}
    </label>
  );
}
