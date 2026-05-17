import { useState } from "react";
import { getInitials } from "../utils";
import "../video-consultation.css";

const TABS = ["Summary", "History", "Vitals", "Notes", "Rx", "Refer"] as const;
type Tab = (typeof TABS)[number];

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
  const [activeTab, setActiveTab] = useState<Tab>("Summary");

  return (
    <aside className="vc-sidebar">
      <header className="vc-sidebar__header">
        <div className="vc-sidebar__avatar">{getInitials(patientName)}</div>
        <span>{patientName}</span>
        <button onClick={onClose} aria-label="Close sidebar">
          ✕
        </button>
      </header>

      <div className="vc-sidebar__tabs" role="tablist" aria-label="Clinical tabs">
        {TABS.map((tab) => (
          <button
            key={tab}
            role="tab"
            aria-label={tab}
            aria-selected={activeTab === tab}
            className={activeTab === tab ? "vc-sidebar__tab--active" : undefined}
            onClick={() => setActiveTab(tab)}
          >
            {tab}
          </button>
        ))}
      </div>

      <div className="vc-sidebar__content" role="tabpanel">
        {activeTab === "Summary" && <p>No conditions recorded</p>}
        {activeTab === "History" && <p>No previous consultations</p>}
        {activeTab === "Vitals" && <p>No vitals recorded</p>}
        {activeTab === "Notes" && (
          <textarea
            role="textbox"
            className="vc-sidebar__notes"
            value={notes}
            onChange={(e) => onUpdateNotes(e.target.value)}
          />
        )}
        {activeTab === "Rx" && <p>No prescriptions</p>}
        {activeTab === "Refer" && <p>No referrals</p>}
      </div>
    </aside>
  );
}
