import { useState } from 'react';
import { useAuth } from '../../shared/components/AuthProvider';
import { DashboardShell } from '../../shared/components/DashboardShell';
import ClinicsPage from './ClinicsPage';
import ClinicDetailPage from './ClinicDetailPage';

type View = { page: 'list' } | { page: 'detail'; clinicId: string };

export default function PlatformAdminDashboard() {
  const { user } = useAuth();
  const [view, setView] = useState<View>({ page: 'list' });

  const nav = (
    <nav className="admin-nav">
      <button
        className={`admin-nav-btn${view.page === 'list' ? ' active' : ''}`}
        onClick={() => setView({ page: 'list' })}
      >
        Clinics
      </button>
    </nav>
  );

  return (
    <DashboardShell
      subtitle={`Platform Admin · ${user?.given_name}`}
      userName={`${user?.given_name} ${user?.family_name}`}
      nav={nav}
      wide
    >
      {view.page === 'list' && (
        <ClinicsPage onSelectClinic={(id) => setView({ page: 'detail', clinicId: id })} />
      )}
      {view.page === 'detail' && (
        <ClinicDetailPage
          clinicId={view.clinicId}
          onBack={() => setView({ page: 'list' })}
        />
      )}
    </DashboardShell>
  );
}
