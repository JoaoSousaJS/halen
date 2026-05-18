import { useState } from 'react';
import { getInitials } from '../utils';
import '../video-consultation.css';

const TABS = ['Summary', 'History', 'Vitals', 'Notes', 'Rx', 'Refer'] as const;
type Tab = (typeof TABS)[number];

function hashHue(name: string): number {
  let h = 0;
  for (let i = 0; i < name.length; i++) {
    h = (h * 31 + name.charCodeAt(i)) >>> 0;
  }
  return h % 360;
}

export function ClinicalSidebar({
  notes,
  patientName,
  onUpdateNotes,
  onClose,
}: {
  notes: string;
  patientName: string;
  onUpdateNotes: (notes: string) => void;
  onClose: () => void;
}) {
  const [activeTab, setActiveTab] = useState<Tab>('Summary');
  const hue = hashHue(patientName);

  return (
    <aside className="vc-sidebar">
      <header className="vc-sidebar__header">
        <div
          className="vc-sidebar__avatar"
          style={{ background: `oklch(0.32 0.08 ${hue})` }}
        >
          {getInitials(patientName)}
        </div>
        <div className="vc-sidebar__header-info">
          <strong>{patientName}</strong>
        </div>
        <button onClick={onClose} aria-label="Close sidebar">
          ×
        </button>
      </header>

      <div className="vc-sidebar__tabs" role="tablist" aria-label="Clinical tabs">
        {TABS.map((tab) => (
          <button
            key={tab}
            role="tab"
            aria-label={tab}
            aria-selected={activeTab === tab}
            className={activeTab === tab ? 'vc-sidebar__tab--active' : undefined}
            onClick={() => setActiveTab(tab)}
          >
            {tab}
          </button>
        ))}
      </div>

      <div className="vc-sidebar__content" role="tabpanel">
        {activeTab === 'Summary' && <p>No conditions recorded</p>}
        {activeTab === 'History' && <p>No previous consultations</p>}
        {activeTab === 'Vitals' && <p>No vitals recorded</p>}
        {activeTab === 'Notes' && (
          <textarea
            role="textbox"
            className="vc-sidebar__notes"
            value={notes}
            onChange={(e) => onUpdateNotes(e.target.value)}
          />
        )}
        {activeTab === 'Rx' && <p>No prescriptions</p>}
        {activeTab === 'Refer' && <p>No referrals</p>}
      </div>

      <div className="vc-sidebar__footer">
        <button className="btn btn-ghost btn-sm" style={{ flex: 1 }}>
          Save draft
        </button>
        <button className="btn btn-primary btn-sm" style={{ flex: 1 }}>
          Mark complete
        </button>
      </div>
    </aside>
  );
}
