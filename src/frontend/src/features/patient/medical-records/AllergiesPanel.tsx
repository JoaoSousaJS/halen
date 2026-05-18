import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, Field, Select, Chip } from '../../../shared/components';
import { getPatientAllergies, addAllergy } from '../../../shared/api/medical-records';
import type { AllergyDto, AddAllergyPayload } from '../../../shared/api/medical-records';

interface AllergiesPanelProps {
  patientProfileId: string;
}

const SEVERITY_OPTIONS = [
  { value: 'Mild', label: 'Mild' },
  { value: 'Moderate', label: 'Moderate' },
  { value: 'Severe', label: 'Severe' },
];

const SEVERITY_VARIANT: Record<string, 'good' | 'warn' | 'danger'> = {
  Mild: 'good',
  Moderate: 'warn',
  Severe: 'danger',
};

function AllergyCard({ allergy }: { allergy: AllergyDto }) {
  return (
    <article className="panel-item" aria-label={allergy.allergenName}>
      <div className="panel-item-header">
        <h3>{allergy.allergenName}</h3>
        {allergy.reaction && <p className="panel-item-reaction">{allergy.reaction}</p>}
      </div>
      <div className="panel-item-badges">
        <Chip
          status={allergy.severity}
          variant={SEVERITY_VARIANT[allergy.severity]}
        />
        <Chip
          status={allergy.isActive ? 'Active' : 'Inactive'}
          variant={allergy.isActive ? 'warn' : undefined}
        />
      </div>
      {allergy.dateIdentified && (
        <p className="panel-item-meta text-dim">
          Identified: {new Date(allergy.dateIdentified).toLocaleDateString()}
        </p>
      )}
    </article>
  );
}

export default function AllergiesPanel({
  patientProfileId,
}: AllergiesPanelProps) {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState<Partial<AddAllergyPayload>>({
    severity: 'Moderate',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const allergies = useQuery({
    queryKey: ['patient-allergies', patientProfileId],
    queryFn: () => getPatientAllergies(patientProfileId),
  });

  const add = useMutation({
    mutationFn: (data: AddAllergyPayload) =>
      addAllergy(patientProfileId, data),
    onSuccess: () => {
      setShowForm(false);
      setFormData({ severity: 'Moderate' });
      setErrors({});
      queryClient.invalidateQueries({
        queryKey: ['patient-allergies', patientProfileId],
      });
      queryClient.invalidateQueries({
        queryKey: ['patient-timeline', patientProfileId],
      });
    },
  });

  function validate(): boolean {
    const newErrors: Record<string, string> = {};
    if (!formData.allergenName?.trim()) {
      newErrors.allergenName = 'Allergen name is required';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!validate()) return;
    add.mutate({
      allergenName: formData.allergenName!,
      reaction: formData.reaction,
      severity: formData.severity ?? 'Moderate',
      dateIdentified: formData.dateIdentified,
    });
  }

  if (allergies.isLoading) {
    return <p role="status">Loading allergies...</p>;
  }

  if (allergies.isError) {
    return <p className="auth-error">Failed to load allergies.</p>;
  }

  const data = allergies.data ?? [];

  return (
    <section className="panel" aria-label="Allergies">
      <div className="panel-header">
        <h2>Allergies</h2>
        <Button
          ariaLabel="Add allergy"
          onClick={() => setShowForm(!showForm)}
        >
          {showForm ? 'Cancel' : 'Add Allergy'}
        </Button>
      </div>

      {showForm && (
        <form className="panel-form" onSubmit={handleSubmit} aria-label="Add allergy form">
          <Field label="Allergen Name">
            <input
              id="allergen-name"
              aria-label="Allergen Name"
              required
              value={formData.allergenName ?? ''}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  allergenName: e.target.value,
                }))
              }
            />
          </Field>
          {errors.allergenName && (
            <p className="auth-error" role="alert">
              {errors.allergenName}
            </p>
          )}

          <Field label="Reaction">
            <input
              id="reaction"
              aria-label="Reaction"
              value={formData.reaction ?? ''}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  reaction: e.target.value || undefined,
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
                setFormData((prev) => ({ ...prev, severity: e.target.value as AddAllergyPayload['severity'] }))
              }
            />
          </Field>

          <Field label="Date Identified">
            <input
              type="date"
              id="date-identified"
              aria-label="Date Identified"
              value={formData.dateIdentified ?? ''}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  dateIdentified: e.target.value || undefined,
                }))
              }
            />
          </Field>

          {add.isError && (
            <p className="auth-error">Failed to add allergy.</p>
          )}

          <Button variant="primary" type="submit" disabled={add.isPending}>
            {add.isPending ? 'Saving...' : 'Save'}
          </Button>
        </form>
      )}

      {data.length === 0 && !showForm ? (
        <p className="panel-empty text-dim">No allergies recorded yet.</p>
      ) : (
        <div className="panel-list" role="list" aria-label="Allergies list">
          {data.map((allergy) => (
            <div key={allergy.id} role="listitem">
              <AllergyCard allergy={allergy} />
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
