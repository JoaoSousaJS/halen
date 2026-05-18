import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../video-consultation.css';

function PatientSummary({
  doctorName,
  elapsedSeconds,
}: {
  doctorName: string;
  elapsedSeconds: number;
}) {
  const navigate = useNavigate();
  const durationMinutes = Math.round(elapsedSeconds / 60);

  return (
    <div>
      <div className="vc-post-call__eyebrow">
        Today · {durationMinutes} minutes
      </div>
      <h2 className="vc-post-call__heading">
        Consult <em>complete.</em>
      </h2>

      <div className="vc-post-call__card" style={{ marginTop: 28 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <div style={{ flex: 1 }}>
            <strong style={{ fontSize: 15 }}>{doctorName}</strong>
          </div>
        </div>
        <p className="vc-post-call__detail">
          Your consultation has ended. Check your dashboard for any prescriptions or follow-up appointments.
        </p>
      </div>

      <div className="vc-post-call__actions">
        <button className="btn btn-primary" onClick={() => navigate('/dashboard')}>
          Done
        </button>
      </div>
    </div>
  );
}

function DoctorFinalize({
  patientName,
  notes,
  onSave,
}: {
  patientName: string;
  notes: string;
  onSave: (notes: string) => void;
}) {
  const [draft, setDraft] = useState(notes);

  return (
    <div className="vc-post-call--doctor">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
        <div>
          <div className="vc-post-call__eyebrow">
            Wrap-up · {patientName}
          </div>
          <h2 className="vc-post-call__heading">
            Save your <em>consult.</em>
          </h2>
        </div>

        <section style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <h3 style={{
            fontFamily: 'var(--font-display)',
            fontSize: 18,
            fontWeight: 400,
          }}>
            Consultation notes
          </h3>
          <textarea
            role="textbox"
            className="vc-sidebar__notes"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            style={{ minHeight: 220 }}
          />
          <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>
            Auto-saved · Visible to patient
          </div>
        </section>
      </div>

      <aside style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div className="vc-post-call__card">
          <strong>{patientName}</strong>
        </div>

        <button
          className="btn btn-primary"
          style={{ width: '100%', padding: '14px 18px', fontSize: 15 }}
          onClick={() => onSave(draft)}
        >
          Save & complete
        </button>
        <button className="btn btn-ghost" style={{ width: '100%' }}>
          Save as draft
        </button>
      </aside>
    </div>
  );
}

export function PostCallWrapUp({
  role,
  doctorName,
  patientName,
  notes,
  elapsedSeconds,
  onSave,
}: {
  role: string;
  doctorName: string;
  patientName: string;
  notes: string;
  elapsedSeconds: number;
  onSave?: (notes: string) => void;
}) {
  return (
    <div className="vc-post-call">
      {role === 'Patient' ? (
        <PatientSummary doctorName={doctorName} elapsedSeconds={elapsedSeconds} />
      ) : (
        <DoctorFinalize
          patientName={patientName}
          notes={notes}
          onSave={onSave ?? (() => {})}
        />
      )}
    </div>
  );
}
