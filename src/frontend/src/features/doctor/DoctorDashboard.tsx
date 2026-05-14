import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../shared/components/AuthProvider';
import { getApiError } from '../../shared/api/errors';
import {
  getMyAppointments,
  cancelAppointment,
  completeAppointment,
} from '../../shared/api/appointments';

export default function DoctorDashboard() {
  const { user, logout } = useAuth();
  const queryClient = useQueryClient();

  const [completingId, setCompletingId] = useState<string | null>(null);
  const [notesMap, setNotesMap] = useState<Record<string, string>>({});
  const [cancellingId, setCancellingId] = useState<string | null>(null);

  const appointments = useQuery({ queryKey: ['my-appointments'], queryFn: getMyAppointments });

  const cancel = useMutation({
    mutationFn: cancelAppointment,
    onSuccess: () => {
      setCancellingId(null);
      queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
    },
    onError: () => setCancellingId(null),
  });

  const complete = useMutation({
    mutationFn: (id: string) => completeAppointment(id, notesMap[id] || undefined),
    onSuccess: (_data, id) => {
      setCompletingId(null);
      setNotesMap((prev) => { const next = { ...prev }; delete next[id]; return next; });
      queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
    },
  });

  return (
    <div className="dashboard-shell">
      <header className="dashboard-header">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">Doctor portal</div>
          </div>
        </div>
        <span className="dashboard-user">Dr. {user?.family_name}</span>
        <button className="btn btn-sm" onClick={logout}>Sign out</button>
      </header>

      <main className="dashboard-main">
        <h1 className="auth-heading">
          Your<br /><em>schedule.</em>
        </h1>

        {appointments.isLoading ? <p className="text-dim">Loading…</p> : null}

        {appointments.data?.length === 0 ? (
          <p className="text-dim" style={{ marginTop: 16 }}>No appointments yet.</p>
        ) : null}

        <div className="appt-list" style={{ marginTop: 20 }}>
          {appointments.data?.map((a) => (
            <div key={a.id} className="appt-card">
              <div className="appt-card-header">
                <span className={`appt-status appt-status--${a.status.toLowerCase()}`}>
                  {a.status}
                </span>
                <span className="appt-date">
                  {new Date(a.scheduledAt).toLocaleString()}
                </span>
              </div>
              <div className="appt-card-body">
                <strong>{a.patientName}</strong>
                <p>{a.reason}</p>
                {a.notes ? <p className="text-dim">Notes: {a.notes}</p> : null}
              </div>

              {a.status === 'Scheduled' ? (
                <div className="appt-actions">
                  {completingId === a.id ? (
                    <div className="appt-complete-form">
                      <textarea
                        rows={2}
                        value={notesMap[a.id] ?? ''}
                        onChange={(e) => setNotesMap((prev) => ({ ...prev, [a.id]: e.target.value }))}
                        placeholder="Session notes (optional)…"
                      />
                      <div className="appt-complete-buttons">
                        <button
                          className="btn btn-primary btn-sm"
                          disabled={complete.isPending}
                          onClick={() => complete.mutate(a.id)}
                        >
                          {complete.isPending ? 'Saving…' : 'Confirm'}
                        </button>
                        <button
                          className="btn btn-sm"
                          onClick={() => setCompletingId(null)}
                        >
                          Back
                        </button>
                      </div>
                    </div>
                  ) : (
                    <>
                      <button
                        className="btn btn-primary btn-sm"
                        aria-label={`Complete appointment with ${a.patientName}`}
                        onClick={() => setCompletingId(a.id)}
                      >
                        Complete
                      </button>
                      <button
                        className="btn btn-danger btn-sm"
                        aria-label={`Cancel appointment with ${a.patientName}`}
                        disabled={cancel.isPending}
                        onClick={() => {
                          cancel.reset();
                          setCancellingId(a.id);
                          cancel.mutate(a.id);
                        }}
                      >
                        {cancel.isPending && cancellingId === a.id ? 'Cancelling…' : 'Cancel'}
                      </button>
                    </>
                  )}
                </div>
              ) : null}
            </div>
          ))}
        </div>

        {cancel.isError ? (
          <p className="auth-error" style={{ marginTop: 8 }}>{getApiError(cancel.error)}</p>
        ) : null}
        {complete.isError ? (
          <p className="auth-error" style={{ marginTop: 8 }}>{getApiError(complete.error)}</p>
        ) : null}
      </main>
    </div>
  );
}
