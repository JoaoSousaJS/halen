import '../video-consultation.css';
import { VideoTile } from './VideoTile';
import { getInitials } from '../utils';

function hashHue(name: string): number {
  let h = 0;
  for (let i = 0; i < name.length; i++) {
    h = (h * 31 + name.charCodeAt(i)) >>> 0;
  }
  return h % 360;
}

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
  const selfName = role === 'Patient' ? patientName : doctorName;
  const otherName = role === 'Patient' ? doctorName : patientName;
  const otherRole = role === 'Patient' ? 'Doctor' : 'Patient';
  const otherReady = participants.some(
    (p) => p.role.toLowerCase() === otherRole.toLowerCase(),
  );

  return (
    <div className="vc-lobby">
      {/* Left — camera preview */}
      <div className="vc-lobby__preview">
        <div className="vc-lobby__eyebrow">Camera preview</div>
        <div className="vc-lobby__preview-frame">
          <VideoTile name={selfName} size="lg" />
        </div>
      </div>

      {/* Right — appointment info */}
      <div className="vc-lobby__card">
        <div className="vc-lobby__eyebrow">
          {role === 'Patient' ? 'Your appointment' : 'Next patient'}
        </div>

        <div className="vc-lobby__appt">
          <div className="vc-lobby__appt-row">
            <div
              className="vc-lobby__appt-avatar"
              style={{
                background: `oklch(0.32 0.08 ${hashHue(otherName)})`,
              }}
            >
              {getInitials(otherName)}
            </div>
            <div style={{ flex: 1 }}>
              <strong style={{ fontSize: 14 }}>{otherName}</strong>
              <div style={{ fontSize: 12, color: 'var(--text-dim)' }}>
                {role === 'Patient' ? 'Doctor' : 'Patient'}
              </div>
            </div>
          </div>

          {reason && (
            <div style={{ fontSize: 13, color: 'var(--text-dim)' }}>
              Reason: {reason}
            </div>
          )}
        </div>

        {otherReady && (
          <div className="vc-lobby__ready">
            <span className="vc-lobby__ready-dot" />
            <div style={{ flex: 1 }}>
              <strong>{otherName} is ready.</strong>
              <div className="vc-lobby__ready-sub">
                Join when you're set up.
              </div>
            </div>
          </div>
        )}

        {!otherReady && participants.length > 0 && (
          <div className="vc-lobby__ready">
            <span className="vc-lobby__ready-dot" />
            <div>
              <p>
                {participants.length} participant
                {participants.length > 1 ? 's' : ''} ready
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
          {role === 'Patient' ? 'Join consult' : 'Admit & start consult'}
        </button>

        <p className="vc-lobby__terms">
          By joining, you confirm Halen's terms of telehealth care.
        </p>
      </div>
    </div>
  );
}
