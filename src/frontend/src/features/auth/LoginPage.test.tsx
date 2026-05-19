import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import LoginPage from './LoginPage';
import { useAuth } from '../../shared/components/AuthProvider';
import { login } from '../../shared/api/auth';

vi.mock('../../shared/components/AuthProvider', () => ({
  useAuth: vi.fn(),
}));

vi.mock('../../shared/api/auth', () => ({
  login: vi.fn(),
}));

const mockSaveToken = vi.fn();

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/dashboard" element={<div>Dashboard</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.mocked(useAuth).mockReturnValue({
      token: null,
      user: null,
      saveToken: mockSaveToken,
      logout: vi.fn(),
    });
    mockSaveToken.mockClear();
    vi.mocked(login).mockClear();
  });

  it('renders email and password fields', () => {
    renderLogin();
    expect(screen.getByPlaceholderText('you@example.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('••••••••')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument();
  });

  it('navigates to /dashboard after a successful login', async () => {
    vi.mocked(login).mockResolvedValueOnce({ token: 'fake-token' });
    renderLogin();

    await userEvent.type(screen.getByPlaceholderText('you@example.com'), 'user@test.com');
    await userEvent.type(screen.getByPlaceholderText('••••••••'), 'pass1234');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByText('Dashboard')).toBeInTheDocument());
    expect(mockSaveToken).toHaveBeenCalledWith('fake-token');
  });

  it('calls login with the entered credentials', async () => {
    vi.mocked(login).mockResolvedValueOnce({ token: 'fake-token' });
    renderLogin();

    await userEvent.type(screen.getByPlaceholderText('you@example.com'), 'user@test.com');
    await userEvent.type(screen.getByPlaceholderText('••••••••'), 'pass1234');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(login).toHaveBeenCalledWith('user@test.com', 'pass1234'));
  });

  it('shows an error message when login fails', async () => {
    vi.mocked(login).mockRejectedValueOnce(new Error('Invalid credentials'));
    renderLogin();

    await userEvent.type(screen.getByPlaceholderText('you@example.com'), 'bad@test.com');
    await userEvent.type(screen.getByPlaceholderText('••••••••'), 'wrongpass');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByText('Invalid credentials')).toBeInTheDocument());
    expect(mockSaveToken).not.toHaveBeenCalled();
  });

  it('always renders error slot to prevent layout shift', () => {
    renderLogin();
    const slot = document.querySelector('.auth-error-slot');
    expect(slot).toBeInTheDocument();
    expect(slot).toBeEmptyDOMElement();
  });

  it('shows error inside the error slot', async () => {
    vi.mocked(login).mockRejectedValueOnce(new Error('Bad creds'));
    renderLogin();

    await userEvent.type(screen.getByPlaceholderText('you@example.com'), 'a@b.com');
    await userEvent.type(screen.getByPlaceholderText('••••••••'), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    const slot = document.querySelector('.auth-error-slot');
    await waitFor(() => expect(slot).toHaveTextContent('Bad creds'));
  });
});
