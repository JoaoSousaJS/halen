import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ToggleSwitch } from './ToggleSwitch';

describe('ToggleSwitch', () => {
  it('renders with role="switch" and aria-checked="false" when unchecked', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} />);

    const toggle = screen.getByRole('switch');
    expect(toggle).toHaveAttribute('aria-checked', 'false');
  });

  it('renders with aria-checked="true" when checked', () => {
    render(<ToggleSwitch checked={true} onChange={vi.fn()} />);

    expect(screen.getByRole('switch')).toHaveAttribute('aria-checked', 'true');
  });

  it('calls onChange(true) when clicked while unchecked', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<ToggleSwitch checked={false} onChange={onChange} />);

    await user.click(screen.getByRole('switch'));
    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('calls onChange(false) when clicked while checked', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<ToggleSwitch checked={true} onChange={onChange} />);

    await user.click(screen.getByRole('switch'));
    expect(onChange).toHaveBeenCalledWith(false);
  });

  it('does not call onChange when disabled', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<ToggleSwitch checked={false} onChange={onChange} disabled />);

    await user.click(screen.getByRole('switch'));
    expect(onChange).not.toHaveBeenCalled();
  });

  it('does not call onChange when loading', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<ToggleSwitch checked={false} onChange={onChange} loading />);

    await user.click(screen.getByRole('switch'));
    expect(onChange).not.toHaveBeenCalled();
  });

  it('sets aria-busy="true" when loading', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} loading />);

    expect(screen.getByRole('switch')).toHaveAttribute('aria-busy', 'true');
  });

  it('does not set aria-busy when not loading', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} />);

    expect(screen.getByRole('switch')).not.toHaveAttribute('aria-busy', 'true');
  });

  it('renders label text when label prop is provided', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} label="Active" />);

    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  it('does not render label element when label prop is omitted', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} />);

    const toggle = screen.getByRole('switch');
    expect(toggle.querySelector('.toggle-switch-label')).toBeNull();
  });

  it('applies toggle-switch--checked class when checked', () => {
    render(<ToggleSwitch checked={true} onChange={vi.fn()} />);

    expect(screen.getByRole('switch')).toHaveClass('toggle-switch--checked');
  });

  it('does not apply toggle-switch--checked class when unchecked', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} />);

    expect(screen.getByRole('switch')).not.toHaveClass('toggle-switch--checked');
  });

  it('applies toggle-switch--disabled class when disabled', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} disabled />);

    expect(screen.getByRole('switch')).toHaveClass('toggle-switch--disabled');
  });

  it('applies toggle-switch--loading class when loading', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} loading />);

    expect(screen.getByRole('switch')).toHaveClass('toggle-switch--loading');
  });

  it('toggles on Space key press', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<ToggleSwitch checked={false} onChange={onChange} />);

    screen.getByRole('switch').focus();
    await user.keyboard(' ');
    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('toggles on Enter key press', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<ToggleSwitch checked={false} onChange={onChange} />);

    screen.getByRole('switch').focus();
    await user.keyboard('{Enter}');
    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('uses label as aria-label when ariaLabel is not provided', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} label="Active" />);

    expect(screen.getByRole('switch')).toHaveAttribute('aria-label', 'Active');
  });

  it('uses ariaLabel prop over label for aria-label', () => {
    render(
      <ToggleSwitch
        checked={false}
        onChange={vi.fn()}
        label="Active"
        ariaLabel="Set clinic active status"
      />,
    );

    expect(screen.getByRole('switch')).toHaveAttribute(
      'aria-label',
      'Set clinic active status',
    );
  });

  it('is disabled when disabled prop is true', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} disabled />);

    expect(screen.getByRole('switch')).toBeDisabled();
  });

  it('is disabled when loading prop is true', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} loading />);

    expect(screen.getByRole('switch')).toBeDisabled();
  });

  it('has type="button"', () => {
    render(<ToggleSwitch checked={false} onChange={vi.fn()} />);

    expect(screen.getByRole('switch')).toHaveAttribute('type', 'button');
  });
});
