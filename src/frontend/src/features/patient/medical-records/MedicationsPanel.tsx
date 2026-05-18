import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getApiError } from '../../../shared/api/errors';
import {
  getPatientMedications,
  addMedication,
} from '../../../shared/api/medical-records';
import type { MedicationDto, AddMedicationPayload } from '../../../shared/api/medical-records';
import { Button, Field, Input, Dialog, DialogActions, Chip } from '../../../shared/components';

interface MedicationsPanelProps {
  patientProfileId: string;
}

const FREQUENCY_OPTIONS = ['Once daily', 'Twice daily', 'Three times daily', 'As needed', 'Weekly'];

export default function MedicationsPanel({ patientProfileId }: MedicationsPanelProps) {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);

  const [name, setName] = useState('');
  const [dosage, setDosage] = useState('');
  const [frequency, setFrequency] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [prescribedBy, setPrescribedBy] = useState('');

  const medications = useQuery({
    queryKey: ['patient-medications', patientProfileId],
    queryFn: () => getPatientMedications(patientProfileId),
  });

  const add = useMutation({
    mutationFn: (payload: AddMedicationPayload) => addMedication(patientProfileId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patient-medications', patientProfileId] });
      resetForm();
    },
  });

  function resetForm() {
    setName('');
    setDosage('');
    setFrequency('');
    setStartDate('');
    setEndDate('');
    setPrescribedBy('');
    setShowForm(false);
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    add.mutate({
      medicationName: name,
      dosage,
      frequency,
      startDate,
      endDate: endDate || undefined,
      prescribedByName: prescribedBy || undefined,
    });
  }

  if (medications.isLoading) {
    return <p className="text-dim" role="status">Loading medications...</p>;
  }

  if (medications.isError) {
    return <p className="auth-error">Failed to load medications.</p>;
  }

  const items = medications.data ?? [];

  return (
    <section className="panel" aria-label="Medications">
      <div className="panel-header">
        <h3>Medications</h3>
        <Button size="sm" onClick={() => setShowForm(true)}>Add Medication</Button>
      </div>

      {items.length === 0 ? (
        <p className="panel-empty text-dim">No medications recorded.</p>
      ) : (
        <ul className="record-list" role="list">
          {items.map((med: MedicationDto) => (
            <li key={med.id} className="record-card">
              <div className="record-card-header">
                <strong>{med.medicationName}</strong>
                <Chip
                  status={med.isActive ? 'Active' : 'Inactive'}
                  variant={med.isActive ? 'good' : undefined}
                />
              </div>
              <div className="record-card-body">
                <span>Dosage: {med.dosage}</span>
                <span>Frequency: {med.frequency}</span>
                {med.startDate ? (
                  <span>Start: {new Date(med.startDate).toLocaleDateString()}</span>
                ) : null}
                {med.endDate ? (
                  <span>End: {new Date(med.endDate).toLocaleDateString()}</span>
                ) : null}
                {med.prescribedByName ? (
                  <span className="text-dim">Prescribed by: {med.prescribedByName}</span>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      )}

      {showForm ? (
        <Dialog title="Add Medication" onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} aria-label="Add medication form">
            <Field label="Medication Name">
              <Input
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Lisinopril"
              />
            </Field>

            <Field label="Dosage">
              <Input
                required
                value={dosage}
                onChange={(e) => setDosage(e.target.value)}
                placeholder="e.g. 10mg"
              />
            </Field>

            <Field label="Frequency">
              <Input
                required
                value={frequency}
                onChange={(e) => setFrequency(e.target.value)}
                placeholder="e.g. Once daily"
                list="frequency-suggestions"
              />
              <datalist id="frequency-suggestions">
                {FREQUENCY_OPTIONS.map((f) => (
                  <option key={f} value={f} />
                ))}
              </datalist>
            </Field>

            <Field label="Start Date">
              <input
                type="date"
                required
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
              />
            </Field>

            <Field label="End Date">
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
              />
            </Field>

            <Field label="Prescribed By">
              <Input
                value={prescribedBy}
                onChange={(e) => setPrescribedBy(e.target.value)}
                placeholder="Doctor name (optional)"
              />
            </Field>

            {add.isError ? (
              <p className="auth-error">{getApiError(add.error)}</p>
            ) : null}

            <DialogActions>
              <Button variant="ghost" type="button" onClick={() => setShowForm(false)}>
                Cancel
              </Button>
              <Button variant="primary" type="submit" disabled={add.isPending}>
                {add.isPending ? 'Saving...' : 'Save Medication'}
              </Button>
            </DialogActions>
          </form>
        </Dialog>
      ) : null}
    </section>
  );
}
