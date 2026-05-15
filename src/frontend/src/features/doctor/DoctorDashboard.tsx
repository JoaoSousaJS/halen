import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../shared/components/AuthProvider';
import { getApiError } from '../../shared/api/errors';
import {
  getMyAppointments,
  cancelAppointment,
  completeAppointment,
} from '../../shared/api/appointments';
import type { AppointmentDto } from '../../shared/api/appointments';
import {
  getMyPrescriptions,
  issuePrescription,
  cancelPrescription,
} from '../../shared/api/prescriptions';
import type { IssuePrescriptionPayload } from '../../shared/api/prescriptions';
import { useNotifications } from '../../shared/hooks/useNotifications';
import { ToastContainer } from '../../shared/components/ToastContainer';
import { getKycStatus } from '../../shared/api/doctor';
import KycSetup from './KycSetup';

export default function DoctorDashboard() {
  const { user, logout } = useAuth();
  const queryClient = useQueryClient();
  const { toasts, dismissToast } = useNotifications();
  const kyc = useQuery({ queryKey: ['kyc-status'], queryFn: getKycStatus });

  const [completingId, setCompletingId] = useState<string | null>(null);
  const [notesMap, setNotesMap] = useState<Record<string, string>>({});
  const [cancellingId, setCancellingId] = useState<string | null>(null);

  const [rxForm, setRxForm] = useState({ patientId: '', drugName: '', dosage: '', frequency: '', refillsRemaining: 0, pharmacyName: '' });
  const [rxSuccess, setRxSuccess] = useState('');

  const appointments = useQuery({ queryKey: ['my-appointments'], queryFn: getMyAppointments });
  const prescriptions = useQuery({ queryKey: ['my-prescriptions'], queryFn: getMyPrescriptions });

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

  const issue = useMutation({
    mutationFn: (payload: IssuePrescriptionPayload) => issuePrescription(payload),
    onSuccess: () => {
      setRxForm({ patientId: '', drugName: '', dosage: '', frequency: '', refillsRemaining: 0, pharmacyName: '' });
      setRxSuccess('Prescription issued!');
      setTimeout(() => setRxSuccess(''), 4000);
      queryClient.invalidateQueries({ queryKey: ['my-prescriptions'] });
    },
  });

  const cancelRx = useMutation({
    mutationFn: cancelPrescription,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-prescriptions'] }),
  });

  const uniquePatients = appointments.data
    ?.reduce<{ id: string; name: string }[]>((acc, a) => {
      if (!acc.some((p) => p.id === a.patientId)) {
        acc.push({ id: a.patientId, name: a.patientName });
      }
      return acc;
    }, []) ?? [];

  function handleIssue(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setRxSuccess('');
    issue.mutate({
      patientId: rxForm.patientId,
      drugName: rxForm.drugName,
      dosage: rxForm.dosage,
      frequency: rxForm.frequency,
      refillsRemaining: rxForm.refillsRemaining,
      pharmacyName: rxForm.pharmacyName || undefined,
    });
  }

  const kycApproved = kyc.data?.status === 'Approved';

  return (
    <div className="dashboard-shell">
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />
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
        {!kycApproved ? (
          <>
            <h1 className="auth-heading">
              Welcome,<br /><em>Dr. {user?.family_name}.</em>
            </h1>
            <KycSetup />
          </>
        ) : (
        <>
        <h1 className="auth-heading">
          Your<br /><em>schedule.</em>
        </h1>

        {appointments.isLoading ? <p className="text-dim">Loading…</p> : null}
        {appointments.isError ? <p className="auth-error">Failed to load appointments.</p> : null}

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

        <section style={{ marginTop: 48 }}>
          <h2 className="section-heading">Issue a prescription</h2>

          <div className="auth-card" style={{ marginTop: 20 }}>
            <form onSubmit={handleIssue} className="auth-form">
              <label className="field">
                <span>Patient</span>
                <select
                  required
                  value={rxForm.patientId}
                  onChange={(e) => setRxForm((f) => ({ ...f, patientId: e.target.value }))}
                >
                  <option value="">Select a patient…</option>
                  {uniquePatients.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>Drug name</span>
                <input
                  type="text"
                  required
                  value={rxForm.drugName}
                  onChange={(e) => setRxForm((f) => ({ ...f, drugName: e.target.value }))}
                  placeholder="e.g. Amoxicillin"
                />
              </label>

              <label className="field">
                <span>Dosage</span>
                <input
                  type="text"
                  required
                  value={rxForm.dosage}
                  onChange={(e) => setRxForm((f) => ({ ...f, dosage: e.target.value }))}
                  placeholder="e.g. 500mg"
                />
              </label>

              <label className="field">
                <span>Frequency</span>
                <input
                  type="text"
                  required
                  value={rxForm.frequency}
                  onChange={(e) => setRxForm((f) => ({ ...f, frequency: e.target.value }))}
                  placeholder="e.g. Twice daily"
                />
              </label>

              <label className="field">
                <span>Refills</span>
                <input
                  type="number"
                  required
                  min={0}
                  max={24}
                  value={rxForm.refillsRemaining}
                  onChange={(e) => setRxForm((f) => ({ ...f, refillsRemaining: Number(e.target.value) }))}
                />
              </label>

              <label className="field">
                <span>Pharmacy (optional)</span>
                <input
                  type="text"
                  value={rxForm.pharmacyName}
                  onChange={(e) => setRxForm((f) => ({ ...f, pharmacyName: e.target.value }))}
                  placeholder="e.g. CVS Pharmacy"
                />
              </label>

              {issue.isError ? (
                <p className="auth-error">{getApiError(issue.error)}</p>
              ) : null}
              {rxSuccess ? (
                <p style={{ color: 'var(--accent)', fontSize: 13 }}>{rxSuccess}</p>
              ) : null}

              <button
                type="submit"
                className="btn btn-primary btn-block"
                disabled={issue.isPending}
              >
                {issue.isPending ? 'Issuing…' : 'Issue prescription'}
              </button>
            </form>
          </div>
        </section>

        <section style={{ marginTop: 40 }}>
          <h2 className="section-heading">Prescriptions issued</h2>

          {prescriptions.isLoading ? <p className="text-dim">Loading…</p> : null}
          {prescriptions.isError ? <p className="auth-error">Failed to load prescriptions.</p> : null}

          {prescriptions.data?.length === 0 ? (
            <p className="text-dim">No prescriptions issued yet.</p>
          ) : null}

          <div className="appt-list">
            {prescriptions.data?.map((rx) => (
              <div key={rx.id} className="appt-card">
                <div className="appt-card-header">
                  <span className={`appt-status appt-status--${rx.status.toLowerCase()}`}>
                    {rx.status}
                  </span>
                  <span className="appt-date">
                    {new Date(rx.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <div className="appt-card-body">
                  <strong>{rx.drugName}</strong> — {rx.dosage}, {rx.frequency}
                  <p className="text-dim">Patient: {rx.patientName}</p>
                  <p className="text-dim">Refills: {rx.refillsRemaining}</p>
                  {rx.pharmacyName ? <p className="text-dim">Pharmacy: {rx.pharmacyName}</p> : null}
                </div>
                {rx.status === 'Active' ? (
                  <button
                    className="btn btn-danger btn-sm"
                    aria-label={`Cancel prescription for ${rx.patientName}`}
                    disabled={cancelRx.isPending}
                    onClick={() => cancelRx.mutate(rx.id)}
                  >
                    {cancelRx.isPending ? 'Cancelling…' : 'Cancel'}
                  </button>
                ) : null}
              </div>
            ))}
          </div>

          {cancelRx.isError ? (
            <p className="auth-error" style={{ marginTop: 8 }}>{getApiError(cancelRx.error)}</p>
          ) : null}
        </section>
        </>
        )}
      </main>
    </div>
  );
}
