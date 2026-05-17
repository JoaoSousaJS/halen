import { useState } from "react";

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
    <aside
      className="clinical-sidebar"
      style={{
        width: 400,
        position: "fixed",
        top: 0,
        right: 0,
        bottom: 0,
        display: "flex",
        flexDirection: "column",
      }}
    >
      <header className="clinical-sidebar__header">
        <span className="clinical-sidebar__patient-name">{patientName}</span>
        <button
          className="clinical-sidebar__close"
          onClick={onClose}
          aria-label="Close sidebar"
        >
          Close
        </button>
      </header>

      <div className="clinical-sidebar__tabs" role="tablist" aria-label="Clinical tabs">
        {TABS.map((tab) => (
          <button
            key={tab}
            role="tab"
            aria-label={tab}
            aria-selected={activeTab === tab}
            className={`clinical-sidebar__tab${activeTab === tab ? " clinical-sidebar__tab--active" : ""}`}
            onClick={() => setActiveTab(tab)}
          >
            {tab}
          </button>
        ))}
      </div>

      <div className="clinical-sidebar__content" role="tabpanel">
        {activeTab === "Summary" && <p>No conditions recorded</p>}
        {activeTab === "History" && <p>No previous consultations</p>}
        {activeTab === "Vitals" && <p>No vitals recorded</p>}
        {activeTab === "Notes" && (
          <textarea
            role="textbox"
            className="clinical-sidebar__notes"
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
