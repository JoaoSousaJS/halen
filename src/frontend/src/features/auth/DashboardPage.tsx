import { lazy, Suspense } from 'react';
import { useAuth } from '../../shared/components/AuthProvider';

const PlatformAdminDashboard = lazy(() => import('../platform-admin/PlatformAdminDashboard'));
const AdminDashboard = lazy(() => import('../admin/AdminDashboard'));
const DoctorDashboard = lazy(() => import('../doctor/DoctorDashboard'));
const PatientDashboard = lazy(() => import('../patient/PatientDashboard'));

export default function DashboardPage() {
  const { user } = useAuth();

  return (
    <Suspense fallback={<div className="text-dim">Loading…</div>}>
      {user?.role === 'PlatformAdmin' ? <PlatformAdminDashboard /> :
       user?.role === 'ClinicAdmin' ? <AdminDashboard /> :
       user?.role === 'Doctor' ? <DoctorDashboard /> :
       <PatientDashboard />}
    </Suspense>
  );
}
