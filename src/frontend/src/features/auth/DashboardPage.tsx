import { lazy, Suspense } from 'react';
import { useAuth } from '../../shared/components/AuthProvider';

const AdminDashboard = lazy(() => import('../admin/AdminDashboard'));
const DoctorDashboard = lazy(() => import('../doctor/DoctorDashboard'));
const PatientDashboard = lazy(() => import('../patient/PatientDashboard'));

export default function DashboardPage() {
  const { user } = useAuth();

  return (
    <Suspense fallback={<div className="text-dim">Loading…</div>}>
      {user?.role === 'Admin' ? <AdminDashboard /> :
       user?.role === 'Doctor' ? <DoctorDashboard /> :
       <PatientDashboard />}
    </Suspense>
  );
}
