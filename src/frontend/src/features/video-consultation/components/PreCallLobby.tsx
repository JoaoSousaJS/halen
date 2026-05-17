export function PreCallLobby({
  role,
  doctorName,
  patientName,
  reason,
  participants,
  onJoin,
}: {
  role: string;
  doctorName: string;
  patientName: string;
  reason: string;
  participants: { name: string; role: string }[];
  onJoin: () => void;
}) {
  return (
    <div className="pre-call-lobby">
      <div className="pre-call-lobby__brief">
        <h2 className="pre-call-lobby__heading">Appointment brief</h2>
        <p className="pre-call-lobby__doctor">Doctor: {doctorName}</p>
        <p className="pre-call-lobby__patient">Patient: {patientName}</p>
        <p className="pre-call-lobby__reason">Reason: {reason}</p>
      </div>

      {participants.length > 0 && (
        <div className="pre-call-lobby__ready">
          <p>
            {participants.length} participant{participants.length > 1 ? "s" : ""} ready
          </p>
          <ul className="pre-call-lobby__participants">
            {participants.map((p) => (
              <li key={p.name}>
                {p.name} ({p.role})
              </li>
            ))}
          </ul>
        </div>
      )}

      <button className="pre-call-lobby__join" onClick={onJoin}>
        {role === "Patient" ? "Join consult" : "Admit & start consult"}
      </button>
    </div>
  );
}
