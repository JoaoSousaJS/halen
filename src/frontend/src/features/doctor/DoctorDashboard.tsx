import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../shared/components/AuthProvider';
import { DashboardShell } from '../../shared/components/DashboardShell';
import { getApiError } from '../../shared/api/errors';
import {
  getMyAppointments,
  cancelAppointment,
  completeAppointment,
} from '../../shared/api/appointments';
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
import AvailabilityEditor from './AvailabilityEditor';
import { FeatureGate } from '../../shared/components/FeatureGate';
import { Button, Field, Chip } from '../../shared/components';
import DoctorMyReviews from './DoctorMyReviews';

export default function DoctorDashboard() {
  const { user } = useAuth();
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
    <DashboardShell
      subtitle="Doctor portal"
      userName={`Dr. ${user?.family_name}`}
      wide
    >
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />

      {!kycApproved ? (
        <FeatureGate feature="kyc">
          <div className="doc-welcome">
            <h1 className="doc-welcome-heading">
              Welcome, <em>Dr. {user?.family_name}.</em>
            </h1>
          </div>
          <KycSetup />
        </FeatureGate>
      ) : (
        <>
          <div className="doc-welcome">
            <h1 className="doc-welcome-heading">
              Welcome back, <em>Dr. {user?.family_name}.</em>
            </h1>
          </div>

          <div className="doc-grid">
            {/* ── Left column: schedule + availability ── */}
            <div className="doc-col">
              <section>
                <h3 className="doc-section-title">Today's schedule</h3>

                {appointments.isLoading && <p className="text-dim">Loading…</p>}
                {appointments.isError && <p className="dialog-error">Failed to load appointments.</p>}
                {appointments.data?.length === 0 && <p className="text-dim">No appointments yet.</p>}

                <div className="doc-schedule">
                  {appointments.data?.map((a) => (
                    <div key={a.id} className="doc-schedule-card">
                      <span className="doc-schedule-time">
                        {new Date(a.scheduledAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                      <div className="doc-schedule-info">
                        <div className="doc-schedule-row">
                          <strong>{a.patientName}</strong>
                          <Chip
                            status={a.status}
                            variant={a.status === 'Scheduled' ? 'good' : a.status === 'Cancelled' ? 'danger' : undefined}
                          />
                        </div>
                        <p className="text-dim">{a.reason}</p>
                        {a.notes && <p className="text-dim">Notes: {a.notes}</p>}

                        {a.status === 'Scheduled' && (
                          <div className="doc-schedule-actions">
                            {completingId === a.id ? (
                              <div className="appt-complete-form">
                                <textarea
                                  rows={2}
                                  value={notesMap[a.id] ?? ''}
                                  onChange={(e) => setNotesMap((prev) => ({ ...prev, [a.id]: e.target.value }))}
                                  placeholder="Session notes (optional)…"
                                />
                                <div className="appt-complete-buttons">
                                  <Button variant="primary" size="sm" disabled={complete.isPending} onClick={() => complete.mutate(a.id)}>
                                    {complete.isPending ? 'Saving…' : 'Confirm'}
                                  </Button>
                                  <Button size="sm" onClick={() => setCompletingId(null)}>Back</Button>
                                </div>
                              </div>
                            ) : (
                              <>
                                <Button variant="primary" size="sm" ariaLabel={`Complete appointment with ${a.patientName}`} onClick={() => setCompletingId(a.id)}>
                                  Complete
                                </Button>
                                <Button variant="danger" size="sm" ariaLabel={`Cancel appointment with ${a.patientName}`} disabled={cancel.isPending} onClick={() => { cancel.reset(); setCancellingId(a.id); cancel.mutate(a.id); }}>
                                  {cancel.isPending && cancellingId === a.id ? 'Cancelling…' : 'Cancel'}
                                </Button>
                              </>
                            )}
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>

                {cancel.isError && <p className="dialog-error" role="alert">{getApiError(cancel.error)}</p>}
                {complete.isError && <p className="dialog-error" role="alert">{getApiError(complete.error)}</p>}
              </section>

              <section>
                <h3 className="doc-section-title">Your availability</h3>
                <AvailabilityEditor />
              </section>
            </div>

            {/* ── Right column: prescriptions + reviews ── */}
            <div className="doc-col">
              <FeatureGate feature="prescriptions">
                <section>
                  <h3 className="doc-section-title">Issue prescription</h3>
                  <div className="doc-card">
                    <form onSubmit={handleIssue} className="doc-rx-form">
                      <Field label="Patient">
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
                      </Field>

                      <div className="doc-rx-row">
                        <Field label="Drug name">
                          <input type="text" required value={rxForm.drugName} onChange={(e) => setRxForm((f) => ({ ...f, drugName: e.target.value }))} placeholder="e.g. Amoxicillin" />
                        </Field>
                        <Field label="Dosage">
                          <input type="text" required value={rxForm.dosage} onChange={(e) => setRxForm((f) => ({ ...f, dosage: e.target.value }))} placeholder="e.g. 500mg" />
                        </Field>
                      </div>

                      <div className="doc-rx-row">
                        <Field label="Frequency">
                          <input type="text" required value={rxForm.frequency} onChange={(e) => setRxForm((f) => ({ ...f, frequency: e.target.value }))} placeholder="e.g. Twice daily" />
                        </Field>
                        <Field label="Refills">
                          <input type="number" required min={0} max={24} value={rxForm.refillsRemaining} onChange={(e) => setRxForm((f) => ({ ...f, refillsRemaining: Number(e.target.value) }))} />
                        </Field>
                      </div>

                      <Field label="Pharmacy (optional)">
                        <input type="text" value={rxForm.pharmacyName} onChange={(e) => setRxForm((f) => ({ ...f, pharmacyName: e.target.value }))} placeholder="e.g. CVS Pharmacy" />
                      </Field>

                      {issue.isError && <p className="dialog-error" role="alert">{getApiError(issue.error)}</p>}
                      {rxSuccess && <p className="doc-rx-success">{rxSuccess}</p>}

                      <Button variant="primary" block type="submit" disabled={issue.isPending}>
                        {issue.isPending ? 'Issuing…' : 'Issue prescription'}
                      </Button>
                    </form>
                  </div>
                </section>

                <section>
                  <h3 className="doc-section-title">Recent prescriptions</h3>

                  {prescriptions.isLoading && <p className="text-dim">Loading…</p>}
                  {prescriptions.isError && <p className="dialog-error">Failed to load prescriptions.</p>}
                  {prescriptions.data?.length === 0 && <p className="text-dim">No prescriptions issued yet.</p>}

                  {prescriptions.data && prescriptions.data.length > 0 && (
                    <div className="doc-card doc-rx-list">
                      {prescriptions.data.map((rx) => (
                        <div key={rx.id} className="doc-rx-item">
                          <div className="doc-rx-item-info">
                            <div className="doc-rx-item-top">
                              <strong>{rx.drugName}</strong>
                              <span className="text-dim"> — {rx.dosage}, {rx.frequency}</span>
                            </div>
                            <p className="text-dim">{rx.patientName} · {rx.refillsRemaining} refill{rx.refillsRemaining !== 1 ? 's' : ''}{rx.pharmacyName ? ` · ${rx.pharmacyName}` : ''}</p>
                          </div>
                          <div className="doc-rx-item-actions">
                            <Chip
                              status={rx.status}
                              variant={rx.status === 'Active' ? 'good' : 'danger'}
                            />
                            {rx.status === 'Active' && (
                              <Button variant="danger" size="sm" ariaLabel={`Cancel prescription for ${rx.patientName}`} disabled={cancelRx.isPending} onClick={() => cancelRx.mutate(rx.id)}>
                                {cancelRx.isPending ? 'Cancelling…' : 'Cancel'}
                              </Button>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {cancelRx.isError && <p className="dialog-error" role="alert">{getApiError(cancelRx.error)}</p>}
                </section>
              </FeatureGate>

              <FeatureGate feature="doctor_reviews">
                <DoctorMyReviews />
              </FeatureGate>
            </div>
          </div>
        </>
      )}
    </DashboardShell>
  );
}
