import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getClinic, updateClinic, setFeatureFlag } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';
import { Button, Field, Input, Chip } from '../../shared/components';

interface ClinicDetailPageProps {
  clinicId: string;
  onBack: () => void;
}

const FEATURE_LABELS: Record<string, string> = {
  prescriptions: 'Prescriptions',
  kyc: 'KYC Verification',
  video_calls: 'Video Calls',
  doctor_reviews: 'Doctor Reviews',
  medical_records: 'Medical Records',
  messaging: 'Messaging',
  audit_trail: 'Audit Trail',
};

export default function ClinicDetailPage({ clinicId, onBack }: ClinicDetailPageProps) {
  const queryClient = useQueryClient();

  const clinic = useQuery({
    queryKey: ['clinic', clinicId],
    queryFn: () => getClinic(clinicId),
  });

  const [editName, setEditName] = useState('');
  const [editActive, setEditActive] = useState(true);
  const [editing, setEditing] = useState(false);
  const [error, setError] = useState('');

  const update = useMutation({
    mutationFn: () => updateClinic(clinicId, { name: editName, isActive: editActive }),
    onSuccess: () => {
      setEditing(false);
      queryClient.invalidateQueries({ queryKey: ['clinic', clinicId] });
      queryClient.invalidateQueries({ queryKey: ['clinics'] });
    },
    onError: (err) => setError(getApiError(err)),
  });

  const [flagError, setFlagError] = useState('');

  const toggleFlag = useMutation({
    mutationFn: ({ featureKey, isEnabled }: { featureKey: string; isEnabled: boolean }) =>
      setFeatureFlag(clinicId, featureKey, isEnabled),
    onSuccess: () => {
      setFlagError('');
      queryClient.invalidateQueries({ queryKey: ['clinic', clinicId] });
    },
    onError: (err) => setFlagError(getApiError(err)),
  });

  function startEditing() {
    if (!clinic.data) return;
    setEditName(clinic.data.name);
    setEditActive(clinic.data.isActive);
    setEditing(true);
    setError('');
  }

  function handleSave(e: FormEvent) {
    e.preventDefault();
    update.mutate();
  }

  if (clinic.isLoading) return <p className="text-dim">Loading...</p>;
  if (clinic.error) return <p className="text-error">{getApiError(clinic.error)}</p>;
  if (!clinic.data) return null;

  const c = clinic.data;

  return (
    <section className="clinic-detail">
      <Button size="sm" className="clinic-detail-back" onClick={onBack}>&larr; Back to clinics</Button>

      <div className="clinic-detail-header">
        <h2>{c.name}</h2>
        <Chip status={c.isActive ? 'Active' : 'Inactive'} variant={c.isActive ? 'good' : undefined} />
      </div>

      <div className="clinic-detail-meta">
        <div className="clinic-detail-meta-card">
          <dt>Slug</dt>
          <dd><code>{c.slug}</code></dd>
        </div>
        <div className="clinic-detail-meta-card">
          <dt>Users</dt>
          <dd>{c.userCount}</dd>
        </div>
        <div className="clinic-detail-meta-card">
          <dt>Created</dt>
          <dd>{new Date(c.createdAt).toLocaleDateString('pt-PT')}</dd>
        </div>
      </div>

      {!editing ? (
        <div className="clinic-detail-section">
          <h3>Clinic Settings</h3>
          <Button variant="primary" size="sm" onClick={startEditing}>Edit clinic</Button>
        </div>
      ) : (
        <form onSubmit={handleSave} className="edit-form-card">
          <h3>Edit Clinic</h3>
          <Field label="Name">
            <Input value={editName} onChange={(e) => setEditName(e.target.value)} required />
          </Field>
          <Field label="Active" inline>
            <input type="checkbox" checked={editActive} onChange={(e) => setEditActive(e.target.checked)} />
          </Field>
          {error && <p className="dialog-error">{error}</p>}
          <div className="edit-actions">
            <Button variant="ghost" type="button" onClick={() => setEditing(false)}>Cancel</Button>
            <Button variant="primary" type="submit" disabled={update.isPending}>Save</Button>
          </div>
        </form>
      )}

      <div className="clinic-detail-section">
        <h3>Feature Flags</h3>
        {flagError && <p className="dialog-error">{flagError}</p>}
        <div className="feature-flags-grid">
          {c.featureFlags.map((flag) => (
            <label key={flag.featureKey} className="feature-flag-card">
              <input
                type="checkbox"
                checked={flag.isEnabled}
                onChange={() => toggleFlag.mutate({ featureKey: flag.featureKey, isEnabled: !flag.isEnabled })}
                disabled={toggleFlag.isPending}
              />
              <span className="feature-flag-label">{FEATURE_LABELS[flag.featureKey] ?? flag.featureKey}</span>
            </label>
          ))}
        </div>
      </div>
    </section>
  );
}
