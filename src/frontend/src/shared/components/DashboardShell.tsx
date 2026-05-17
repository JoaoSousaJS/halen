import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from './AuthProvider';

interface DashboardShellProps {
  subtitle: string;
  userName: string;
  nav?: ReactNode;
  children: ReactNode;
  wide?: boolean;
}

export function DashboardShell({ subtitle, userName, nav, children, wide }: DashboardShellProps) {
  const { logout } = useAuth();

  return (
    <div className="dashboard-shell">
      <header className="dashboard-header">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">{subtitle}</div>
          </div>
        </div>

        {nav}

        <Link to="/profile" className="dashboard-user">{userName}</Link>
        <button className="btn btn-sm" onClick={logout}>Sign out</button>
      </header>

      <main className={`dashboard-main${wide ? ' dashboard-main--wide' : ''}`}>
        {children}
      </main>
    </div>
  );
}
