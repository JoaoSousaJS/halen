import { useAuth } from '../../shared/components/AuthProvider';
import AdminDashboard from '../admin/AdminDashboard';
import DoctorDashboard from '../doctor/DoctorDashboard';
import PatientDashboard from '../patient/PatientDashboard';

export default function DashboardPage() {
  const { user } = useAuth();

  if (user?.role === 'Admin') return <AdminDashboard />;
  if (user?.role === 'Doctor') return <DoctorDashboard />;
  return <PatientDashboard />;
}
