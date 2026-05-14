import { useState } from 'react';
import { useAuth } from '../../shared/components/AuthProvider';
import CreateDoctorForm from './CreateDoctorForm';
import AdminUsersPage from './AdminUsersPage';

type AdminTab = 'create-doctor' | 'users';

export default function AdminDashboard() {
  const { user, logout } = useAuth();
  const [tab, setTab] = useState<AdminTab>('users');

  return (
    <div className="dashboard-shell">
      <header className="dashboard-header">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">Admin · {user?.given_name}</div>
          </div>
        </div>

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
        </nav>

        <span className="dashboard-user">{user?.given_name} {user?.family_name}</span>
        <button className="btn btn-sm" onClick={logout}>Sign out</button>
      </header>

      <main className="dashboard-main dashboard-main--wide">
        {tab === 'users' && <AdminUsersPage />}
        {tab === 'create-doctor' && <CreateDoctorForm />}
      </main>
    </div>
  );
}
