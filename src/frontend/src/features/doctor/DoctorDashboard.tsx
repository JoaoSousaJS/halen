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
import AvailabilityEditor from './AvailabilityEditor';
import { FeatureGate } from '../../shared/components/FeatureGate';
import { Button, Field } from '../../shared/components';
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
    >
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />
        {!kycApproved ? (
          <FeatureGate feature="kyc">
            <h1 className="auth-heading">
              Welcome,<br /><em>Dr. {user?.family_name}.</em>
            </h1>
            <KycSetup />
          </FeatureGate>
        ) : (
        <>
        <section>
          <h2 className="section-heading">Your availability</h2>
          <AvailabilityEditor />
        </section>

        <section>
        <h1 className="auth-heading">
          Your<br /><em>schedule.</em>
        </h1>

        {appointments.isLoading ? <p className="text-dim">Loading…</p> : null}
        {appointments.isError ? <p className="auth-error">Failed to load appointments.</p> : null}

        {appointments.data?.length === 0 ? (
          <p className="text-dim">No appointments yet.</p>
        ) : null}

        <div className="appt-list">
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
                        <Button
                          variant="primary"
                          size="sm"
                          disabled={complete.isPending}
                          onClick={() => complete.mutate(a.id)}
                        >
                          {complete.isPending ? 'Saving…' : 'Confirm'}
                        </Button>
                        <Button
                          size="sm"
                          onClick={() => setCompletingId(null)}
                        >
                          Back
                        </Button>
                      </div>
                    </div>
                  ) : (
                    <>
                      <Button
                        variant="primary"
                        size="sm"
                        ariaLabel={`Complete appointment with ${a.patientName}`}
                        onClick={() => setCompletingId(a.id)}
                      >
                        Complete
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        ariaLabel={`Cancel appointment with ${a.patientName}`}
                        disabled={cancel.isPending}
                        onClick={() => {
                          cancel.reset();
                          setCancellingId(a.id);
                          cancel.mutate(a.id);
                        }}
                      >
                        {cancel.isPending && cancellingId === a.id ? 'Cancelling…' : 'Cancel'}
                      </Button>
                    </>
                  )}
                </div>
              ) : null}
            </div>
          ))}
        </div>

        {cancel.isError ? (
          <p className="auth-error">{getApiError(cancel.error)}</p>
        ) : null}
        {complete.isError ? (
          <p className="auth-error">{getApiError(complete.error)}</p>
        ) : null}
        </section>

        <FeatureGate feature="prescriptions">
          <section>
            <h2 className="section-heading">Issue a prescription</h2>

            <div className="auth-card">
              <form onSubmit={handleIssue} className="auth-form">
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

                <Field label="Drug name">
                  <input
                    type="text"
                    required
                    value={rxForm.drugName}
                    onChange={(e) => setRxForm((f) => ({ ...f, drugName: e.target.value }))}
                    placeholder="e.g. Amoxicillin"
                  />
                </Field>

                <Field label="Dosage">
                  <input
                    type="text"
                    required
                    value={rxForm.dosage}
                    onChange={(e) => setRxForm((f) => ({ ...f, dosage: e.target.value }))}
                    placeholder="e.g. 500mg"
                  />
                </Field>

                <Field label="Frequency">
                  <input
                    type="text"
                    required
                    value={rxForm.frequency}
                    onChange={(e) => setRxForm((f) => ({ ...f, frequency: e.target.value }))}
                    placeholder="e.g. Twice daily"
                  />
                </Field>

                <Field label="Refills">
                  <input
                    type="number"
                    required
                    min={0}
                    max={24}
                    value={rxForm.refillsRemaining}
                    onChange={(e) => setRxForm((f) => ({ ...f, refillsRemaining: Number(e.target.value) }))}
                  />
                </Field>

                <Field label="Pharmacy (optional)">
                  <input
                    type="text"
                    value={rxForm.pharmacyName}
                    onChange={(e) => setRxForm((f) => ({ ...f, pharmacyName: e.target.value }))}
                    placeholder="e.g. CVS Pharmacy"
                  />
                </Field>

                {issue.isError ? (
                  <p className="auth-error">{getApiError(issue.error)}</p>
                ) : null}
                {rxSuccess ? (
                  <p style={{ color: 'var(--accent)', fontSize: 13 }}>{rxSuccess}</p>
                ) : null}

                <Button
                  variant="primary"
                  block
                  type="submit"
                  disabled={issue.isPending}
                >
                  {issue.isPending ? 'Issuing…' : 'Issue prescription'}
                </Button>
              </form>
            </div>
          </section>

          <section>
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
                    <Button
                      variant="danger"
                      size="sm"
                      ariaLabel={`Cancel prescription for ${rx.patientName}`}
                      disabled={cancelRx.isPending}
                      onClick={() => cancelRx.mutate(rx.id)}
                    >
                      {cancelRx.isPending ? 'Cancelling…' : 'Cancel'}
                    </Button>
                  ) : null}
                </div>
              ))}
            </div>

            {cancelRx.isError ? (
              <p className="auth-error">{getApiError(cancelRx.error)}</p>
            ) : null}
          </section>
        </FeatureGate>

        <FeatureGate feature="doctor_reviews">
          <DoctorMyReviews />
        </FeatureGate>
        </>
        )}
    </DashboardShell>
  );
}
