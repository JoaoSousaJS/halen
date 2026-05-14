import { useState } from 'react';
import type { SubmitEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../shared/components/AuthProvider';
import { getApiError } from '../../shared/api/errors';
import {
  listDoctors,
  getMyAppointments,
  bookAppointment,
  cancelAppointment,
} from '../../shared/api/appointments';
import type { DoctorDto } from '../../shared/api/appointments';

function toLocalDatetime(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export default function PatientDashboard() {
  const { user, logout } = useAuth();
  const queryClient = useQueryClient();

  const [doctorId, setDoctorId] = useState('');
  const [scheduledAt, setScheduledAt] = useState('');
  const [reason, setReason] = useState('');
  const [bookSuccess, setBookSuccess] = useState('');

  const doctors = useQuery({ queryKey: ['doctors'], queryFn: listDoctors });
  const appointments = useQuery({ queryKey: ['my-appointments'], queryFn: getMyAppointments });

  const book = useMutation({
    mutationFn: () => bookAppointment({
      doctorId,
      scheduledAt: new Date(scheduledAt).toISOString(),
      reason,
    }),
    onSuccess: () => {
      setDoctorId('');
      setScheduledAt('');
      setReason('');
      setBookSuccess('Appointment booked!');
      setTimeout(() => setBookSuccess(''), 4000);
      queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
    },
  });

  const [cancellingId, setCancellingId] = useState<string | null>(null);

  const cancel = useMutation({
    mutationFn: cancelAppointment,
    onSuccess: () => {
      setCancellingId(null);
      queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
    },
    onError: () => setCancellingId(null),
  });

  function handleBook(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault();
    setBookSuccess('');
    book.mutate();
  }

  const selectedDoctor: DoctorDto | undefined = doctors.data?.find((d) => d.id === doctorId);
  const minDatetime = toLocalDatetime(new Date());

  return (
    <div className="dashboard-shell">
      <header className="dashboard-header">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">care · on call</div>
          </div>
        </div>
        <span className="dashboard-user">{user?.given_name} {user?.family_name}</span>
        <button className="btn btn-sm" onClick={logout}>Sign out</button>
      </header>

      <main className="dashboard-main">
        <section>
          <h1 className="auth-heading">
            Book an<br /><em>appointment.</em>
          </h1>

          <div className="auth-card" style={{ marginTop: 20 }}>
            <form onSubmit={handleBook} className="auth-form">
              <label className="field">
                <span>Doctor</span>
                <select
                  required
                  value={doctorId}
                  onChange={(e) => setDoctorId(e.target.value)}
                >
                  <option value="">Select a doctor…</option>
                  {doctors.data?.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name} — {d.specialty} (${d.consultationFee})
                    </option>
                  ))}
                </select>
              </label>

              {selectedDoctor ? (
                <p className="doctor-hint">
                  {selectedDoctor.yearsOfExperience} years of experience · ${selectedDoctor.consultationFee} per visit
                </p>
              ) : null}

              <label className="field">
                <span>Date & time</span>
                <input
                  type="datetime-local"
                  required
                  value={scheduledAt}
                  min={minDatetime}
                  onChange={(e) => setScheduledAt(e.target.value)}
                />
              </label>

              <label className="field">
                <span>Reason for visit</span>
                <textarea
                  required
                  rows={3}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="Describe your symptoms or reason…"
                />
              </label>

              {book.isError ? (
                <p className="auth-error">{getApiError(book.error)}</p>
              ) : null}
              {bookSuccess ? (
                <p style={{ color: 'var(--accent)', fontSize: 13 }}>{bookSuccess}</p>
              ) : null}

              <button
                type="submit"
                className="btn btn-primary btn-block"
                disabled={book.isPending}
              >
                {book.isPending ? 'Booking…' : 'Book appointment'}
              </button>
            </form>
          </div>
        </section>

        <section style={{ marginTop: 40 }}>
          <h2 className="section-heading">Your appointments</h2>

          {appointments.isLoading ? <p className="text-dim">Loading…</p> : null}

          {appointments.data?.length === 0 ? (
            <p className="text-dim">No appointments yet — book one above.</p>
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
                  <strong>{a.doctorName}</strong>
                  <span className="text-dim">{a.specialty}</span>
                  <p>{a.reason}</p>
                  {a.notes ? <p className="text-dim">Notes: {a.notes}</p> : null}
                </div>
                {a.status === 'Scheduled' ? (
                  <button
                    className="btn btn-danger btn-sm"
                    aria-label={`Cancel appointment with ${a.doctorName}`}
                    disabled={cancel.isPending}
                    onClick={() => {
                      cancel.reset();
                      setCancellingId(a.id);
                      cancel.mutate(a.id);
                    }}
                  >
                    {cancel.isPending && cancellingId === a.id ? 'Cancelling…' : 'Cancel'}
                  </button>
                ) : null}
              </div>
            ))}
          </div>

          {cancel.isError ? (
            <p className="auth-error" style={{ marginTop: 8 }}>{getApiError(cancel.error)}</p>
          ) : null}
        </section>
      </main>
    </div>
  );
}
