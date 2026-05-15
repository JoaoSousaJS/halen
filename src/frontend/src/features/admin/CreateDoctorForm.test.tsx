import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import CreateDoctorForm from './CreateDoctorForm';
import { AxiosError } from 'axios';
import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockCreateDoctor = vi.fn().mockResolvedValue({ doctorId: 'd-001' });

vi.mock('../../shared/api/admin', () => ({
  createDoctor: (...args: unknown[]) => mockCreateDoctor(...args),
  listUsers: vi.fn(),
}));

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '1', email: 'admin@test.com', given_name: 'Admin', family_name: 'User', role: 'Admin', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

function renderForm() {
  localStorage.setItem('token', fakeJwt());
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <AuthProvider>
          <CreateDoctorForm />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function fillForm() {
  fireEvent.change(screen.getByPlaceholderText('James'), { target: { value: 'Gregory' } });
  fireEvent.change(screen.getByPlaceholderText('Wilson'), { target: { value: 'House' } });
  fireEvent.change(screen.getByPlaceholderText('doctor@halen.dev'), { target: { value: 'house@halen.dev' } });
  fireEvent.change(screen.getByPlaceholderText('8+ characters, include a digit'), { target: { value: 'Secure123!' } });
  fireEvent.change(screen.getByPlaceholderText('Cardiology'), { target: { value: 'Diagnostics' } });
  fireEvent.change(screen.getByPlaceholderText('MED-12345'), { target: { value: 'MED-99999' } });
  fireEvent.change(screen.getByPlaceholderText('150'), { target: { value: '200' } });
  fireEvent.change(screen.getByPlaceholderText('5'), { target: { value: '15' } });
}

describe('CreateDoctorForm', () => {
  beforeEach(() => {
    localStorage.clear();
    mockCreateDoctor.mockResolvedValue({ doctorId: 'd-001' });
  });

  it('renders heading and all form fields', () => {
    renderForm();
    expect(screen.getByText('doctor account.')).toBeDefined();
    expect(screen.getByPlaceholderText('James')).toBeDefined();
    expect(screen.getByPlaceholderText('Wilson')).toBeDefined();
    expect(screen.getByPlaceholderText('doctor@halen.dev')).toBeDefined();
    expect(screen.getByPlaceholderText('8+ characters, include a digit')).toBeDefined();
    expect(screen.getByPlaceholderText('Cardiology')).toBeDefined();
    expect(screen.getByPlaceholderText('MED-12345')).toBeDefined();
  });

  it('submits form with correct payload', async () => {
    renderForm();
    fillForm();

    await act(() => fireEvent.click(screen.getByText('Create doctor account')));

    await waitFor(() => {
      expect(mockCreateDoctor).toHaveBeenCalledWith({
        firstName: 'Gregory',
        lastName: 'House',
        email: 'house@halen.dev',
        password: 'Secure123!',
        specialty: 'Diagnostics',
        licenseNumber: 'MED-99999',
        consultationFee: 200,
        yearsOfExperience: 15,
      });
    });
  });

  it('shows success message after creation', async () => {
    renderForm();
    fillForm();

    await act(() => fireEvent.click(screen.getByText('Create doctor account')));

    expect(await screen.findByText('Doctor account created for house@halen.dev')).toBeDefined();
  });

  it('resets form after successful creation', async () => {
    renderForm();
    fillForm();

    await act(() => fireEvent.click(screen.getByText('Create doctor account')));

    await screen.findByText('Doctor account created for house@halen.dev');
    expect((screen.getByPlaceholderText('James') as HTMLInputElement).value).toBe('');
    expect((screen.getByPlaceholderText('Wilson') as HTMLInputElement).value).toBe('');
  });

  it('shows error message when API fails', async () => {
    const axiosErr = new AxiosError('Request failed', '400', undefined, undefined, {
      status: 400, data: { error: 'Email already exists' }, statusText: 'Bad Request', headers: {}, config: {},
    } as never);
    mockCreateDoctor.mockRejectedValue(axiosErr);
    renderForm();
    fillForm();

    await act(() => fireEvent.click(screen.getByText('Create doctor account')));

    expect(await screen.findByText('Email already exists')).toBeDefined();
  });

  it('disables button while submitting', async () => {
    mockCreateDoctor.mockReturnValue(new Promise(() => {}));
    renderForm();
    fillForm();

    await act(() => fireEvent.click(screen.getByText('Create doctor account')));

    expect(screen.getByText('Creating account…')).toBeDefined();
  });
});
