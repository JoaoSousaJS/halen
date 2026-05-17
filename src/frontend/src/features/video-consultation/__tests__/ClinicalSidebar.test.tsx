import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect } from 'vitest';
import { ClinicalSidebar } from '../components/ClinicalSidebar';

const defaultProps = {
  notes: '',
  patientName: 'Pat Ient',
  onUpdateNotes: vi.fn(),
  onClose: vi.fn(),
};

describe('ClinicalSidebar', () => {
  it('renders all tabs', () => {
    render(<ClinicalSidebar {...defaultProps} />);

    expect(screen.getByRole('tab', { name: /summary/i })).toBeDefined();
    expect(screen.getByRole('tab', { name: /history/i })).toBeDefined();
    expect(screen.getByRole('tab', { name: /vitals/i })).toBeDefined();
    expect(screen.getByRole('tab', { name: /notes/i })).toBeDefined();
    expect(screen.getByRole('tab', { name: /rx/i })).toBeDefined();
    expect(screen.getByRole('tab', { name: /refer/i })).toBeDefined();
  });

  it('switches tab content on click', async () => {
    const user = userEvent.setup();
    render(<ClinicalSidebar {...defaultProps} />);

    await user.click(screen.getByRole('tab', { name: /notes/i }));
    expect(screen.getByRole('textbox')).toBeDefined();

    await user.click(screen.getByRole('tab', { name: /summary/i }));
    expect(screen.queryByRole('textbox')).toBeNull();
  });

  it('notes tab shows textarea with current value', async () => {
    const user = userEvent.setup();
    render(<ClinicalSidebar {...defaultProps} notes="Existing notes" />);

    await user.click(screen.getByRole('tab', { name: /notes/i }));
    const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
    expect(textarea.value).toBe('Existing notes');
  });

  it('notes tab calls onUpdateNotes on change', async () => {
    const user = userEvent.setup();
    const onUpdateNotes = vi.fn();
    render(<ClinicalSidebar {...defaultProps} onUpdateNotes={onUpdateNotes} />);

    await user.click(screen.getByRole('tab', { name: /notes/i }));
    await user.type(screen.getByRole('textbox'), 'New note');

    expect(onUpdateNotes).toHaveBeenCalled();
  });
});
