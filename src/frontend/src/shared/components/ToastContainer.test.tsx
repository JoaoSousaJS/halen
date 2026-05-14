import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ToastContainer } from './ToastContainer';
import type { Toast } from '../hooks/useNotifications';

function makeToast(overrides: Partial<Toast> = {}): Toast {
  return {
    id: crypto.randomUUID(),
    message: 'New appointment with Maya Chen',
    type: 'appointment.booked',
    timestamp: Date.now(),
    ...overrides,
  };
}

describe('ToastContainer', () => {
  it('renders nothing when toasts array is empty', () => {
    const { container } = render(<ToastContainer toasts={[]} onDismiss={() => {}} />);
    expect(container.innerHTML).toBe('');
  });

  it('renders toast messages', () => {
    const toasts = [
      makeToast({ message: 'Appointment booked' }),
      makeToast({ message: 'Appointment cancelled', type: 'appointment.cancelled' }),
    ];

    render(<ToastContainer toasts={toasts} onDismiss={() => {}} />);

    expect(screen.getByText('Appointment booked')).toBeInTheDocument();
    expect(screen.getByText('Appointment cancelled')).toBeInTheDocument();
  });

  it('calls onDismiss with toast id when dismiss button is clicked', async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    const toast = makeToast({ id: 'toast-123' });

    render(<ToastContainer toasts={[toast]} onDismiss={onDismiss} />);

    await user.click(screen.getByRole('button', { name: /dismiss/i }));

    expect(onDismiss).toHaveBeenCalledWith('toast-123');
  });

  it('applies correct CSS class based on notification type', () => {
    const toast = makeToast({ type: 'appointment.cancelled' });

    render(<ToastContainer toasts={[toast]} onDismiss={() => {}} />);

    const toastEl = screen.getByText(toast.message).closest('.toast');
    expect(toastEl).toHaveClass('toast--cancelled');
  });
});
