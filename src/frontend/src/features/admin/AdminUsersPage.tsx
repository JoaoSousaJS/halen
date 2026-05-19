import { useState, useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { listUsers } from '../../shared/api/admin';
import type { AdminUserDto } from '../../shared/api/admin';
import KycReviewPage from './KycReviewPage';
import { Button, Chip, SearchFilterBar } from '../../shared/components';
import type { Filter } from '../../shared/components';

type RoleFilter = 'all' | 'patient' | 'doctor' | 'flagged';

const PAGE_SIZE = 25;

function statusVariant(status: string): 'good' | 'danger' | 'warn' | undefined {
  switch (status) {
    case 'Active': return 'good';
    case 'PendingReview': return 'warn';
    case 'Suspended': return 'danger';
    default: return undefined;
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
  const [reviewingDoctorId, setReviewingDoctorId] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<AdminUserDto | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout>>(undefined);

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

  if (reviewingDoctorId) {
    return <KycReviewPage doctorProfileId={reviewingDoctorId} onBack={() => setReviewingDoctorId(null)} />;
  }

  if (selectedUser) {
    return (
      <UserDetailPanel
        user={selectedUser}
        onBack={() => setSelectedUser(null)}
        onReviewKyc={(doctorProfileId) => {
          setSelectedUser(null);
          setReviewingDoctorId(doctorProfileId);
        }}
      />
    );
  }

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

      <SearchFilterBar
        searchPlaceholder="Search by name or email…"
        searchValue={search}
        onSearchChange={setSearch}
        filters={[
          {
            type: 'dropdown',
            key: 'role',
            label: 'All roles',
            options: filters.map((f) => ({ value: f.key, label: f.label })),
            value: filter === 'all' ? '' : filter,
            onChange: (v) => changeFilter((v || 'all') as RoleFilter),
          } satisfies Filter,
        ]}
        resultCount={totalCount}
        resultLabel="users"
      />

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
                <th>Email</th>
                <th>Role</th>
                <th>Plan</th>
                <th>Status</th>
                <th>Last seen</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users.map((u: AdminUserDto) => (
                <tr key={u.id} className="admin-table-row" onClick={() => setSelectedUser(u)}>
                  <td>
                    <div className="admin-user-cell">
                      <div className="admin-avatar">{initials(u.name)}</div>
                      <span className="admin-user-name">{u.name}</span>
                    </div>
                  </td>
                  <td className="text-dim">{u.email ?? '—'}</td>
                  <td>{u.role}</td>
                  <td className="text-dim">{u.plan ?? '—'}</td>
                  <td>
                    <Chip status={statusLabel(u.status, u.role)} variant={statusVariant(u.status)} />
                  </td>
                  <td className="admin-mono text-dim">{timeAgo(u.lastLoginAt)}</td>
                  <td>
                    {u.role === 'Doctor' && u.status === 'PendingReview' && u.doctorProfileId ? (
                      <Button
                        size="sm"
                        variant="primary"
                        onClick={(e) => {
                          e.stopPropagation();
                          setReviewingDoctorId(u.doctorProfileId!);
                        }}
                      >
                        Review
                      </Button>
                    ) : u.isFlagged ? (
                      <Button size="sm" variant="danger" disabled>Review</Button>
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
          <Button
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            ← Prev
          </Button>
          <span className="text-dim">
            Page {page} of {totalPages}
          </span>
          <Button
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Next →
          </Button>
        </div>
      ) : null}
    </>
  );
}

function UserDetailPanel({
  user,
  onBack,
  onReviewKyc,
}: {
  user: AdminUserDto;
  onBack: () => void;
  onReviewKyc: (doctorProfileId: string) => void;
}) {
  return (
    <section className="user-detail">
      <Button size="sm" className="kyc-review-back" onClick={onBack}>&larr; Back</Button>

      <div className="user-detail-header">
        <div className="user-detail-avatar">{initials(user.name)}</div>
        <div>
          <h2 className="section-heading">{user.name}</h2>
          <p className="text-dim">{user.email ?? user.role} · Last seen {timeAgo(user.lastLoginAt)}</p>
        </div>
        <Chip
          status={statusLabel(user.status, user.role)}
          variant={statusVariant(user.status)}
        />
      </div>

      <div className="user-detail-grid">
        <div className="user-detail-card">
          <h3>Account</h3>
          <dl className="user-detail-dl">
            <dt>Email</dt>
            <dd>{user.email ?? '—'}</dd>
            <dt>Role</dt>
            <dd>{user.role}</dd>
            <dt>Status</dt>
            <dd>{statusLabel(user.status, user.role)}</dd>
            <dt>Plan</dt>
            <dd>{user.plan ?? 'None'}</dd>
            <dt>Last seen</dt>
            <dd>{timeAgo(user.lastLoginAt)}</dd>
          </dl>
        </div>

        {user.role === 'Doctor' && user.status === 'PendingReview' && user.doctorProfileId && (
          <div className="user-detail-card">
            <h3>KYC Verification</h3>
            <p className="text-dim" style={{ marginBottom: 12 }}>
              This doctor has submitted documents for review.
            </p>
            <Button
              variant="primary"
              onClick={() => onReviewKyc(user.doctorProfileId!)}
            >
              Review KYC documents
            </Button>
          </div>
        )}
      </div>
    </section>
  );
}
