import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import ProfilePage from './ProfilePage';
import type { ProfileDto } from '../../shared/api/profile';

const mockGetMyProfile = vi.fn();
const mockUpdateMyProfile = vi.fn();
const mockChangePassword = vi.fn();

vi.mock('../../shared/api/profile', () => ({
  getMyProfile: (...args: unknown[]) => mockGetMyProfile(...args),
  updateMyProfile: (...args: unknown[]) => mockUpdateMyProfile(...args),
  changePassword: (...args: unknown[]) => mockChangePassword(...args),
}));

vi.mock('react-router-dom', () => ({
  Link: ({ children, ...props }: any) => <a {...props}>{children}</a>,
}));

vi.mock('../../shared/components/AuthProvider', () => ({
  useAuth: () => ({
    user: { given_name: 'Test', family_name: 'User', role: 'Patient' },
    logout: vi.fn(),
    token: 'fake',
  }),
}));

function makePatientProfile(overrides: Partial<ProfileDto> = {}): ProfileDto {
  return {
    id: 'p-001',
    firstName: 'Maria',
    lastName: 'Santos',
    email: 'maria@example.com',
    role: 'Patient',
    createdAt: '2025-01-15T10:00:00Z',
    lastLoginAt: '2026-05-17T08:00:00Z',
    specialty: null,
    consultationFee: null,
    yearsOfExperience: null,
    languages: null,
    dateOfBirth: '1990-06-15',
    city: 'Lisbon',
    subscriptionPlan: null,
    ...overrides,
  };
}

function makeDoctorProfile(overrides: Partial<ProfileDto> = {}): ProfileDto {
  return {
    id: 'd-001',
    firstName: 'Carlos',
    lastName: 'Mendes',
    email: 'carlos@example.com',
    role: 'Doctor',
    createdAt: '2024-11-01T10:00:00Z',
    lastLoginAt: '2026-05-17T09:00:00Z',
    specialty: 'Cardiology',
    consultationFee: 150.0,
    yearsOfExperience: 12,
    languages: ['English', 'Portuguese'],
    dateOfBirth: null,
    city: null,
    subscriptionPlan: null,
    ...overrides,
  };
}

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <ProfilePage />
    </QueryClientProvider>,
  );
}

