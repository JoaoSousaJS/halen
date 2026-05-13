import { useAuth } from '../../shared/components/AuthProvider';

export default function PatientDashboard() {
  const { user, logout } = useAuth();

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">care · on call</div>
          </div>
        </div>
        <h1 className="auth-heading">
          Welcome,<br /><em>{user?.given_name ?? 'there'}.</em>
        </h1>
        <p style={{ color: 'var(--text-dim)' }}>
          Book and manage your appointments here — coming soon.
        </p>
        <button className="btn btn-primary btn-block" onClick={logout} style={{ marginTop: 8 }}>
          Sign out
        </button>
      </div>
    </div>
  );
}
