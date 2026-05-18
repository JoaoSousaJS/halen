import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, Field, Select, Chip } from '../../../shared/components';
import { getPatientConditions, addCondition } from '../../../shared/api/medical-records';
import type { ConditionDto, AddConditionPayload } from '../../../shared/api/medical-records';

interface ConditionsPanelProps {
  patientProfileId: string;
}

const SEVERITY_OPTIONS = [
  { value: 'Mild', label: 'Mild' },
  { value: 'Moderate', label: 'Moderate' },
  { value: 'Severe', label: 'Severe' },
];

const STATUS_OPTIONS = [
  { value: 'Active', label: 'Active' },
  { value: 'InRemission', label: 'In Remission' },
  { value: 'Resolved', label: 'Resolved' },
];

const SEVERITY_VARIANT: Record<string, 'good' | 'warn' | 'danger'> = {
  Mild: 'good',
  Moderate: 'warn',
  Severe: 'danger',
};

const STATUS_VARIANT: Record<string, 'good' | 'warn' | 'danger'> = {
  Active: 'warn',
  InRemission: 'good',
  Resolved: 'good',
};

function ConditionCard({ condition }: { condition: ConditionDto }) {
  return (
    <article className="panel-item" aria-label={condition.icdDescription}>
      <div className="panel-item-header">
        <code className="panel-item-code">{condition.icdCode}</code>
        <h3>{condition.icdDescription}</h3>
      </div>
      <div className="panel-item-badges">
        <Chip status={condition.severity} variant={SEVERITY_VARIANT[condition.severity]} />
        <Chip status={condition.status} variant={STATUS_VARIANT[condition.status]} />
      </div>
      {condition.dateOfOnset && (
        <p className="panel-item-meta text-dim">
          Onset: {new Date(condition.dateOfOnset).toLocaleDateString()}
        </p>
      )}
      {condition.clinicalNotes && <p className="panel-item-notes">{condition.clinicalNotes}</p>}
    </article>
  );
}

export default function ConditionsPanel({
  patientProfileId,
}: ConditionsPanelProps) {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState<Partial<AddConditionPayload>>({
    severity: 'Moderate',
    status: 'Active',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const conditions = useQuery({
    queryKey: ['patient-conditions', patientProfileId],
    queryFn: () => getPatientConditions(patientProfileId),
  });

  const add = useMutation({
    mutationFn: (data: AddConditionPayload) =>
      addCondition(patientProfileId, data),
    onSuccess: () => {
      setShowForm(false);
      setFormData({ severity: 'Moderate', status: 'Active' });
      setErrors({});
      queryClient.invalidateQueries({
        queryKey: ['patient-conditions', patientProfileId],
      });
      queryClient.invalidateQueries({
        queryKey: ['patient-timeline', patientProfileId],
      });
    },
  });

  function validate(): boolean {
    const newErrors: Record<string, string> = {};
    if (!formData.icdCode?.trim()) {
      newErrors.icdCode = 'ICD code is required';
    }
    if (!formData.icdDescription?.trim()) {
      newErrors.icdDescription = 'Description is required';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!validate()) return;
    add.mutate({
      icdCode: formData.icdCode!,
      icdDescription: formData.icdDescription!,
      dateOfOnset: formData.dateOfOnset,
      severity: formData.severity ?? 'Moderate',
      status: formData.status ?? 'Active',
      clinicalNotes: formData.clinicalNotes,
    });
  }

  if (conditions.isLoading) {
    return <p role="status">Loading conditions...</p>;
  }

  if (conditions.isError) {
    return <p className="auth-error">Failed to load conditions.</p>;
  }

  const data = conditions.data ?? [];

  return (
    <section className="panel" aria-label="Conditions">
      <div className="panel-header">
        <h2>Conditions</h2>
        <Button
          ariaLabel="Add condition"
          onClick={() => setShowForm(!showForm)}
        >
          {showForm ? 'Cancel' : 'Add Condition'}
        </Button>
      </div>

      {showForm && (
        <form className="panel-form" onSubmit={handleSubmit} aria-label="Add condition form">
          <Field label="ICD Code">
            <input
              id="icd-code"
              aria-label="ICD Code"
              required
              value={formData.icdCode ?? ''}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, icdCode: e.target.value }))
              }
            />
          </Field>
          {errors.icdCode && (
            <p className="auth-error" role="alert">
              {errors.icdCode}
            </p>
          )}

          <Field label="Description">
            <input
              id="description"
              aria-label="Description"
              required
              value={formData.icdDescription ?? ''}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  icdDescription: e.target.value,
                }))
              }
            />
          </Field>
          {errors.icdDescription && (
            <p className="auth-error" role="alert">
              {errors.icdDescription}
            </p>
          )}

          <Field label="Date of Onset">
            <input
              type="date"
              id="date-of-onset"
              aria-label="Date of Onset"
              value={formData.dateOfOnset ?? ''}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  dateOfOnset: e.target.value || undefined,
                }))
              }
            />
          </Field>

          <Field label="Severity">
            <Select
              options={SEVERITY_OPTIONS}
              value={formData.severity ?? 'Moderate'}
              aria-label="Severity"
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, severity: e.target.value as AddConditionPayload['severity'] }))
              }
            />
          </Field>

          <Field label="Status">
            <Select
              options={STATUS_OPTIONS}
              value={formData.status ?? 'Active'}
              aria-label="Status"
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, status: e.target.value as AddConditionPayload['status'] }))
              }
            />
          </Field>

          <Field label="Clinical Notes">
            <textarea
              id="clinical-notes"
              aria-label="Clinical Notes"
              rows={3}
              value={formData.clinicalNotes ?? ''}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  clinicalNotes: e.target.value || undefined,
                }))
              }
            />
          </Field>

          {add.isError && (
            <p className="auth-error">Failed to add condition.</p>
          )}

          <Button variant="primary" type="submit" disabled={add.isPending}>
            {add.isPending ? 'Saving...' : 'Save'}
          </Button>
        </form>
      )}

      {data.length === 0 && !showForm ? (
        <p className="panel-empty text-dim">No conditions recorded yet.</p>
      ) : (
        <div className="panel-list" role="list" aria-label="Conditions list">
          {data.map((condition) => (
            <div key={condition.id} role="listitem">
              <ConditionCard condition={condition} />
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
