import '../video-consultation.css';
import { VideoTile } from './VideoTile';

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
  const selfName = role === "Patient" ? patientName : doctorName;

  return (
    <div className="vc-lobby">
      <div className="vc-lobby__preview">
        <VideoTile name={selfName} size="lg" />
      </div>

      <div className="vc-lobby__card">
        <h2 className="vc-lobby__heading">Appointment brief</h2>
        <p className="vc-lobby__detail">Doctor: {doctorName}</p>
        <p className="vc-lobby__detail">Patient: {patientName}</p>
        <p className="vc-lobby__detail">Reason: {reason}</p>

        {participants.length > 0 && (
          <div className="vc-lobby__ready">
            <span className="vc-lobby__ready-dot" />
            <div>
              <p>
                {participants.length} participant{participants.length > 1 ? "s" : ""} ready
              </p>
              <ul className="vc-lobby__participants">
                {participants.map((p) => (
                  <li key={p.name}>
                    {p.name} ({p.role})
                  </li>
                ))}
              </ul>
            </div>
          </div>
        )}

        <button className="vc-lobby__join btn btn-primary" onClick={onJoin}>
          {role === "Patient" ? "Join consult" : "Admit & start consult"}
        </button>
      </div>
    </div>
  );
}
