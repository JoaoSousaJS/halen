import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getClinic, updateClinic, setFeatureFlag } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';

interface ClinicDetailPageProps {
  clinicId: string;
  onBack: () => void;
}

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

  const toggleFlag = useMutation({
    mutationFn: ({ featureKey, isEnabled }: { featureKey: string; isEnabled: boolean }) =>
      setFeatureFlag(clinicId, featureKey, isEnabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['clinic', clinicId] }),
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
      <button className="btn btn-sm clinic-detail-back" onClick={onBack}>&larr; Back to clinics</button>

      <div className="clinic-detail-header">
        <h2>{c.name}</h2>
        <span className={`chip ${c.isActive ? 'chip-good' : ''}`}>
          {c.isActive ? 'Active' : 'Inactive'}
        </span>
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
          <dd>{new Date(c.createdAt).toLocaleDateString()}</dd>
        </div>
      </div>

      {!editing ? (
        <div className="clinic-detail-section">
          <h3>Clinic Settings</h3>
          <button className="btn btn-primary btn-sm" onClick={startEditing}>Edit clinic</button>
        </div>
      ) : (
        <form onSubmit={handleSave} className="edit-form-card">
          <h3>Edit Clinic</h3>
          <label className="field">
            <span>Name</span>
            <input className="input" value={editName} onChange={(e) => setEditName(e.target.value)} required />
          </label>
          <label className="field field-inline">
            <input type="checkbox" checked={editActive} onChange={(e) => setEditActive(e.target.checked)} />
            <span>Active</span>
          </label>
          {error && <p className="dialog-error">{error}</p>}
          <div className="edit-actions">
            <button type="button" className="btn btn-ghost" onClick={() => setEditing(false)}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={update.isPending}>Save</button>
          </div>
        </form>
      )}

      <div className="clinic-detail-section">
        <h3>Feature Flags</h3>
        <div className="feature-flags-grid">
          {c.featureFlags.map((flag) => (
            <label key={flag.featureKey} className="feature-flag-card">
              <input
                type="checkbox"
                checked={flag.isEnabled}
                onChange={() => toggleFlag.mutate({ featureKey: flag.featureKey, isEnabled: !flag.isEnabled })}
                disabled={toggleFlag.isPending}
              />
              <span>{flag.featureKey}</span>
            </label>
          ))}
        </div>
      </div>
    </section>
  );
}
