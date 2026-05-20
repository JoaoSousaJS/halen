import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
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

function renderCard(doctor: DoctorSearchDto, onSelect = vi.fn()) {
  return render(
    <MemoryRouter>
      <DoctorCard doctor={doctor} onSelect={onSelect} />
    </MemoryRouter>,
  );
}

describe('DoctorCard', () => {
  it('renders doctor name and specialty', () => {
    renderCard(makeDoctor({ name: 'Dr. Silva', specialty: 'Cardiology' }));

    expect(screen.getByText('Dr. Silva')).toBeDefined();
    expect(screen.getByText('Cardiology')).toBeDefined();
  });

  it('renders consultation fee', () => {
    renderCard(makeDoctor({ consultationFee: 150 }));

    expect(screen.getByText('$150')).toBeDefined();
  });

  it('renders years of experience', () => {
    renderCard(makeDoctor({ yearsOfExperience: 10 }));

    expect(screen.getByText('10 years experience')).toBeDefined();
  });

  it('renders next available slot', () => {
    renderCard(makeDoctor({
      nextAvailableSlot: { startUtc: '2026-05-19T09:00:00Z', dayOfWeek: 'Monday' },
    }));

    expect(screen.getByText(/Monday/)).toBeDefined();
  });

  it('renders "No upcoming availability" for null slot', () => {
    renderCard(makeDoctor({ nextAvailableSlot: null }));

    expect(screen.getByText('No upcoming availability')).toBeDefined();
  });

  it('calls onSelect when Select button clicked', async () => {
    const user = userEvent.setup();
    const doctor = makeDoctor();
    const onSelect = vi.fn();
    renderCard(doctor, onSelect);

    await user.click(screen.getByRole('button', { name: 'Select Dr. Silva' }));

    expect(onSelect).toHaveBeenCalledTimes(1);
    expect(onSelect).toHaveBeenCalledWith(doctor);
  });

  it('renders View profile link', () => {
    const doctor = makeDoctor({ id: 'abc-123' });
    renderCard(doctor);

    const link = screen.getByRole('link', { name: 'View profile' });
    expect(link).toBeDefined();
    expect(link.getAttribute('href')).toBe('/doctors/abc-123/profile');
  });
});
