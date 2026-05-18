import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { PostCallWrapUp } from '../components/PostCallWrapUp';

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
}));

const defaultProps = {
  doctorName: 'Dr House',
  patientName: 'Pat Ient',
  notes: 'Patient is recovering well',
  elapsedSeconds: 1200,
};

describe('PostCallWrapUp', () => {
  it('renders patient summary for patient role', () => {
    render(<PostCallWrapUp {...defaultProps} role="Patient" />);

    const heading = screen.getByRole('heading');
    expect(heading.textContent).toMatch(/consult/i);
    expect(heading.textContent).toMatch(/complete/i);
    expect(screen.getByText(/Dr House/)).toBeDefined();
  });

  it('renders doctor finalize for doctor role', () => {
    render(<PostCallWrapUp {...defaultProps} role="Doctor" />);

    const heading = screen.getByRole('heading', { level: 2 });
    expect(heading.textContent).toMatch(/save your/i);
    expect(heading.textContent).toMatch(/consult/i);
  });

  it('patient view shows doctor name and duration', () => {
    render(<PostCallWrapUp {...defaultProps} role="Patient" />);

    expect(screen.getByText(/Dr House/)).toBeDefined();
    expect(screen.getByText(/20/)).toBeDefined();
  });

  it('doctor view pre-fills notes textarea', () => {
    render(<PostCallWrapUp {...defaultProps} role="Doctor" />);

    const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
    expect(textarea.value).toBe('Patient is recovering well');
  });
});
