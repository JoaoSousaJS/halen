import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../shared/components/AuthProvider';
import AuditLogPage from './AuditLogPage';
import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockLogs = [
  {
    id: 'log-1',
    timestamp: '2026-05-19T10:00:00Z',
    actorId: 'user-1',
    actorName: 'Jane Doe',
    action: 'BookAppointment',
    targetId: 'apt-1',
    metadata: '{"DoctorId":"doc-1","Reason":"Checkup"}',
    ipAddress: '192.168.1.1',
  },
  {
    id: 'log-2',
    timestamp: '2026-05-19T09:00:00Z',
    actorId: 'user-2',
    actorName: 'Admin User',
    action: 'CreateDoctor',
    targetId: 'doc-2',
    metadata: null,
    ipAddress: '10.0.0.1',
  },
];

const mockSearchAuditLogs = vi.fn().mockResolvedValue({ logs: mockLogs, totalCount: mockLogs.length });
const mockExportAuditLogsCsv = vi.fn().mockResolvedValue(new Blob(['csv-data'], { type: 'text/csv' }));

vi.mock('../../shared/api/audit-logs', () => ({
  searchAuditLogs: (...args: unknown[]) => mockSearchAuditLogs(...args),
  exportAuditLogsCsv: (...args: unknown[]) => mockExportAuditLogsCsv(...args),
}));

function fakeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({
    sub: '1', email: 'admin@test.com', given_name: 'Admin', family_name: 'User', role: 'PlatformAdmin', exp: 9999999999,
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
          <AuditLogPage />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AuditLogPage', () => {
  beforeEach(() => {
    localStorage.clear();
    mockSearchAuditLogs.mockResolvedValue({ logs: mockLogs, totalCount: mockLogs.length });
  });

  it('renders heading', async () => {
    renderPage();
    expect(screen.getByText('Audit Trail')).toBeDefined();
  });

  it('renders log rows after loading', async () => {
    renderPage();
    expect(await screen.findByText('Jane Doe')).toBeDefined();
    expect(screen.getByText('BookAppointment')).toBeDefined();
    expect(screen.getByText('Admin User')).toBeDefined();
    expect(screen.getByText('CreateDoctor')).toBeDefined();
  });

  it('shows empty state when no logs', async () => {
    mockSearchAuditLogs.mockResolvedValue({ logs: [], totalCount: 0 });
    renderPage();
    expect(await screen.findByText(/no audit logs/i)).toBeDefined();
  });

  it('expands row to show metadata on click', async () => {
    renderPage();
    const row = await screen.findByText('BookAppointment');
    fireEvent.click(row);
    await waitFor(() => {
      expect(screen.getByText(/DoctorId/)).toBeDefined();
    });
  });

  it('calls searchAuditLogs with filter params', async () => {
    renderPage();
    await screen.findByText('Jane Doe');
    expect(mockSearchAuditLogs).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 50 }),
    );
  });

  it('triggers CSV export', async () => {
    renderPage();
    await screen.findByText('Jane Doe');
    const exportBtn = screen.getByRole('button', { name: /export/i });
    fireEvent.click(exportBtn);
    await waitFor(() => {
      expect(mockExportAuditLogsCsv).toHaveBeenCalled();
    });
  });
});
