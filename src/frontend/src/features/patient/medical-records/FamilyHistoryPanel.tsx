import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getApiError } from '../../../shared/api/errors';
import {
  getPatientFamilyHistory,
  addFamilyHistory,
} from '../../../shared/api/medical-records';
import type { FamilyHistoryDto, AddFamilyHistoryPayload } from '../../../shared/api/medical-records';
import { Button, Field, Input, Select, Dialog, DialogActions } from '../../../shared/components';

interface FamilyHistoryPanelProps {
  patientProfileId: string;
}

const RELATIONSHIP_OPTIONS = [
  { value: 'Mother', label: 'Mother' },
  { value: 'Father', label: 'Father' },
  { value: 'Sister', label: 'Sister' },
  { value: 'Brother', label: 'Brother' },
  { value: 'Grandmother', label: 'Grandmother' },
  { value: 'Grandfather', label: 'Grandfather' },
  { value: 'Aunt', label: 'Aunt' },
  { value: 'Uncle', label: 'Uncle' },
  { value: 'Other', label: 'Other' },
];

export default function FamilyHistoryPanel({ patientProfileId }: FamilyHistoryPanelProps) {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);

  const [relationship, setRelationship] = useState('');
  const [conditionName, setConditionName] = useState('');
  const [ageAtOnset, setAgeAtOnset] = useState('');
  const [notes, setNotes] = useState('');

  const familyHistory = useQuery({
    queryKey: ['patient-family-history', patientProfileId],
    queryFn: () => getPatientFamilyHistory(patientProfileId),
  });

  const add = useMutation({
    mutationFn: (payload: AddFamilyHistoryPayload) => addFamilyHistory(patientProfileId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patient-family-history', patientProfileId] });
      resetForm();
    },
  });

  function resetForm() {
    setRelationship('');
    setConditionName('');
    setAgeAtOnset('');
    setNotes('');
    setShowForm(false);
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    add.mutate({
      relationship,
      conditionName,
      ageAtOnset: ageAtOnset ? Number(ageAtOnset) : undefined,
      notes: notes || undefined,
    });
  }

  if (familyHistory.isLoading) {
    return <p className="text-dim" role="status">Loading family history...</p>;
  }

  if (familyHistory.isError) {
    return <p className="auth-error">Failed to load family history.</p>;
  }

  const items = familyHistory.data ?? [];

  return (
    <section className="panel" aria-label="Family History">
      <div className="panel-header">
        <h3>Family History</h3>
        <Button size="sm" onClick={() => setShowForm(true)}>Add Entry</Button>
      </div>

      {items.length === 0 ? (
        <p className="panel-empty text-dim">No family history recorded.</p>
      ) : (
        <ul className="record-list" role="list">
          {items.map((entry: FamilyHistoryDto) => (
            <li key={entry.id} className="record-card">
              <div className="record-card-header">
                <strong>{entry.conditionName}</strong>
                <span className="text-dim">{entry.relationship}</span>
              </div>
              <div className="record-card-body">
                {entry.ageAtOnset != null ? (
                  <span>Age at onset: {entry.ageAtOnset}</span>
                ) : null}
                {entry.notes ? (
                  <p className="text-dim">{entry.notes}</p>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      )}

      {showForm ? (
        <Dialog title="Add Family History Entry" onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} aria-label="Add family history form">
            <Field label="Relationship">
              <Select
                required
                value={relationship}
                onChange={(e) => setRelationship(e.target.value)}
                options={RELATIONSHIP_OPTIONS}
                placeholder="Select relationship"
              />
            </Field>

            <Field label="Condition Name">
              <Input
                required
                value={conditionName}
                onChange={(e) => setConditionName(e.target.value)}
                placeholder="e.g. Type 2 Diabetes"
              />
            </Field>

            <Field label="Age at Onset">
              <Input
                type="number"
                min={0}
                max={150}
                value={ageAtOnset}
                onChange={(e) => setAgeAtOnset(e.target.value)}
                placeholder="Optional"
              />
            </Field>

            <Field label="Notes">
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
                placeholder="Additional details (optional)"
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
                {add.isPending ? 'Saving...' : 'Save Entry'}
              </Button>
            </DialogActions>
          </form>
        </Dialog>
      ) : null}
    </section>
  );
}
