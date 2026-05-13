import { useAuth } from '../../shared/components/AuthProvider';

export default function DoctorDashboard() {
  const { user, logout } = useAuth();

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">Doctor portal</div>
          </div>
        </div>
        <h1 className="auth-heading">
          Dr. {user?.family_name},<br /><em>good to see you.</em>
        </h1>
        <p style={{ color: 'var(--text-dim)' }}>
          Your appointment schedule is coming soon.
        </p>
        <button className="btn btn-primary btn-block" onClick={logout} style={{ marginTop: 8 }}>
          Sign out
        </button>
      </div>
    </div>
  );
}
