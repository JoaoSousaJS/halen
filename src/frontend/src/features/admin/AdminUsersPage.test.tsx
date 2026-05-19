import { render, screen, fireEvent, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import AdminUsersPage from './AdminUsersPage';
import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockUsers = [
  { id: 'u-1', name: 'Maya Chen', role: 'Patient', status: 'Active', plan: 'HALEN+', lastLoginAt: new Date().toISOString(), isFlagged: false, doctorProfileId: null },
  { id: 'u-2', name: 'Dr. House', role: 'Doctor', status: 'PendingReview', plan: null, lastLoginAt: null, isFlagged: true, doctorProfileId: 'dp-1' },
];

const mockListUsers = vi.fn().mockResolvedValue({ users: mockUsers, totalCount: mockUsers.length });

vi.mock('../../shared/api/admin', () => ({
  listUsers: (...args: unknown[]) => mockListUsers(...args),
  createDoctor: vi.fn(),
}));

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '1', email: 'admin@test.com', given_name: 'Admin', family_name: 'User', role: 'Admin', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

function renderPage() {
  localStorage.setItem('token', fakeJwt());
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <AuthProvider>
          <AdminUsersPage />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AdminUsersPage', () => {
  beforeEach(() => {
    localStorage.clear();
    mockListUsers.mockResolvedValue({ users: mockUsers, totalCount: mockUsers.length });
  });

  it('renders heading and role filter dropdown', async () => {
    renderPage();
    expect(screen.getByText('Users.')).toBeDefined();
    expect(screen.getByRole('button', { name: /all roles/i })).toBeDefined();
  });

  it('renders user rows after loading', async () => {
    renderPage();
    expect(await screen.findByText('Maya Chen')).toBeDefined();
    expect(screen.getByText('Dr. House')).toBeDefined();
  });

  it('shows status chip with correct label for doctor PendingReview', async () => {
    renderPage();
    expect(await screen.findByText('Pending KYC')).toBeDefined();
  });

  it('shows Review button for flagged users', async () => {
    renderPage();
    expect(await screen.findByText('Review')).toBeDefined();
  });

  it('renders search input', () => {
    renderPage();
    expect(screen.getByPlaceholderText('Search by name or email…')).toBeDefined();
  });

  it('updates search input value on type', async () => {
    renderPage();
    const input = screen.getByPlaceholderText('Search by name or email…') as HTMLInputElement;
    await act(() => fireEvent.change(input, { target: { value: 'maya' } }));
    expect(input.value).toBe('maya');
  });

  it('shows error message when API fails', async () => {
    mockListUsers.mockRejectedValue(new Error('Server error'));
    renderPage();
    expect(await screen.findByText('Failed to load users. Please try again.')).toBeDefined();
  });
});
