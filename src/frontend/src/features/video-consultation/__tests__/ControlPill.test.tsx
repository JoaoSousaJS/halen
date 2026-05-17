import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect } from 'vitest';
import { ControlPill } from '../components/ControlPill';

const defaultControls = {
  mic: true,
  cam: true,
  chatOpen: false,
  sidebarOpen: false,
};

describe('ControlPill', () => {
  it('renders mic and cam buttons for patient', () => {
    render(
      <ControlPill
        role="Patient"
        controls={defaultControls}
        onToggleMic={vi.fn()}
        onToggleCam={vi.fn()}
        onToggleChat={vi.fn()}
        onToggleSidebar={vi.fn()}
        onEndCall={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: /mic/i })).toBeDefined();
    expect(screen.getByRole('button', { name: /cam/i })).toBeDefined();
    expect(screen.getByRole('button', { name: /chat/i })).toBeDefined();
  });

  it('hides sidebar toggle and end call for patient', () => {
    render(
      <ControlPill
        role="Patient"
        controls={defaultControls}
        onToggleMic={vi.fn()}
        onToggleCam={vi.fn()}
        onToggleChat={vi.fn()}
        onToggleSidebar={vi.fn()}
        onEndCall={vi.fn()}
      />,
    );

    expect(screen.queryByRole('button', { name: /sidebar/i })).toBeNull();
    expect(screen.queryByRole('button', { name: /end/i })).toBeNull();
  });

  it('shows sidebar toggle and end call for doctor', () => {
    render(
      <ControlPill
        role="Doctor"
        controls={defaultControls}
        onToggleMic={vi.fn()}
        onToggleCam={vi.fn()}
        onToggleChat={vi.fn()}
        onToggleSidebar={vi.fn()}
        onEndCall={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: /sidebar/i })).toBeDefined();
    expect(screen.getByRole('button', { name: /end/i })).toBeDefined();
  });

  it('calls onToggleMic when mic button clicked', async () => {
    const user = userEvent.setup();
    const onToggleMic = vi.fn();

    render(
      <ControlPill
        role="Doctor"
        controls={defaultControls}
        onToggleMic={onToggleMic}
        onToggleCam={vi.fn()}
        onToggleChat={vi.fn()}
        onToggleSidebar={vi.fn()}
        onEndCall={vi.fn()}
      />,
    );

    await user.click(screen.getByRole('button', { name: /mic/i }));
    expect(onToggleMic).toHaveBeenCalledOnce();
  });

  it('reflects active state on toggle buttons', () => {
    render(
      <ControlPill
        role="Doctor"
        controls={{ ...defaultControls, mic: false, chatOpen: true }}
        onToggleMic={vi.fn()}
        onToggleCam={vi.fn()}
        onToggleChat={vi.fn()}
        onToggleSidebar={vi.fn()}
        onEndCall={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: /mic/i })).toHaveAttribute('data-active', 'false');
    expect(screen.getByRole('button', { name: /chat/i })).toHaveAttribute('data-active', 'true');
  });
});
