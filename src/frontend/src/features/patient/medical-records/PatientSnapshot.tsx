import { useQuery } from '@tanstack/react-query';
import { Chip } from '../../../shared/components';
import { getPatientSnapshot } from '../../../shared/api/medical-records';
import type { PatientSnapshotDto, LatestVitalsDto } from '../../../shared/api/medical-records';

interface PatientSnapshotProps {
  patientProfileId: string;
}

const ONBOARDING_TOTAL = 6;

function OnboardingProgress({ progress }: { progress: number }) {
  const percent = Math.round((progress / ONBOARDING_TOTAL) * 100);

  return (
    <div aria-label="Onboarding progress">
      <p>
        Profile completion: {progress} of {ONBOARDING_TOTAL}
      </p>
      <div
        role="progressbar"
        aria-valuenow={progress}
        aria-valuemin={0}
        aria-valuemax={ONBOARDING_TOTAL}
        aria-label={`${progress} of ${ONBOARDING_TOTAL} sections complete`}
      >
        <div style={{ width: `${percent}%` }} />
      </div>
    </div>
  );
}

function SnapshotCard({
  title,
  emptyMessage,
  children,
  count,
}: {
  title: string;
  emptyMessage: string;
  children: React.ReactNode;
  count: number;
}) {
  return (
    <article aria-label={title}>
      <header>
        <h3>{title}</h3>
        <span className="text-dim">{count}</span>
      </header>
      {count === 0 ? (
        <p className="text-dim">Get started by adding your first {emptyMessage}.</p>
      ) : (
        children
      )}
    </article>
  );
}

function formatVital(vitals: LatestVitalsDto, key: keyof LatestVitalsDto, label: string) {
  const reading = vitals[key];
  if (!reading) return null;
  const display = reading.secondaryValue != null
    ? `${reading.value}/${reading.secondaryValue}`
    : String(reading.value);
  return (
    <li key={key}>
      <span>{label}</span>
      <span>{display}</span>
      <span className="text-dim">{reading.unit}</span>
    </li>
  );
}

export default function PatientSnapshot({
  patientProfileId,
}: PatientSnapshotProps) {
  const snapshot = useQuery({
    queryKey: ['patient-snapshot', patientProfileId],
    queryFn: () => getPatientSnapshot(patientProfileId),
  });

  if (snapshot.isLoading) {
    return <p role="status">Loading snapshot...</p>;
  }

  if (snapshot.isError) {
    return <p className="auth-error">Failed to load snapshot.</p>;
  }

  const data = snapshot.data as PatientSnapshotDto | undefined;
  if (!data) return null;

  const vitalsCount = data.latestVitals
    ? Object.values(data.latestVitals).filter(Boolean).length
    : 0;

  return (
    <section aria-label="Patient snapshot">
      <OnboardingProgress progress={data.onboardingProgress} />

      <div aria-label="Snapshot cards">
        <SnapshotCard
          title="Active Conditions"
          emptyMessage="condition"
          count={data.activeConditions.length}
        >
          <ul>
            {data.activeConditions.map((c) => (
              <li key={c.id}>
                <span>{c.icdDescription}</span>
                <Chip status={c.severity} variant="warn" />
              </li>
            ))}
          </ul>
        </SnapshotCard>

        <SnapshotCard
          title="Allergies"
          emptyMessage="allergy"
          count={data.allergies.length}
        >
          <ul>
            {data.allergies.map((a) => (
              <li key={a.id}>
                <span>{a.allergenName}</span>
                <Chip status={a.severity} variant="danger" />
              </li>
            ))}
          </ul>
        </SnapshotCard>

        <SnapshotCard
          title="Current Medications"
          emptyMessage="medication"
          count={data.activeMedications.length}
        >
          <ul>
            {data.activeMedications.map((m) => (
              <li key={m.id}>
                <span>{m.medicationName}</span>
                <span className="text-dim">{m.dosage}</span>
              </li>
            ))}
          </ul>
        </SnapshotCard>

        <SnapshotCard
          title="Family History"
          emptyMessage="family history entry"
          count={data.familyHistory.length}
        >
          <ul>
            {data.familyHistory.map((f) => (
              <li key={f.id}>
                <span>{f.conditionName}</span>
                <span className="text-dim">{f.relationship}</span>
              </li>
            ))}
          </ul>
        </SnapshotCard>

        <SnapshotCard
          title="Latest Vitals"
          emptyMessage="vital reading"
          count={vitalsCount}
        >
          {data.latestVitals && (
            <ul>
              {formatVital(data.latestVitals, 'bloodPressure', 'Blood Pressure')}
              {formatVital(data.latestVitals, 'heartRate', 'Heart Rate')}
              {formatVital(data.latestVitals, 'weight', 'Weight')}
              {formatVital(data.latestVitals, 'spO2', 'SpO2')}
            </ul>
          )}
        </SnapshotCard>
      </div>
    </section>
  );
}
