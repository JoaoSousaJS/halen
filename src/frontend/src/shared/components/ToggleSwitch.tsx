interface ToggleSwitchProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
  loading?: boolean;
  label?: string;
  ariaLabel?: string;
}

export function ToggleSwitch({
  checked,
  onChange,
  disabled,
  loading,
  label,
  ariaLabel,
}: ToggleSwitchProps) {
  const classes = [
    'toggle-switch',
    checked && 'toggle-switch--checked',
    disabled && 'toggle-switch--disabled',
    loading && 'toggle-switch--loading',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={ariaLabel ?? label}
      aria-busy={loading || undefined}
      disabled={disabled || loading}
      className={classes}
      onClick={() => onChange(!checked)}
    >
      <span className="toggle-switch-track" aria-hidden="true">
        <span className="toggle-switch-knob" />
      </span>
      {label ? <span className="toggle-switch-label">{label}</span> : null}
    </button>
  );
}
