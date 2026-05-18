import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';
import { Button, Field, Select } from '../../../shared/components';
import {
  getPatientSnapshot,
  getPatientVitalsHistory,
  addVital,
} from '../../../shared/api/medical-records';
import type {
  VitalType,
  VitalSource,
  AddVitalPayload,
  VitalReadingDetailDto,
} from '../../../shared/api/medical-records';

interface VitalsPanelProps {
  patientProfileId: string;
}

const VITAL_TYPES: { value: VitalType; label: string }[] = [
  { value: 'BloodPressure', label: 'Blood Pressure' },
  { value: 'HeartRate', label: 'Heart Rate' },
  { value: 'Weight', label: 'Weight' },
  { value: 'SpO2', label: 'SpO2' },
  { value: 'Temperature', label: 'Temperature' },
  { value: 'BloodGlucose', label: 'Blood Glucose' },
];

const VITAL_UNITS: Record<VitalType, string> = {
  BloodPressure: 'mmHg',
  HeartRate: 'bpm',
  Weight: 'kg',
  SpO2: '%',
  Temperature: '°C',
  BloodGlucose: 'mg/dL',
};

const SOURCE_OPTIONS: { value: VitalSource; label: string }[] = [
  { value: 'Manual', label: 'Manual entry' },
  { value: 'Device', label: 'Device' },
  { value: 'ClinicalEntry', label: 'Clinical measurement' },
];

