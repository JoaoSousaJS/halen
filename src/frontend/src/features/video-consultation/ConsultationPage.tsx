import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useAuth } from '../../shared/components/AuthProvider';
import { getConsultationRoom, type ConsultationRoomDto } from '../../shared/api/consultations';
import { useConsultation } from './hooks/useConsultation';
import { PreCallLobby } from './components/PreCallLobby';
import { VideoTile } from './components/VideoTile';
import { ControlPill } from './components/ControlPill';
import { ChatDrawer } from './components/ChatDrawer';
import { ClinicalSidebar } from './components/ClinicalSidebar';
import { PostCallWrapUp } from './components/PostCallWrapUp';
import './video-consultation.css';

export default function ConsultationPage() {
  const { appointmentId } = useParams<{ appointmentId: string }>();
  const { token, user } = useAuth();
  const role = user?.role ?? 'Patient';
  const fullName = user ? `${user.given_name} ${user.family_name}` : '';

  const [room, setRoom] = useState<ConsultationRoomDto | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);

  const {
    state,
    joinRoom,
    sendChat,
    updateNotes,
    endConsultation,
    toggleMic,
    toggleCam,
    toggleChat,
    toggleSidebar,
  } = useConsultation(appointmentId!, token);

  useEffect(() => {
    if (!appointmentId || !token) return;
    getConsultationRoom(appointmentId)
      .then(setRoom)
      .catch(() => setLoadError('Unable to load consultation room.'));
  }, [appointmentId, token]);

  if (loadError) {
    return (
      <div className="vc-post-call">
        <h2 className="vc-post-call__heading">{loadError}</h2>
      </div>
    );
  }

  if (!room) {
    return (
      <div className="vc-connecting">
        <div className="vc-connecting__mark" />
        <div className="vc-connecting__text">
          Connecting<em>…</em>
        </div>
        <div className="vc-connecting__sub">
          Joining secure room · negotiating audio + video.
        </div>
      </div>
    );
  }

  const isEnded = state.phase === 'ended' || room.status === 'Ended';
  const isActive = state.phase === 'active';
  const isLobby = !isEnded && !isActive;

  if (isEnded) {
    return (
      <PostCallWrapUp
        role={role}
        doctorName={room.doctorName}
        patientName={room.patientName}
        notes={state.notes || room.notes || ''}
        elapsedSeconds={state.elapsedSeconds}
        onSave={updateNotes}
      />
    );
  }

  if (isLobby) {
    return (
      <PreCallLobby
        role={role}
        doctorName={room.doctorName}
        patientName={room.patientName}
        reason={room.reason}
        participants={state.participants}
        onJoin={joinRoom}
      />
    );
  }

  const otherName = role === 'Patient' ? room.doctorName : room.patientName;
  const sidebarW = state.localControls.sidebarOpen && role === 'Doctor' ? 400 : 0;

  return (
    <div className="vc-stage">
      {/* Video area */}
      <div style={{ position: 'absolute', left: 0, top: 0, right: sidebarW, bottom: 0 }}>
        {/* Full-bleed remote video */}
        <div className="vc-stage__video">
          <VideoTile name={otherName} size="lg" />
        </div>

        {/* Vignette for chrome readability */}
        <div className="vc-stage__vignette" />

        {/* Top info */}
        <div style={{
          position: 'absolute', top: 14, left: 14, right: 14,
          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
          zIndex: 5,
        }}>
          <div className="vc-info-pill vc-info-pill--glow">
            <div style={{
              width: 6, height: 6, borderRadius: '50%',
              background: 'var(--accent)',
              boxShadow: '0 0 6px rgba(196,255,61,0.7)',
            }} />
            With <strong style={{ fontWeight: 600, marginLeft: 2 }}>{otherName}</strong>
          </div>
        </div>

        {/* Self PIP */}
        <div className="vc-stage__pip">
          <VideoTile name={fullName} size="pip" isMuted={!state.localControls.mic} />
        </div>

        {/* Chat drawer */}
        {state.localControls.chatOpen && (
          <ChatDrawer
            messages={state.chatMessages}
            currentUserName={fullName}
            onSend={sendChat}
            onClose={toggleChat}
          />
        )}

        {/* Control bar */}
        <ControlPill
          role={role}
          controls={state.localControls}
          elapsedSeconds={state.elapsedSeconds}
          onToggleMic={toggleMic}
          onToggleCam={toggleCam}
          onToggleChat={toggleChat}
          onToggleSidebar={toggleSidebar}
          onEndCall={endConsultation}
        />
      </div>

      {/* Clinical sidebar */}
      {state.localControls.sidebarOpen && role === 'Doctor' && (
        <ClinicalSidebar
          notes={state.notes}
          patientName={room.patientName}
          onUpdateNotes={updateNotes}
          onClose={toggleSidebar}
        />
      )}
    </div>
  );
}
