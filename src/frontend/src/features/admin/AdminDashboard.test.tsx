import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import AdminDashboard from './AdminDashboard';
import { vi, describe, it, expect, beforeEach } from 'vitest';

vi.mock('../../shared/api/admin', () => ({
  createDoctor: vi.fn().mockResolvedValue({ doctorId: 'd-001' }),
  listUsers: vi.fn().mockResolvedValue({ users: [], totalCount: 0 }),
}));

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '1', email: 'admin@test.com', given_name: 'Lior', family_name: 'Adler', role: 'ClinicAdmin', clinic_id: 'c-001', exp: 9999999999,
  }));
  return `${header}.${body}.fake`;
}

function renderDashboard() {
  localStorage.setItem('token', fakeJwt());
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <AuthProvider>
          <AdminDashboard />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AdminDashboard', () => {
  beforeEach(() => localStorage.clear());

  it('renders header with brand and admin name', () => {
    renderDashboard();
    expect(screen.getByText('Halen')).toBeDefined();
    expect(screen.getByText('Clinic Admin · Lior')).toBeDefined();
  });

  it('shows Users tab by default', () => {
    renderDashboard();
    expect(screen.getByText('Users.')).toBeDefined();
  });

  it('switches to Create doctor tab', () => {
    renderDashboard();
    fireEvent.click(screen.getByText('Create doctor'));
    expect(screen.getByText('doctor account.')).toBeDefined();
  });

  it('switches back to Users tab', () => {
    renderDashboard();
    fireEvent.click(screen.getByText('Create doctor'));
    fireEvent.click(screen.getByText('Users'));
    expect(screen.getByText('Users.')).toBeDefined();
  });

  it('renders sign out button', () => {
    renderDashboard();
    expect(screen.getByText('Sign out')).toBeDefined();
  });
});
