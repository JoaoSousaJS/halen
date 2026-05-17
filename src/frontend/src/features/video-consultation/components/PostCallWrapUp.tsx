import { useState } from "react";
import { useNavigate } from "react-router-dom";
import "../video-consultation.css";

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
      <h2 className="vc-post-call__heading">Consult complete.</h2>
      <p className="vc-post-call__detail">Doctor: {doctorName}</p>
      <p className="vc-post-call__detail">Duration: {durationMinutes} minutes</p>
      <div className="vc-post-call__actions">
        <button className="btn btn-primary" onClick={() => navigate("/dashboard")}>
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
    <div>
      <h2 className="vc-post-call__heading">Save your consult.</h2>
      <p className="vc-post-call__detail">Patient: {patientName}</p>
      <textarea
        role="textbox"
        className="vc-sidebar__notes"
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
      />
      <div className="vc-post-call__actions">
        <button className="btn btn-primary" onClick={() => onSave(draft)}>
          Save &amp; complete
        </button>
      </div>
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
      {role === "Patient" ? (
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
