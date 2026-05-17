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
      <div className="vc-post-call">
        <p className="vc-post-call__detail">Connecting…</p>
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

  return (
    <div className="vc-active" style={{ background: 'var(--vc-surface-1, #0b0e0c)', minHeight: '100vh', position: 'relative' }}>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: 8, padding: 16, height: 'calc(100vh - 80px)' }}>
        <VideoTile name={otherName} size="lg" />
        <div style={{ position: 'fixed', bottom: 100, right: 16 }}>
          <VideoTile name={fullName} size="pip" isMuted={!state.localControls.mic} />
        </div>
      </div>

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

      {state.localControls.chatOpen && (
        <ChatDrawer
          messages={state.chatMessages}
          currentUserName={fullName}
          onSend={sendChat}
          onClose={toggleChat}
        />
      )}

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
