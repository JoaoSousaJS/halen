import { useState } from 'react';
import { useAuth } from '../../shared/components/AuthProvider';
import { DashboardShell } from '../../shared/components/DashboardShell';
import { useQueryClient } from '@tanstack/react-query';
import CreateDoctorForm from './CreateDoctorForm';
import AdminUsersPage from './AdminUsersPage';
import CreateUserDialog from './CreateUserDialog';

type AdminTab = 'create-doctor' | 'users';

export default function AdminDashboard() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<AdminTab>('users');
  const [showCreateUser, setShowCreateUser] = useState(false);

  const nav = (
    <nav className="admin-nav">
      <button
        className={`admin-nav-btn${tab === 'users' ? ' active' : ''}`}
        onClick={() => setTab('users')}
      >
        Users
      </button>
      <button
        className={`admin-nav-btn${tab === 'create-doctor' ? ' active' : ''}`}
        onClick={() => setTab('create-doctor')}
      >
        Create doctor
      </button>
      <button
        className="admin-nav-btn"
        onClick={() => setShowCreateUser(true)}
      >
        + Create user
      </button>
    </nav>
  );

  return (
    <DashboardShell
      subtitle={`Clinic Admin · ${user?.given_name}`}
      userName={`${user?.given_name} ${user?.family_name}`}
      nav={nav}
      wide
    >
      {tab === 'users' && <AdminUsersPage />}
      {tab === 'create-doctor' && <CreateDoctorForm />}

      {showCreateUser && (
        <CreateUserDialog
          onClose={() => setShowCreateUser(false)}
          onCreated={() => {
            setShowCreateUser(false);
            queryClient.invalidateQueries({ queryKey: ['admin-users'] });
          }}
        />
      )}
    </DashboardShell>
  );
}
