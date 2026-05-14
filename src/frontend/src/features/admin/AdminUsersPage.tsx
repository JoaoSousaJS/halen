import { useState, useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { listUsers } from '../../shared/api/admin';
import type { AdminUserDto } from '../../shared/api/admin';

type RoleFilter = 'all' | 'patient' | 'doctor' | 'flagged';

const PAGE_SIZE = 25;

function statusClass(status: string): string {
  switch (status) {
    case 'Active': return 'chip-good';
    case 'PendingReview': return 'chip-warn';
    case 'Suspended': return 'chip-danger';
    case 'Idle': return '';
    default: return '';
  }
}

function statusLabel(status: string, role: string): string {
  if (status === 'PendingReview') {
    return role === 'Doctor' ? 'Pending KYC' : 'Pending review';
  }
  return status;
}

function initials(name: string): string {
  return name.split(' ').map((s) => s[0]).join('').slice(0, 2).toUpperCase();
}

function timeAgo(dateStr: string | null): string {
  if (!dateStr) return '—';
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return 'now';
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export default function AdminUsersPage() {
  const [filter, setFilter] = useState<RoleFilter>('all');
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [page, setPage] = useState(1);
  const timerRef = useRef<ReturnType<typeof setTimeout>>();

  useEffect(() => {
    clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);
    return () => clearTimeout(timerRef.current);
  }, [search]);

  function changeFilter(f: RoleFilter) {
    setFilter(f);
    setPage(1);
  }

  const queryParams = {
    role: filter === 'all' || filter === 'flagged' ? undefined : filter,
    search: debouncedSearch || undefined,
    flaggedOnly: filter === 'flagged' ? true : undefined,
    page,
    pageSize: PAGE_SIZE,
  };

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-users', queryParams],
    queryFn: () => listUsers(queryParams),
  });

  const users = data?.users;
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  const flaggedCount = useQuery({
    queryKey: ['admin-users-flagged-count'],
    queryFn: () => listUsers({ flaggedOnly: true, pageSize: 1 }),
    select: (d) => d.totalCount,
    staleTime: 30_000,
  });

  const filters: { key: RoleFilter; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'patient', label: 'Patient' },
    { key: 'doctor', label: 'Doctor' },
    { key: 'flagged', label: `Needs review${flaggedCount.data ? ` · ${flaggedCount.data}` : ''}` },
  ];

  return (
    <>
      <div className="admin-page-head">
        <div>
          <div className="admin-tag">User management</div>
          <h1 className="auth-heading">Users.</h1>
          <p className="text-dim" style={{ marginTop: 4 }}>
            Search, filter, and manage all platform accounts.
          </p>
        </div>
      </div>

      <div className="admin-toolbar">
        <div className="admin-pill-tab" role="tablist">
          {filters.map((f) => (
            <button
              key={f.key}
              role="tab"
              aria-selected={filter === f.key}
              className={filter === f.key ? 'on' : ''}
              onClick={() => changeFilter(f.key)}
            >
              {f.label}
            </button>
          ))}
        </div>
        <input
          className="admin-search"
          type="text"
          placeholder="Search by name or email…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {isLoading ? <p className="text-dim">Loading…</p> : null}

      {isError ? (
        <p className="auth-error" style={{ marginTop: 16 }}>Failed to load users. Please try again.</p>
      ) : null}

      {users && users.length === 0 ? (
        <p className="text-dim" style={{ marginTop: 16 }}>No users match the current filters.</p>
      ) : null}

      {users && users.length > 0 ? (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>User</th>
                <th>ID</th>
                <th>Role</th>
                <th>Plan</th>
                <th>Status</th>
                <th>Last seen</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users.map((u: AdminUserDto) => (
                <tr key={u.id}>
                  <td>
                    <div className="admin-user-cell">
                      <div className="admin-avatar">{initials(u.name)}</div>
                      <span className="admin-user-name">{u.name}</span>
                    </div>
                  </td>
                  <td className="admin-mono">{u.id.slice(0, 8)}</td>
                  <td>{u.role}</td>
                  <td className="text-dim">{u.plan ?? '—'}</td>
                  <td>
                    <span className={`chip ${statusClass(u.status)}`}>
                      {statusLabel(u.status, u.role)}
                    </span>
                  </td>
                  <td className="admin-mono text-dim">{timeAgo(u.lastLoginAt)}</td>
                  <td>
                    {u.isFlagged ? (
                      <button className="btn btn-sm btn-danger" disabled>Review</button>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {totalPages > 1 ? (
        <div className="admin-pagination">
          <button
            className="btn btn-sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            ← Prev
          </button>
          <span className="text-dim">
            Page {page} of {totalPages}
          </span>
          <button
            className="btn btn-sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Next →
          </button>
        </div>
      ) : null}
    </>
  );
}
