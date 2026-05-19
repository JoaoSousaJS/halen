import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import ClinicDetailPage from './ClinicDetailPage';
import type { ClinicDetailsDto } from '../../shared/api/clinics';

const mockGetClinic = vi.fn();
const mockUpdateClinic = vi.fn();
const mockSetFeatureFlag = vi.fn();

vi.mock('../../shared/api/clinics', () => ({
  getClinic: (...args: unknown[]) => mockGetClinic(...args),
  updateClinic: (...args: unknown[]) => mockUpdateClinic(...args),
  setFeatureFlag: (...args: unknown[]) => mockSetFeatureFlag(...args),
}));

vi.mock('../../shared/api/errors', () => ({
  getApiError: (err: unknown) =>
    err instanceof Error ? err.message : 'Something went wrong',
}));

function makeClinic(overrides: Partial<ClinicDetailsDto> = {}): ClinicDetailsDto {
  return {
    id: 'c-001',
    name: 'Sunrise Health',
    slug: 'sunrise',
    isActive: true,
    userCount: 42,
    featureFlags: [
      { featureKey: 'prescriptions', isEnabled: true },
      { featureKey: 'kyc', isEnabled: false },
      { featureKey: 'video_calls', isEnabled: true },
      { featureKey: 'doctor_reviews', isEnabled: false },
      { featureKey: 'medical_records', isEnabled: true },
      { featureKey: 'messaging', isEnabled: false },
      { featureKey: 'audit_trail', isEnabled: true },
    ],
    createdAt: '2026-01-03T10:00:00Z',
    ...overrides,
  };
}

function renderPage(clinicId = 'c-001') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const onBack = vi.fn();
  return {
    onBack,
    ...render(
      <QueryClientProvider client={client}>
        <ClinicDetailPage clinicId={clinicId} onBack={onBack} />
      </QueryClientProvider>,
    ),
  };
}

