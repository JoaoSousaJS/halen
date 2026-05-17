import { useState } from "react";
import { useNavigate } from "react-router-dom";

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
    <div className="post-call__patient">
      <h2>Consult complete.</h2>
      <p>Doctor: {doctorName}</p>
      <p>Duration: {durationMinutes} minutes</p>
      <button className="post-call__done" onClick={() => navigate("/dashboard")}>
        Done
      </button>
    </div>
  );
}

function DoctorFinalize({ notes }: { notes: string }) {
  const [draft, setDraft] = useState(notes);

  return (
    <div className="post-call__doctor">
      <h2>Save your consult.</h2>
      <textarea
        role="textbox"
        className="post-call__notes"
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
      />
      <button className="post-call__save">Save &amp; complete</button>
    </div>
  );
}

export function PostCallWrapUp({
  role,
  doctorName,
  patientName,
  notes,
  elapsedSeconds,
}: {
  role: string;
  doctorName: string;
  patientName: string;
  notes: string;
  elapsedSeconds: number;
}) {
  return (
    <div className="post-call-wrap-up">
      {role === "Patient" ? (
        <PatientSummary doctorName={doctorName} elapsedSeconds={elapsedSeconds} />
      ) : (
        <DoctorFinalize notes={notes} />
      )}
    </div>
  );
}
