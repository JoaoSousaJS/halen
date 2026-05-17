import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Dialog, DialogActions } from './Dialog';

describe('Dialog', () => {
  const defaultProps = {
    title: 'Confirm Appointment',
    onClose: vi.fn(),
  };

  it('renders the title', () => {
    render(
      <Dialog {...defaultProps}>
        <p>Content</p>
      </Dialog>,
    );

    expect(screen.getByText('Confirm Appointment')).toBeInTheDocument();
  });

  it('renders children', () => {
    render(
      <Dialog {...defaultProps}>
        <p>Are you sure?</p>
      </Dialog>,
    );

    expect(screen.getByText('Are you sure?')).toBeInTheDocument();
  });

  it('renders subtitle when provided', () => {
    render(
      <Dialog {...defaultProps} subtitle="Please review the details">
        <p>Content</p>
      </Dialog>,
    );

    const subtitle = screen.getByText('Please review the details');
    expect(subtitle).toBeInTheDocument();
    expect(subtitle).toHaveClass('dialog-subtitle');
  });

  it('does not render subtitle when not provided', () => {
    const { container } = render(
      <Dialog {...defaultProps}>
        <p>Content</p>
      </Dialog>,
    );

    expect(container.querySelector('.dialog-subtitle')).toBeNull();
  });

  it('applies dialog--md class when wide is true', () => {
    const { container } = render(
      <Dialog {...defaultProps} wide>
        <p>Content</p>
      </Dialog>,
    );

    const dialog = container.querySelector('.dialog');
    expect(dialog).toHaveClass('dialog', 'dialog--md');
  });

  it('does not apply dialog--md class when wide is false', () => {
    const { container } = render(
      <Dialog {...defaultProps}>
        <p>Content</p>
      </Dialog>,
    );

    const dialog = container.querySelector('.dialog');
    expect(dialog).not.toHaveClass('dialog--md');
  });

  it('calls onClose when overlay is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();

    const { container } = render(
      <Dialog title="Test" onClose={onClose}>
        <p>Content</p>
      </Dialog>,
    );

    const overlay = container.querySelector('.dialog-overlay')!;
    await user.click(overlay);

    expect(onClose).toHaveBeenCalledOnce();
  });

  it('does NOT call onClose when dialog body is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();

    const { container } = render(
      <Dialog title="Test" onClose={onClose}>
        <p>Inner content</p>
      </Dialog>,
    );

    const dialog = container.querySelector('.dialog')!;
    await user.click(dialog);

    expect(onClose).not.toHaveBeenCalled();
  });

  it('calls onClose when close button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();

    render(
      <Dialog title="Test" onClose={onClose}>
        <p>Content</p>
      </Dialog>,
    );

    await user.click(screen.getByRole('button', { name: /close dialog/i }));

    expect(onClose).toHaveBeenCalledOnce();
  });

  it('renders close button with aria-label', () => {
    render(
      <Dialog {...defaultProps}>
        <p>Content</p>
      </Dialog>,
    );

    expect(
      screen.getByRole('button', { name: 'Close dialog' }),
    ).toBeInTheDocument();
  });
});

describe('DialogActions', () => {
  it('renders children inside a dialog-actions container', () => {
    const { container } = render(
      <DialogActions>
        <button>Cancel</button>
        <button>Confirm</button>
      </DialogActions>,
    );

    const actions = container.querySelector('.dialog-actions');
    expect(actions).toBeInTheDocument();
    expect(screen.getByText('Cancel')).toBeInTheDocument();
    expect(screen.getByText('Confirm')).toBeInTheDocument();
  });
});
