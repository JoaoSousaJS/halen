import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect } from 'vitest';
import { PreCallLobby } from '../components/PreCallLobby';

const defaultProps = {
  doctorName: 'Dr House',
  patientName: 'Pat Ient',
  reason: 'Checkup',
  participants: [] as { name: string; role: string }[],
  onJoin: vi.fn(),
};

describe('PreCallLobby', () => {
  it('shows ready indicator when other participant is present', () => {
    render(
      <PreCallLobby
        {...defaultProps}
        role="Patient"
        participants={[{ name: 'Dr House', role: 'Doctor' }]}
      />,
    );

    expect(screen.getByText(/ready/i)).toBeDefined();
  });

  it('hides ready indicator when no other participant', () => {
    render(<PreCallLobby {...defaultProps} role="Patient" participants={[]} />);

    expect(screen.queryByText(/ready/i)).toBeNull();
  });

  it('shows "Join consult" for patients', () => {
    render(<PreCallLobby {...defaultProps} role="Patient" />);

    expect(screen.getByRole('button', { name: /join consult/i })).toBeDefined();
  });

  it('shows "Admit & start consult" for doctors', () => {
    render(<PreCallLobby {...defaultProps} role="Doctor" />);

    expect(screen.getByRole('button', { name: /admit.*start/i })).toBeDefined();
  });

  it('calls onJoin when button clicked', async () => {
    const user = userEvent.setup();
    const onJoin = vi.fn();

    render(<PreCallLobby {...defaultProps} role="Patient" onJoin={onJoin} />);

    await user.click(screen.getByRole('button', { name: /join consult/i }));
    expect(onJoin).toHaveBeenCalledOnce();
  });
});
