import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../../../shared/components/AuthProvider';
import { DashboardShell } from '../../../shared/components/DashboardShell';
import { FeatureGate } from '../../../shared/components/FeatureGate';
import { Chip } from '../../../shared/components';
import { getPatientHeader } from '../../../shared/api/medical-records';
import type { PatientHeaderDto } from '../../../shared/api/medical-records';
import PatientTimeline from './PatientTimeline';
import PatientSnapshot from './PatientSnapshot';
import ConditionsPanel from './ConditionsPanel';
import AllergiesPanel from './AllergiesPanel';
import VitalsPanel from './VitalsPanel';
import MedicationsPanel from './MedicationsPanel';
import FamilyHistoryPanel from './FamilyHistoryPanel';
import DocumentsPanel from './DocumentsPanel';

const TABS = [
  'Timeline',
  'Snapshot',
  'Conditions',
  'Allergies',
  'Vitals',
  'Medications',
  'Family History',
  'Documents',
] as const;

type TabName = (typeof TABS)[number];

interface MedicalRecordsPageProps {
  /** Passed directly in tests; falls back to URL param when rendered via route. */
  patientProfileId?: string;
}

function PatientHeader({ header }: { header: PatientHeaderDto }) {
  return (
    <section className="patient-header" aria-label="Patient header">
      <div className="patient-header-info">
        <h1>{header.patientName}</h1>
        {header.city && <p className="patient-header-city">{header.city}</p>}
      </div>
      <div className="patient-header-chips" aria-label="Allergies and conditions">
        {header.allergyChips.map((a) => (
          <Chip key={a} status={a} variant="danger" />
        ))}
        {header.conditionChips.map((c) => (
          <Chip key={c} status={c} variant="warn" />
        ))}
      </div>
    </section>
  );
}

function TabPanel({
  tab,
  patientProfileId,
}: {
  tab: TabName;
  patientProfileId: string;
}) {
  switch (tab) {
    case 'Timeline':
      return <PatientTimeline patientProfileId={patientProfileId} />;
    case 'Snapshot':
      return <PatientSnapshot patientProfileId={patientProfileId} />;
    case 'Conditions':
      return <ConditionsPanel patientProfileId={patientProfileId} />;
    case 'Allergies':
      return <AllergiesPanel patientProfileId={patientProfileId} />;
    case 'Vitals':
      return <VitalsPanel patientProfileId={patientProfileId} />;
    case 'Medications':
      return <MedicationsPanel patientProfileId={patientProfileId} />;
    case 'Family History':
      return <FamilyHistoryPanel patientProfileId={patientProfileId} />;
    case 'Documents':
      return <DocumentsPanel patientProfileId={patientProfileId} />;
    default:
      return null;
  }
}

export default function MedicalRecordsPage({
  patientProfileId: propId,
}: MedicalRecordsPageProps) {
  const { patientProfileId: paramId } = useParams<{ patientProfileId: string }>();
  const patientProfileId = propId ?? paramId ?? '';
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<TabName>('Timeline');

  const header = useQuery({
    queryKey: ['patient-header', patientProfileId],
    queryFn: () => getPatientHeader(patientProfileId),
  });

  return (
    <FeatureGate feature="medical_records">
      <DashboardShell
        subtitle="medical records"
        userName={`${user?.given_name} ${user?.family_name}`}
      >
        <div className="medical-records-page">
          {header.isLoading ? (
            <p role="status">Loading patient records...</p>
          ) : header.isError ? (
            <p className="auth-error">Failed to load patient data.</p>
          ) : header.data ? (
            <>
              <PatientHeader header={header.data} />

              <nav className="records-tabs" aria-label="Medical records tabs">
                <div role="tablist" aria-label="Medical record sections">
                  {TABS.map((tab) => (
                    <button
                      key={tab}
                      role="tab"
                      className={`records-tab${activeTab === tab ? ' records-tab-active' : ''}`}
                      aria-selected={activeTab === tab}
                      aria-controls={`tabpanel-${tab}`}
                      onClick={() => setActiveTab(tab)}
                    >
                      {tab}
                    </button>
                  ))}
                </div>
              </nav>

              <div
                className="records-panel"
                id={`tabpanel-${activeTab}`}
                role="tabpanel"
                aria-label={activeTab}
              >
                <TabPanel tab={activeTab} patientProfileId={patientProfileId} />
              </div>
            </>
          ) : null}
        </div>
      </DashboardShell>
    </FeatureGate>
  );
}
