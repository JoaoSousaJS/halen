import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import CreateClinicAdminDialog from './CreateClinicAdminDialog';

const mockCreateClinicAdmin = vi.fn();

vi.mock('../../shared/api/clinics', () => ({
  createClinicAdmin: (...args: unknown[]) => mockCreateClinicAdmin(...args),
}));

vi.mock('../../shared/api/errors', () => ({
  getApiError: (err: unknown) =>
    err instanceof Error ? err.message : 'Something went wrong',
}));

function renderDialog(clinicId = 'c-001') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const onClose = vi.fn();
  const onCreated = vi.fn();
  return {
    onClose,
    onCreated,
    ...render(
      <QueryClientProvider client={client}>
        <CreateClinicAdminDialog clinicId={clinicId} onClose={onClose} onCreated={onCreated} />
      </QueryClientProvider>,
    ),
  };
}

describe('CreateClinicAdminDialog', () => {
  beforeEach(() => {
    mockCreateClinicAdmin.mockReset();
  });

  it('renders dialog title and form fields', () => {
    renderDialog();

    expect(screen.getByText('Create Clinic Admin')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Jane')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Doe')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('admin@clinic.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Min. 8 characters')).toBeInTheDocument();
  });

  it('calls API with correct payload on submit', async () => {
    mockCreateClinicAdmin.mockResolvedValue({ userId: 'u-123' });
    const user = userEvent.setup();
    renderDialog('c-042');

    await user.type(screen.getByPlaceholderText('Jane'), 'Alice');
    await user.type(screen.getByPlaceholderText('Doe'), 'Smith');
    await user.type(screen.getByPlaceholderText('admin@clinic.com'), 'alice@test.com');
    await user.type(screen.getByPlaceholderText('Min. 8 characters'), 'Strong1234!');
    await user.click(screen.getByRole('button', { name: 'Create admin' }));

    await waitFor(() => {
      expect(mockCreateClinicAdmin).toHaveBeenCalledWith('c-042', {
        email: 'alice@test.com',
        firstName: 'Alice',
        lastName: 'Smith',
        temporaryPassword: 'Strong1234!',
      });
    });
  });

  it('calls onCreated on success', async () => {
    mockCreateClinicAdmin.mockResolvedValue({ userId: 'u-123' });
    const user = userEvent.setup();
    const { onCreated } = renderDialog();

    await user.type(screen.getByPlaceholderText('Jane'), 'Alice');
    await user.type(screen.getByPlaceholderText('Doe'), 'Smith');
    await user.type(screen.getByPlaceholderText('admin@clinic.com'), 'alice@test.com');
    await user.type(screen.getByPlaceholderText('Min. 8 characters'), 'Strong1234!');
    await user.click(screen.getByRole('button', { name: 'Create admin' }));

    await waitFor(() => expect(onCreated).toHaveBeenCalled());
  });

  it('displays error on failure', async () => {
    mockCreateClinicAdmin.mockRejectedValue(new Error('Email already taken'));
    const user = userEvent.setup();
    renderDialog();

    await user.type(screen.getByPlaceholderText('Jane'), 'Alice');
    await user.type(screen.getByPlaceholderText('Doe'), 'Smith');
    await user.type(screen.getByPlaceholderText('admin@clinic.com'), 'alice@test.com');
    await user.type(screen.getByPlaceholderText('Min. 8 characters'), 'Strong1234!');
    await user.click(screen.getByRole('button', { name: 'Create admin' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Email already taken');
  });

  it('calls onClose when Cancel is clicked', async () => {
    const user = userEvent.setup();
    const { onClose } = renderDialog();

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(onClose).toHaveBeenCalled();
  });

  it('shows pending state on submit button', async () => {
    mockCreateClinicAdmin.mockReturnValue(new Promise(() => {}));
    const user = userEvent.setup();
    renderDialog();

    await user.type(screen.getByPlaceholderText('Jane'), 'Alice');
    await user.type(screen.getByPlaceholderText('Doe'), 'Smith');
    await user.type(screen.getByPlaceholderText('admin@clinic.com'), 'alice@test.com');
    await user.type(screen.getByPlaceholderText('Min. 8 characters'), 'Strong1234!');
    await user.click(screen.getByRole('button', { name: 'Create admin' }));

    expect(await screen.findByText('Creating...')).toBeInTheDocument();
  });
});
