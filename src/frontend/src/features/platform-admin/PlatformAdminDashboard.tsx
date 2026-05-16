import { useState } from 'react';
import { useAuth } from '../../shared/components/AuthProvider';
import ClinicsPage from './ClinicsPage';
import ClinicDetailPage from './ClinicDetailPage';

type View = { page: 'list' } | { page: 'detail'; clinicId: string };

export default function PlatformAdminDashboard() {
  const { user, logout } = useAuth();
  const [view, setView] = useState<View>({ page: 'list' });

  return (
    <div className="dashboard-shell">
      <header className="dashboard-header">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">Platform Admin · {user?.given_name}</div>
          </div>
        </div>

        <nav className="admin-nav">
          <button
            className={`admin-nav-btn${view.page === 'list' ? ' active' : ''}`}
            onClick={() => setView({ page: 'list' })}
          >
            Clinics
          </button>
        </nav>

        <span className="dashboard-user">{user?.given_name} {user?.family_name}</span>
        <button className="btn btn-sm" onClick={logout}>Sign out</button>
      </header>

      <main className="dashboard-main dashboard-main--wide">
        {view.page === 'list' && (
          <ClinicsPage onSelectClinic={(id) => setView({ page: 'detail', clinicId: id })} />
        )}
        {view.page === 'detail' && (
          <ClinicDetailPage
            clinicId={view.clinicId}
            onBack={() => setView({ page: 'list' })}
          />
        )}
      </main>
    </div>
  );
}
