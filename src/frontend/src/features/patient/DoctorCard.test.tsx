import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import DoctorCard from './DoctorCard';
import type { DoctorSearchDto } from '../../shared/api/doctors';

function makeDoctor(overrides: Partial<DoctorSearchDto> = {}): DoctorSearchDto {
  return {
    id: crypto.randomUUID(),
    name: 'Dr. Silva',
    specialty: 'Cardiology',
    consultationFee: 150,
    yearsOfExperience: 10,
    languages: ['English', 'Portuguese'],
    nextAvailableSlot: null,
    ...overrides,
  };
}

describe('DoctorCard', () => {
  it('renders doctor name and specialty', () => {
    const doctor = makeDoctor({ name: 'Dr. Silva', specialty: 'Cardiology' });
    render(<DoctorCard doctor={doctor} onSelect={vi.fn()} />);

    expect(screen.getByText('Dr. Silva')).toBeDefined();
    expect(screen.getByText('Cardiology')).toBeDefined();
  });

  it('renders consultation fee', () => {
    const doctor = makeDoctor({ consultationFee: 150 });
    render(<DoctorCard doctor={doctor} onSelect={vi.fn()} />);

    expect(screen.getByText('$150')).toBeDefined();
  });

  it('renders years of experience', () => {
    const doctor = makeDoctor({ yearsOfExperience: 10 });
    render(<DoctorCard doctor={doctor} onSelect={vi.fn()} />);

    expect(screen.getByText('10 years experience')).toBeDefined();
  });

  it('renders next available slot', () => {
    const doctor = makeDoctor({
      nextAvailableSlot: { startUtc: '2026-05-19T09:00:00Z', dayOfWeek: 'Monday' },
    });
    render(<DoctorCard doctor={doctor} onSelect={vi.fn()} />);

    expect(screen.getByText(/Monday/)).toBeDefined();
  });

  it('renders "No upcoming availability" for null slot', () => {
    const doctor = makeDoctor({ nextAvailableSlot: null });
    render(<DoctorCard doctor={doctor} onSelect={vi.fn()} />);

    expect(screen.getByText('No upcoming availability')).toBeDefined();
  });

  it('calls onSelect when Select button clicked', async () => {
    const user = userEvent.setup();
    const doctor = makeDoctor();
    const onSelect = vi.fn();
    render(<DoctorCard doctor={doctor} onSelect={onSelect} />);

    await user.click(screen.getByRole('button', { name: 'Select Dr. Silva' }));

    expect(onSelect).toHaveBeenCalledTimes(1);
    expect(onSelect).toHaveBeenCalledWith(doctor);
  });
});
