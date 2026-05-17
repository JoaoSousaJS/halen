import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Chip } from './Chip';

describe('Chip', () => {
  it('renders the status text', () => {
    render(<Chip status="Active" />);

    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  it('renders with only the chip class when no variant', () => {
    render(<Chip status="Pending" />);

    const chip = screen.getByText('Pending');
    expect(chip.tagName).toBe('SPAN');
    expect(chip).toHaveClass('chip');
    expect(chip.className).toBe('chip');
  });

  it('applies chip-good class for good variant', () => {
    render(<Chip status="Confirmed" variant="good" />);

    expect(screen.getByText('Confirmed')).toHaveClass('chip', 'chip-good');
  });

  it('applies chip-danger class for danger variant', () => {
    render(<Chip status="Cancelled" variant="danger" />);

    expect(screen.getByText('Cancelled')).toHaveClass('chip', 'chip-danger');
  });

  it('applies chip-warn class for warn variant', () => {
    render(<Chip status="Pending Review" variant="warn" />);

    expect(screen.getByText('Pending Review')).toHaveClass('chip', 'chip-warn');
  });
});