describe('ProfilePage', () => {
  beforeEach(() => {
    mockGetMyProfile.mockReset();
    mockUpdateMyProfile.mockReset();
    mockChangePassword.mockReset();
  });

  // ── Patient role ──────────────────────────────────────

  describe('Patient role', () => {
    it('renders profile data for patient role (shows DateOfBirth, City)', async () => {
      mockGetMyProfile.mockResolvedValue(makePatientProfile());
      renderPage();

      // Wait for profile to load
      expect(await screen.findByDisplayValue('Maria')).toBeDefined();
      expect(screen.getByDisplayValue('Santos')).toBeDefined();
      expect(screen.getByDisplayValue('maria@example.com')).toBeDefined();

      // Patient-specific fields
      expect(screen.getByText('Date of birth')).toBeDefined();
      expect(screen.getByDisplayValue('1990-06-15')).toBeDefined();
      expect(screen.getByText('City')).toBeDefined();
      expect(screen.getByDisplayValue('Lisbon')).toBeDefined();
    });

    it('hides doctor-specific fields for patient role', async () => {
      mockGetMyProfile.mockResolvedValue(makePatientProfile());
      renderPage();

      await screen.findByDisplayValue('Maria');

      expect(screen.queryByText('Specialty')).toBeNull();
      expect(screen.queryByText('Consultation fee')).toBeNull();
      expect(screen.queryByText('Years of experience')).toBeNull();
    });
  });

  // ── Doctor role ───────────────────────────────────────

  describe('Doctor role', () => {
    it('shows doctor-specific fields (Specialty, ConsultationFee) as disabled', async () => {
      mockGetMyProfile.mockResolvedValue(makeDoctorProfile());
      renderPage();

      await screen.findByDisplayValue('Carlos');

      // Doctor-specific fields should be present and disabled
      expect(screen.getByText('Specialty')).toBeDefined();
      const specialtyInput = screen.getByDisplayValue('Cardiology') as HTMLInputElement;
      expect(specialtyInput.disabled).toBe(true);

      expect(screen.getByText('Consultation fee')).toBeDefined();
      const feeInput = screen.getByDisplayValue('$150.00') as HTMLInputElement;
      expect(feeInput.disabled).toBe(true);

      expect(screen.getByText('Years of experience')).toBeDefined();
      const expInput = screen.getByDisplayValue('12') as HTMLInputElement;
      expect(expInput.disabled).toBe(true);
    });

    it('shows doctor languages as chips', async () => {
      mockGetMyProfile.mockResolvedValue(makeDoctorProfile());
      renderPage();

      await screen.findByDisplayValue('Carlos');

      expect(screen.getByText('Languages')).toBeDefined();
      expect(screen.getByText('English')).toBeDefined();
      expect(screen.getByText('Portuguese')).toBeDefined();
    });

    it('hides patient-specific fields for doctor role', async () => {
      mockGetMyProfile.mockResolvedValue(makeDoctorProfile());
      renderPage();

      await screen.findByDisplayValue('Carlos');

      expect(screen.queryByText('Date of birth')).toBeNull();
      expect(screen.queryByText('City')).toBeNull();
    });
  });

  // ── Save profile ──────────────────────────────────────

  describe('Save profile mutation', () => {
    it('calls updateMyProfile with correct payload for patient', async () => {
      const user = userEvent.setup();
      mockGetMyProfile.mockResolvedValue(makePatientProfile());
      mockUpdateMyProfile.mockResolvedValue(undefined);
      renderPage();

      await screen.findByDisplayValue('Maria');

      // Edit the first name
      const firstNameInput = screen.getByDisplayValue('Maria');
      await user.clear(firstNameInput);
      await user.type(firstNameInput, 'Ana');

      // Click save
      await user.click(screen.getByText('Save changes'));

      await waitFor(() => {
        expect(mockUpdateMyProfile).toHaveBeenCalledTimes(1);
        const payload = mockUpdateMyProfile.mock.calls[0][0];
        expect(payload).toEqual({
          firstName: 'Ana',
          lastName: 'Santos',
          dateOfBirth: '1990-06-15',
          city: 'Lisbon',
        });
      });
    });

    it('calls updateMyProfile with null dateOfBirth and city for doctor', async () => {
      const user = userEvent.setup();
      mockGetMyProfile.mockResolvedValue(makeDoctorProfile());
      mockUpdateMyProfile.mockResolvedValue(undefined);
      renderPage();

      await screen.findByDisplayValue('Carlos');

      await user.click(screen.getByText('Save changes'));

      await waitFor(() => {
        expect(mockUpdateMyProfile).toHaveBeenCalledTimes(1);
        const payload = mockUpdateMyProfile.mock.calls[0][0];
        expect(payload).toEqual({
          firstName: 'Carlos',
          lastName: 'Mendes',
          dateOfBirth: null,
          city: null,
        });
      });
    });
  });

  // ── Password change ───────────────────────────────────

  describe('Password change', () => {
    it('validates confirm password matches new password', async () => {
      const user = userEvent.setup();
      mockGetMyProfile.mockResolvedValue(makePatientProfile());
      renderPage();

      await screen.findByDisplayValue('Maria');

      // Password inputs are type="password" — grab them in order:
      // current, new, confirm
      const pwInputs = document.querySelectorAll<HTMLInputElement>('input[type="password"]');
      expect(pwInputs.length).toBe(3);

      await user.type(pwInputs[0], 'oldpass123');
      await user.type(pwInputs[1], 'newpass456');
      await user.type(pwInputs[2], 'differentpass789');

      // Submit the password form (second form on the page)
      const forms = document.querySelectorAll('form');
      fireEvent.submit(forms[1]);

      // Should NOT call the API
      await waitFor(() => {
        expect(mockChangePassword).not.toHaveBeenCalled();
      });
    });

    it('shows error message on password mismatch', async () => {
      const user = userEvent.setup();
      mockGetMyProfile.mockResolvedValue(makePatientProfile());
      renderPage();

      await screen.findByDisplayValue('Maria');

      const pwInputs = document.querySelectorAll<HTMLInputElement>('input[type="password"]');

      await user.type(pwInputs[0], 'oldpass123');
      await user.type(pwInputs[1], 'newpass456');
      await user.type(pwInputs[2], 'differentpass789');

      const forms = document.querySelectorAll('form');
      fireEvent.submit(forms[1]);

      expect(
        await screen.findByText('New password and confirmation do not match.'),
      ).toBeDefined();
    });

    it('calls changePassword with correct payload when passwords match', async () => {
      const user = userEvent.setup();
      mockGetMyProfile.mockResolvedValue(makePatientProfile());
      mockChangePassword.mockResolvedValue(undefined);
      renderPage();

      await screen.findByDisplayValue('Maria');

      const pwInputs = document.querySelectorAll<HTMLInputElement>('input[type="password"]');

      await user.type(pwInputs[0], 'oldpass123');
      await user.type(pwInputs[1], 'newpass456');
      await user.type(pwInputs[2], 'newpass456');

      const forms = document.querySelectorAll('form');
      fireEvent.submit(forms[1]);

      await waitFor(() => {
        expect(mockChangePassword).toHaveBeenCalledTimes(1);
        const payload = mockChangePassword.mock.calls[0][0];
        expect(payload).toEqual({
          currentPassword: 'oldpass123',
          newPassword: 'newpass456',
        });
      });
    });
  });

  // ── Loading state ─────────────────────────────────────

  it('shows loading state', () => {
    mockGetMyProfile.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText('Loading...')).toBeDefined();
  });
});