export default function VitalsPanel({ patientProfileId }: VitalsPanelProps) {
  const queryClient = useQueryClient();
  const [selectedType, setSelectedType] = useState<VitalType>('BloodPressure');
  const [showForm, setShowForm] = useState(false);
  const [formType, setFormType] = useState<VitalType>('BloodPressure');
  const [formValue, setFormValue] = useState('');
  const [formSecondary, setFormSecondary] = useState('');
  const [formUnit, setFormUnit] = useState(VITAL_UNITS.BloodPressure);
  const [formSource, setFormSource] = useState<VitalSource>('Manual');
  const [formNotes, setFormNotes] = useState('');

  const snapshot = useQuery({
    queryKey: ['patient-snapshot', patientProfileId],
    queryFn: () => getPatientSnapshot(patientProfileId),
  });

  const history = useQuery({
    queryKey: ['patient-vitals-history', patientProfileId, selectedType],
    queryFn: () => getPatientVitalsHistory(patientProfileId, selectedType),
    enabled: !!selectedType,
  });

  const add = useMutation({
    mutationFn: (data: AddVitalPayload) => addVital(patientProfileId, data),
    onSuccess: () => {
      setShowForm(false);
      resetForm();
      queryClient.invalidateQueries({
        queryKey: ['patient-snapshot', patientProfileId],
      });
      queryClient.invalidateQueries({
        queryKey: ['patient-vitals-history', patientProfileId],
      });
      queryClient.invalidateQueries({
        queryKey: ['patient-timeline', patientProfileId],
      });
    },
  });

  function resetForm() {
    setFormType('BloodPressure');
    setFormValue('');
    setFormSecondary('');
    setFormUnit(VITAL_UNITS.BloodPressure);
    setFormSource('Manual');
    setFormNotes('');
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const payload: AddVitalPayload = {
      vitalType: formType,
      value: parseFloat(formValue),
      unit: formUnit,
      measuredAt: new Date().toISOString(),
      source: formSource,
    };
    if (formSecondary) {
      payload.secondaryValue = parseFloat(formSecondary);
    }
    if (formNotes) {
      payload.notes = formNotes;
    }
    add.mutate(payload);
  }

  if (snapshot.isLoading) {
    return <p role="status">Loading vitals...</p>;
  }

  if (snapshot.isError) {
    return <p className="auth-error">Failed to load vitals.</p>;
  }

  const latestVitals = snapshot.data?.latestVitals;
  const historyData: VitalReadingDetailDto[] = history.data ?? [];

  // Build chart data from history
  const chartData = historyData.map((entry) => ({
    date: new Date(entry.measuredAt).toLocaleDateString(),
    value: entry.value,
  }));

  // Build latest vitals summary from the structured LatestVitalsDto
  const vitalsSummary: { type: string; display: string; unit: string }[] = [];
  if (latestVitals) {
    if (latestVitals.bloodPressure) {
      const bp = latestVitals.bloodPressure;
      vitalsSummary.push({
        type: 'Blood Pressure',
        display: bp.secondaryValue != null ? `${bp.value}/${bp.secondaryValue}` : String(bp.value),
        unit: bp.unit,
      });
    }
    if (latestVitals.heartRate) {
      vitalsSummary.push({
        type: 'Heart Rate',
        display: String(latestVitals.heartRate.value),
        unit: latestVitals.heartRate.unit,
      });
    }
    if (latestVitals.weight) {
      vitalsSummary.push({
        type: 'Weight',
        display: String(latestVitals.weight.value),
        unit: latestVitals.weight.unit,
      });
    }
    if (latestVitals.spO2) {
      vitalsSummary.push({
        type: 'SpO2',
        display: String(latestVitals.spO2.value),
        unit: latestVitals.spO2.unit,
      });
    }
  }

  return (
    <section className="panel" aria-label="Vitals">
      <div className="panel-header">
        <h2>Vitals</h2>
        <Button ariaLabel="Add vital" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : 'Add Vital'}
        </Button>
      </div>

      {/* Latest vitals summary */}
      {vitalsSummary.length > 0 && (
        <div className="vitals-current" aria-label="Latest vitals">
          {vitalsSummary.map((v) => (
            <div className="vital-current" key={v.type}>
              <span className="vital-current-label text-dim">{v.type}</span>
              <span className="vital-current-value">{v.display}</span>
              <span className="vital-current-unit text-dim">{v.unit}</span>
            </div>
          ))}
        </div>
      )}

      {/* Type selector */}
      <div className="vitals-type-selector">
        <Field label="Vital type">
          <Select
            options={VITAL_TYPES.map((t) => ({ value: t.value, label: t.label }))}
            value={selectedType}
            aria-label="Vital type"
            onChange={(e) => setSelectedType(e.target.value as VitalType)}
          />
        </Field>
      </div>

      {/* Chart */}
      {chartData.length > 0 && (
        <div className="vitals-chart" aria-label="Vitals chart">
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="date" stroke="var(--text-muted)" fontSize={11} fontFamily="var(--font-mono)" />
              <YAxis stroke="var(--text-muted)" fontSize={11} fontFamily="var(--font-mono)" />
              <Tooltip />
              <Line type="monotone" dataKey="value" stroke="var(--accent)" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* History list */}
      {history.isLoading && (
        <p className="text-dim" role="status">
          Loading history...
        </p>
      )}

      {historyData.length === 0 && !history.isLoading && (
        <p className="panel-empty text-dim">No history for this vital type yet.</p>
      )}

      {/* Add vital form */}
      {showForm && (
        <form className="panel-form" onSubmit={handleSubmit} aria-label="Add vital form">
          <Field label="Type">
            <Select
              options={VITAL_TYPES.map((t) => ({
                value: t.value,
                label: t.label,
              }))}
              value={formType}
              aria-label="Type"
              onChange={(e) => {
                const newType = e.target.value as VitalType;
                setFormType(newType);
                setFormUnit(VITAL_UNITS[newType]);
              }}
            />
          </Field>

          <Field label="Value">
            <input
              id="vital-value"
              aria-label="Value"
              type="number"
              step="any"
              required
              value={formValue}
              onChange={(e) => setFormValue(e.target.value)}
            />
          </Field>

          {formType === 'BloodPressure' && (
            <Field label="Diastolic (secondary value)">
              <input
                id="secondary-value"
                aria-label="Diastolic value"
                type="number"
                step="any"
                value={formSecondary}
                onChange={(e) => setFormSecondary(e.target.value)}
              />
            </Field>
          )}

          <Field label="Unit">
            <input
              id="vital-unit"
              aria-label="Unit"
              value={formUnit}
              onChange={(e) => setFormUnit(e.target.value)}
            />
          </Field>

          <Field label="Source">
            <Select
              options={SOURCE_OPTIONS}
              value={formSource}
              aria-label="Source"
              onChange={(e) => setFormSource(e.target.value as VitalSource)}
            />
          </Field>

          <Field label="Notes">
            <textarea
              id="vital-notes"
              aria-label="Notes"
              rows={2}
              value={formNotes}
              onChange={(e) => setFormNotes(e.target.value)}
            />
          </Field>

          {add.isError && (
            <p className="auth-error">Failed to add vital reading.</p>
          )}

          <Button variant="primary" type="submit" disabled={add.isPending}>
            {add.isPending ? 'Saving...' : 'Save'}
          </Button>
        </form>
      )}
    </section>
  );
}
