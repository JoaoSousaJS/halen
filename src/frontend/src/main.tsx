import { StrictMode, Suspense, lazy } from 'react';
import type { ReactNode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './shared/components/AuthProvider';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import DashboardPage from './features/auth/DashboardPage';
import './index.css';

const ProfilePage = lazy(() => import('./features/profile/ProfilePage'));
const ConsultationPage = lazy(() => import('./features/video-consultation/ConsultationPage'));
const MedicalRecordsPage = lazy(() => import('./features/patient/medical-records/MedicalRecordsPage'));
const MessagingPage = lazy(() => import('./features/messaging/MessagingPage'));

function PrivateRoute({ children }: { children: ReactNode }) {
  const { token } = useAuth();
  return token ? <>{children}</> : <Navigate to="/login" replace />;
}

function RootRedirect() {
  const { token } = useAuth();
  return <Navigate to={token ? '/dashboard' : '/login'} replace />;
}

function Router() {
  return (
    <Routes>
      <Route path="/" element={<RootRedirect />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route
        path="/dashboard"
        element={
          <PrivateRoute>
            <DashboardPage />
          </PrivateRoute>
        }
      />
      <Route
        path="/profile"
        element={
          <PrivateRoute>
            <Suspense fallback={<div className="text-dim">Loading…</div>}>
              <ProfilePage />
            </Suspense>
          </PrivateRoute>
        }
      />
      <Route
        path="/consultation/:appointmentId"
        element={
          <PrivateRoute>
            <Suspense fallback={<div className="text-dim">Connecting…</div>}>
              <ConsultationPage />
            </Suspense>
          </PrivateRoute>
        }
      />
      <Route
        path="/medical-records/:patientProfileId"
        element={
          <PrivateRoute>
            <Suspense fallback={<div className="text-dim">Loading…</div>}>
              <MedicalRecordsPage />
            </Suspense>
          </PrivateRoute>
        }
      />
      <Route
        path="/messages"
        element={
          <PrivateRoute>
            <Suspense fallback={<div className="text-dim">Loading…</div>}>
              <MessagingPage />
            </Suspense>
          </PrivateRoute>
        }
      />
      <Route path="*" element={<RootRedirect />} />
    </Routes>
  );
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 5 * 60_000,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <Router />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>
);