describe('ClinicDetailPage', () => {
  beforeEach(() => {
    mockGetClinic.mockReset();
    mockUpdateClinic.mockReset();
    mockSetFeatureFlag.mockReset();
  });

  describe('layout', () => {
    it('renders two-column layout', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      const { container } = renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      expect(container.querySelector('.clinic-detail-columns')).not.toBeNull();
    });

    it('renders Clinic Settings section', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      expect(await screen.findByText('Clinic Settings')).toBeInTheDocument();
    });

    it('renders Feature Flags section', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      expect(await screen.findByText('Feature Flags')).toBeInTheDocument();
    });
  });

  describe('name inline editing', () => {
    it('shows clinic name in view mode with pencil icon', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      const { container } = renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const pencil = container.querySelector('.settings-field-pencil');
      expect(pencil).not.toBeNull();
    });

    it('clicking the name field enters edit mode with input prefilled', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      const { container } = renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      expect(input).toBeInTheDocument();
      expect(container.querySelector('.settings-field-editing')).not.toBeNull();
    });

    it('pressing Save calls updateClinic with name and current status', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      mockUpdateClinic.mockResolvedValue(undefined);
      renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      await user.clear(input);
      await user.type(input, 'New Clinic Name');

      await user.click(screen.getByRole('button', { name: 'Save' }));

      await waitFor(() => {
        expect(mockUpdateClinic).toHaveBeenCalledWith('c-001', {
          name: 'New Clinic Name',
          isActive: true,
        });
      });
    });

    it('on save success exits edit mode', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      mockUpdateClinic.mockResolvedValue(undefined);
      const { container } = renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);
      await user.click(screen.getByRole('button', { name: 'Save' }));

      await waitFor(() => {
        expect(container.querySelector('.settings-field-editing')).toBeNull();
      });
    });

    it('on save error shows settingsError inline and stays in edit mode', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      mockUpdateClinic.mockRejectedValue(new Error('Network error'));
      const { container } = renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);
      await user.click(screen.getByRole('button', { name: 'Save' }));

      expect(await screen.findByText('Network error')).toBeInTheDocument();
      expect(container.querySelector('.settings-field-editing')).not.toBeNull();
    });

    it('pressing Cancel reverts to view mode without calling updateClinic', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      await user.clear(input);
      await user.type(input, 'Changed Name');

      await user.click(screen.getByRole('button', { name: /Cancel/ }));

      expect(mockUpdateClinic).not.toHaveBeenCalled();
      expect(screen.queryByDisplayValue('Changed Name')).toBeNull();
    });

    it('save handler rejects whitespace-only name and shows error', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      await user.clear(input);
      await user.type(input, '   ');

      await user.click(screen.getByRole('button', { name: 'Save' }));

      expect(await screen.findByText(/at least 3 characters/)).toBeInTheDocument();
      expect(mockUpdateClinic).not.toHaveBeenCalled();
    });

    it('save handler rejects names shorter than 3 characters', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      await user.clear(input);
      await user.type(input, 'AB');

      await user.click(screen.getByRole('button', { name: 'Save' }));

      expect(await screen.findByText(/at least 3 characters/)).toBeInTheDocument();
      expect(mockUpdateClinic).not.toHaveBeenCalled();
    });

    it('Enter key in input triggers save', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      mockUpdateClinic.mockResolvedValue(undefined);
      renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      await user.type(input, '{Enter}');

      await waitFor(() => {
        expect(mockUpdateClinic).toHaveBeenCalledOnce();
      });
    });

    it('Escape key in input triggers cancel', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      const { container } = renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      await user.type(input, '{Escape}');

      expect(container.querySelector('.settings-field-editing')).toBeNull();
      expect(mockUpdateClinic).not.toHaveBeenCalled();
    });

    it('input has maxLength of 200', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);

      const input = screen.getByDisplayValue('Sunrise Health');
      expect(input).toHaveAttribute('maxLength', '200');
    });

    it('error messages have role="alert"', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic());
      mockUpdateClinic.mockRejectedValue(new Error('Save failed'));
      renderPage();

      const nameButton = await screen.findByRole('button', { name: /Sunrise Health/ });
      await user.click(nameButton);
      await user.click(screen.getByRole('button', { name: 'Save' }));

      const alert = await screen.findByRole('alert');
      expect(alert).toHaveTextContent('Save failed');
    });
  });

  describe('status toggle', () => {
    it('renders ToggleSwitch with checked matching isActive', async () => {
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: true }));
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const switches = screen.getAllByRole('switch');
      const statusSwitch = switches.find(
        (s) => s.getAttribute('aria-label')?.toLowerCase().includes('status'),
      );
      expect(statusSwitch).toHaveAttribute('aria-checked', 'true');
    });

    it('shows label "Active" when isActive is true', async () => {
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: true }));
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      expect(screen.getByText('Active', { selector: '.toggle-switch-label' })).toBeInTheDocument();
    });

    it('shows label "Inactive" when isActive is false', async () => {
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: false }));
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      expect(screen.getByText('Inactive', { selector: '.toggle-switch-label' })).toBeInTheDocument();
    });

    it('deactivating shows confirmation dialog instead of toggling immediately', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: true }));
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const switches = screen.getAllByRole('switch');
      const statusSwitch = switches.find(
        (s) => s.getAttribute('aria-label')?.toLowerCase().includes('status'),
      );

      await user.click(statusSwitch!);

      expect(await screen.findByText(/deactivate this clinic/i)).toBeInTheDocument();
      expect(mockUpdateClinic).not.toHaveBeenCalled();
    });

    it('confirming the deactivation dialog calls updateClinic with isActive false', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: true }));
      mockUpdateClinic.mockResolvedValue(undefined);
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const switches = screen.getAllByRole('switch');
      const statusSwitch = switches.find(
        (s) => s.getAttribute('aria-label')?.toLowerCase().includes('status'),
      );
      await user.click(statusSwitch!);

      await screen.findByText(/deactivate this clinic/i);
      await user.click(screen.getByRole('button', { name: /deactivate/i }));

      await waitFor(() => {
        expect(mockUpdateClinic).toHaveBeenCalledWith('c-001', {
          name: 'Sunrise Health',
          isActive: false,
        });
      });
    });

    it('cancelling the deactivation dialog does not call updateClinic', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: true }));
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const switches = screen.getAllByRole('switch');
      const statusSwitch = switches.find(
        (s) => s.getAttribute('aria-label')?.toLowerCase().includes('status'),
      );
      await user.click(statusSwitch!);

      await screen.findByText(/deactivate this clinic/i);
      await user.click(screen.getByRole('button', { name: /cancel/i }));

      expect(mockUpdateClinic).not.toHaveBeenCalled();
      expect(screen.queryByText(/deactivate this clinic/i)).toBeNull();
    });

    it('activating an inactive clinic toggles directly without dialog', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: false }));
      mockUpdateClinic.mockResolvedValue(undefined);
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const switches = screen.getAllByRole('switch');
      const statusSwitch = switches.find(
        (s) => s.getAttribute('aria-label')?.toLowerCase().includes('status'),
      );
      await user.click(statusSwitch!);

      expect(screen.queryByText(/deactivate this clinic/i)).toBeNull();
      await waitFor(() => {
        expect(mockUpdateClinic).toHaveBeenCalledWith('c-001', {
          name: 'Sunrise Health',
          isActive: true,
        });
      });
    });

    it('deactivation dialog warns about user suspension', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: true }));
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const switches = screen.getAllByRole('switch');
      const statusSwitch = switches.find(
        (s) => s.getAttribute('aria-label')?.toLowerCase().includes('status'),
      );
      await user.click(statusSwitch!);

      expect(await screen.findByText(/all users.*will be suspended/i)).toBeInTheDocument();
    });

    it('on error shows settingsError inline', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: false }));
      mockUpdateClinic.mockRejectedValue(new Error('Status toggle failed'));
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const switches = screen.getAllByRole('switch');
      const statusSwitch = switches.find(
        (s) => s.getAttribute('aria-label')?.toLowerCase().includes('status'),
      );

      await user.click(statusSwitch!);

      expect(await screen.findByText('Status toggle failed')).toBeInTheDocument();
    });
  });

  describe('feature flags', () => {
    it('renders one flag card per featureFlag in the data', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      const { container } = renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      const cards = container.querySelectorAll('.flag-card');
      expect(cards.length).toBe(7);
    });

    it('each card shows human-readable label from FEATURE_META', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      await screen.findByText('Prescriptions');
      expect(screen.getByText('KYC Verification')).toBeInTheDocument();
      expect(screen.getByText('Video Calls')).toBeInTheDocument();
      expect(screen.getByText('Doctor Reviews')).toBeInTheDocument();
      expect(screen.getByText('Medical Records')).toBeInTheDocument();
      expect(screen.getByText('Messaging')).toBeInTheDocument();
      expect(screen.getByText('Audit Trail')).toBeInTheDocument();
    });

    it('each card shows description from FEATURE_META', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      await screen.findByText('Prescriptions');
      expect(screen.getByText('Allow doctors to issue prescriptions')).toBeInTheDocument();
      expect(screen.getByText('Require doctor identity verification')).toBeInTheDocument();
      expect(screen.getByText('Enable video consultation rooms')).toBeInTheDocument();
    });

    it('enabled flags have flag-card--enabled class', async () => {
      mockGetClinic.mockResolvedValue(
        makeClinic({
          featureFlags: [
            { featureKey: 'prescriptions', isEnabled: true },
            { featureKey: 'kyc', isEnabled: false },
          ],
        }),
      );
      const { container } = renderPage();

      await screen.findByText('Prescriptions');
      const cards = container.querySelectorAll('.flag-card');
      const enabledCards = container.querySelectorAll('.flag-card--enabled');
      expect(cards.length).toBe(2);
      expect(enabledCards.length).toBe(1);
    });

    it('toggling a flag calls setFeatureFlag with correct args', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(
        makeClinic({
          featureFlags: [{ featureKey: 'kyc', isEnabled: false }],
        }),
      );
      mockSetFeatureFlag.mockResolvedValue(undefined);
      renderPage();

      await screen.findByText('KYC Verification');
      const flagSwitch = screen.getByRole('switch', { name: /KYC Verification/i });
      await user.click(flagSwitch);

      await waitFor(() => {
        expect(mockSetFeatureFlag).toHaveBeenCalledWith('c-001', 'kyc', true);
      });
    });

    it('shows loading on the specific toggle being mutated', async () => {
      const user = userEvent.setup();
      let resolveFlag: () => void;
      mockGetClinic.mockResolvedValue(
        makeClinic({
          featureFlags: [
            { featureKey: 'prescriptions', isEnabled: true },
            { featureKey: 'kyc', isEnabled: false },
          ],
        }),
      );
      mockSetFeatureFlag.mockImplementation(
        () => new Promise<void>((resolve) => { resolveFlag = resolve; }),
      );
      renderPage();

      await screen.findByText('KYC Verification');
      const kycSwitch = screen.getByRole('switch', { name: /KYC Verification/i });
      await user.click(kycSwitch);

      expect(kycSwitch).toHaveAttribute('aria-busy', 'true');

      const prescSwitch = screen.getByRole('switch', { name: /Prescriptions/i });
      expect(prescSwitch).not.toHaveAttribute('aria-busy', 'true');

      resolveFlag!();
    });

    it('on toggle error shows flagError inline', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(
        makeClinic({
          featureFlags: [{ featureKey: 'kyc', isEnabled: false }],
        }),
      );
      mockSetFeatureFlag.mockRejectedValue(new Error('Toggle failed'));
      renderPage();

      await screen.findByText('KYC Verification');
      const flagSwitch = screen.getByRole('switch', { name: /KYC Verification/i });
      await user.click(flagSwitch);

      expect(await screen.findByText('Toggle failed')).toBeInTheDocument();
    });

    it('unknown feature keys fall back to displaying the raw key', async () => {
      mockGetClinic.mockResolvedValue(
        makeClinic({
          featureFlags: [{ featureKey: 'unknown_feature', isEnabled: true }],
        }),
      );
      renderPage();

      expect(await screen.findByText('unknown_feature')).toBeInTheDocument();
    });

    it('shows empty state when no feature flags exist', async () => {
      mockGetClinic.mockResolvedValue(makeClinic({ featureFlags: [] }));
      renderPage();

      expect(await screen.findByText('No feature flags configured.')).toBeInTheDocument();
    });

    it('flag error has role="alert"', async () => {
      const user = userEvent.setup();
      mockGetClinic.mockResolvedValue(
        makeClinic({
          featureFlags: [{ featureKey: 'kyc', isEnabled: false }],
        }),
      );
      mockSetFeatureFlag.mockRejectedValue(new Error('Flag error'));
      renderPage();

      await screen.findByText('KYC Verification');
      await user.click(screen.getByRole('switch', { name: /KYC Verification/i }));

      const alerts = await screen.findAllByRole('alert');
      const flagAlert = alerts.find((a) => a.textContent === 'Flag error');
      expect(flagAlert).toBeDefined();
    });
  });

  describe('meta stats', () => {
    it('renders slug, user count, and created date', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      await screen.findByRole('heading', { name: 'Sunrise Health' });
      expect(screen.getByText('sunrise')).toBeInTheDocument();
      expect(screen.getByText('42')).toBeInTheDocument();
      expect(screen.getByText('03/01/2026')).toBeInTheDocument();
    });
  });

  describe('create clinic admin', () => {
    it('renders the create clinic admin button', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      renderPage();

      expect(await screen.findByRole('button', { name: /create clinic admin/i })).toBeInTheDocument();
    });

    it('opens dialog when button is clicked', async () => {
      mockGetClinic.mockResolvedValue(makeClinic());
      const user = userEvent.setup();
      renderPage();

      const btn = await screen.findByRole('button', { name: /create clinic admin/i });
      await user.click(btn);

      expect(screen.getByText('Create Clinic Admin')).toBeInTheDocument();
    });

    it('disables button when clinic is inactive', async () => {
      mockGetClinic.mockResolvedValue(makeClinic({ isActive: false }));
      renderPage();

      const btn = await screen.findByRole('button', { name: /create clinic admin/i });
      expect(btn).toBeDisabled();
    });
  });
});
