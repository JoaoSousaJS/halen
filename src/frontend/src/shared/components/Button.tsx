import type { ButtonHTMLAttributes, ReactNode } from 'react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'danger' | 'ghost';
  size?: 'sm';
  block?: boolean;
  children: ReactNode;
  ariaLabel?: string;
}

export function Button({
  variant,
  size,
  block,
  ariaLabel,
  className,
  children,
  ...rest
}: ButtonProps) {
  const classes = [
    'btn',
    variant && `btn-${variant}`,
    size && `btn-${size}`,
    block && 'btn-block',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button className={classes} aria-label={ariaLabel} {...rest}>
      {children}
    </button>
  );
}
