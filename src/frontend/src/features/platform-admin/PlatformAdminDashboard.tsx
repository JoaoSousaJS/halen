import { Suspense, lazy, useState } from 'react';
import { useAuth } from '../../shared/components/AuthProvider';
import { DashboardShell } from '../../shared/components/DashboardShell';
import ClinicsPage from './ClinicsPage';
import ClinicDetailPage from './ClinicDetailPage';
import type { AnalyticsView } from './analytics/AnalyticsPage';

const AnalyticsPage = lazy(() => import('./analytics/AnalyticsPage'));

type View =
  | { page: 'list' }
  | { page: 'detail'; clinicId: string }
  | { page: 'analytics'; sub: AnalyticsView };

export default function PlatformAdminDashboard() {
  const { user } = useAuth();
  const [view, setView] = useState<View>({ page: 'list' });

  const nav = (
    <nav className="admin-nav">
      <button
        className={`admin-nav-btn${view.page === 'list' || view.page === 'detail' ? ' active' : ''}`}
        onClick={() => setView({ page: 'list' })}
      >
        Clinics
      </button>
      <button
        className={`admin-nav-btn${view.page === 'analytics' ? ' active' : ''}`}
        onClick={() => setView({ page: 'analytics', sub: 'overview' })}
      >
        Analytics
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
      {view.page === 'analytics' && (
        <Suspense fallback={<div className="analytics-loading">Loading analytics...</div>}>
          <AnalyticsPage
            activeView={view.sub}
            onNavigate={(sub) => setView({ page: 'analytics', sub })}
          />
        </Suspense>
      )}
    </DashboardShell>
  );
}
