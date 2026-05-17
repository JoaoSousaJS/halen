import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../shared/components/AuthProvider';
import { DashboardShell } from '../../shared/components/DashboardShell';
import { getApiError } from '../../shared/api/errors';
import {
  listDoctors,
  getMyAppointments,
  bookAppointment,
  cancelAppointment,
} from '../../shared/api/appointments';
import type { DoctorDto } from '../../shared/api/appointments';
import { getMyPrescriptions } from '../../shared/api/prescriptions';
import { getDoctorAvailability, getAvailableSlots } from '../../shared/api/availability';
import type { TimeSlot } from '../../shared/api/availability';
import { useNotifications } from '../../shared/hooks/useNotifications';
import { ToastContainer } from '../../shared/components/ToastContainer';
import { FeatureGate } from '../../shared/components/FeatureGate';
import { Button, Field } from '../../shared/components';

function todayDate(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export default function PatientDashboard() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const { toasts, dismissToast } = useNotifications();

  const [doctorId, setDoctorId] = useState('');
  const [selectedDate, setSelectedDate] = useState('');
  const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);
  const [reason, setReason] = useState('');
  const [bookSuccess, setBookSuccess] = useState('');

  const doctors = useQuery({ queryKey: ['doctors'], queryFn: listDoctors });
  const appointments = useQuery({ queryKey: ['my-appointments'], queryFn: getMyAppointments });
  const prescriptions = useQuery({ queryKey: ['my-prescriptions'], queryFn: getMyPrescriptions });

  const doctorAvailability = useQuery({
    queryKey: ['doctor-availability', doctorId],
    queryFn: () => getDoctorAvailability(doctorId),
    enabled: !!doctorId,
  });

  const availableSlots = useQuery({
    queryKey: ['available-slots', doctorId, selectedDate],
    queryFn: () => getAvailableSlots(doctorId, selectedDate),
    enabled: !!doctorId && !!selectedDate,
  });

  const book = useMutation({
    mutationFn: () => bookAppointment({
      doctorId,
      scheduledAt: selectedSlot!.startUtc,
      reason,
    }),
    onSuccess: () => {
      setDoctorId('');
      setSelectedDate('');
      setSelectedSlot(null);
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

  function handleBook(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBookSuccess('');
    book.mutate();
  }

  const selectedDoctor: DoctorDto | undefined = doctors.data?.find((d) => d.id === doctorId);
  const hasAvailability = (doctorAvailability.data?.length ?? 0) > 0;
  const slotsForDate = availableSlots.data?.filter((s) => s.isAvailable) ?? [];

  return (
    <DashboardShell
      subtitle="care · on call"
      userName={`${user?.given_name} ${user?.family_name}`}
    >
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />
        <section>
          <h1 className="auth-heading">
            Book an<br /><em>appointment.</em>
          </h1>

          <div className="auth-card">
            <form onSubmit={handleBook} className="auth-form">
              <Field label="Doctor">
                <select
                  required
                  value={doctorId}
                  onChange={(e) => {
                    setDoctorId(e.target.value);
                    setSelectedDate('');
                    setSelectedSlot(null);
                  }}
                >
                  <option value="">Select a doctor…</option>
                  {doctors.data?.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name} — {d.specialty} (${d.consultationFee})
                    </option>
                  ))}
                </select>
              </Field>

              {selectedDoctor ? (
                <p className="doctor-hint">
                  {selectedDoctor.yearsOfExperience} years of experience · ${selectedDoctor.consultationFee} per visit
                </p>
              ) : null}

              {doctorId && doctorAvailability.isLoading ? (
                <p className="text-dim">Checking doctor availability...</p>
              ) : null}

              {doctorId && !doctorAvailability.isLoading && !hasAvailability ? (
                <p className="text-dim">This doctor hasn't set up their schedule yet.</p>
              ) : null}

              {doctorId && hasAvailability ? (
                <>
                  <Field label="Date">
                    <input
                      type="date"
                      required
                      value={selectedDate}
                      min={todayDate()}
                      onChange={(e) => {
                        setSelectedDate(e.target.value);
                        setSelectedSlot(null);
                      }}
                    />
                  </Field>

                  {selectedDate && availableSlots.isLoading ? (
                    <p className="text-dim">Loading available slots...</p>
                  ) : null}

                  {selectedDate && !availableSlots.isLoading && slotsForDate.length === 0 ? (
                    <p className="text-dim">No available slots on this date. Try another day.</p>
                  ) : null}

                  {selectedDate && slotsForDate.length > 0 ? (
                    <div>
                      <span className="field" style={{ marginBottom: 6 }}>
                        <span>Available slots</span>
                      </span>
                      <div className="slot-grid">
                        {slotsForDate.map((slot) => (
                          <Button
                            key={slot.startUtc}
                            type="button"
                            variant={selectedSlot?.startUtc === slot.startUtc ? 'primary' : 'ghost'}
                            size="sm"
                            onClick={() => setSelectedSlot(slot)}
                          >
                            {slot.startLocal}
                          </Button>
                        ))}
                      </div>
                    </div>
                  ) : null}
                </>
              ) : null}

              <Field label="Reason for visit">
                <textarea
                  required
                  rows={3}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="Describe your symptoms or reason…"
                />
              </Field>

              {book.isError ? (
                <p className="auth-error">{getApiError(book.error)}</p>
              ) : null}
              {bookSuccess ? (
                <p style={{ color: 'var(--accent)', fontSize: 13 }}>{bookSuccess}</p>
              ) : null}

              <Button
                variant="primary"
                block
                type="submit"
                disabled={book.isPending || !selectedSlot}
              >
                {book.isPending ? 'Booking…' : 'Book appointment'}
              </Button>
            </form>
          </div>
        </section>

        <section>
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
                  <Button
                    variant="danger"
                    size="sm"
                    ariaLabel={`Cancel appointment with ${a.doctorName}`}
                    data-testid={`cancel-appointment-${a.id}`}
                    disabled={cancel.isPending}
                    onClick={() => {
                      cancel.reset();
                      setCancellingId(a.id);
                      cancel.mutate(a.id);
                    }}
                  >
                    {cancel.isPending && cancellingId === a.id ? 'Cancelling…' : 'Cancel'}
                  </Button>
                ) : null}
              </div>
            ))}
          </div>

          {cancel.isError ? (
            <p className="auth-error">{getApiError(cancel.error)}</p>
          ) : null}
        </section>

        <FeatureGate feature="prescriptions">
          <section>
            <h2 className="section-heading">Your prescriptions</h2>

            {prescriptions.isLoading ? <p className="text-dim">Loading…</p> : null}
            {prescriptions.isError ? <p className="auth-error">Failed to load prescriptions.</p> : null}

            {prescriptions.data?.length === 0 ? (
              <p className="text-dim">No prescriptions yet.</p>
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
                    <p className="text-dim">Prescribed by: {rx.doctorName}</p>
                    <p className="text-dim">Refills remaining: {rx.refillsRemaining}</p>
                    {rx.pharmacyName ? <p className="text-dim">Pharmacy: {rx.pharmacyName}</p> : null}
                  </div>
                </div>
              ))}
            </div>
          </section>
        </FeatureGate>
    </DashboardShell>
  );
}
