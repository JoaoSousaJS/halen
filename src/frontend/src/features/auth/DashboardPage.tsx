import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../shared/components/AuthProvider';

export default function DashboardPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

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
        <p style={{ color: 'var(--text-dim)', marginBottom: 24 }}>
          Full dashboard coming soon. Auth is working!
        </p>
        <button className="btn btn-primary btn-block" onClick={handleLogout}>
          Sign out
        </button>
      </div>
    </div>
  );
}
