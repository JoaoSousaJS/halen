import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getClinic, updateClinic, setFeatureFlag } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';
import { Button, Input, Chip, ToggleSwitch } from '../../shared/components';

interface ClinicDetailPageProps {
  clinicId: string;
  onBack: () => void;
}

const FEATURE_META: Record<string, { label: string; description: string }> = {
  prescriptions:   { label: 'Prescriptions',    description: 'Allow doctors to issue prescriptions' },
  kyc:             { label: 'KYC Verification',  description: 'Require doctor identity verification' },
  video_calls:     { label: 'Video Calls',       description: 'Enable video consultation rooms' },
  doctor_reviews:  { label: 'Doctor Reviews',    description: 'Patient reviews and ratings for doctors' },
  medical_records: { label: 'Medical Records',   description: 'Patient health records and documents' },
  messaging:       { label: 'Messaging',         description: 'In-app messaging between patients and doctors' },
  audit_trail:     { label: 'Audit Trail',       description: 'Track admin actions and system changes' },
};

const FEATURE_ORDER = Object.keys(FEATURE_META);

export default function ClinicDetailPage({ clinicId, onBack }: ClinicDetailPageProps) {
  const queryClient = useQueryClient();

  const clinic = useQuery({
    queryKey: ['clinic', clinicId],
    queryFn: () => getClinic(clinicId),
  });

  const [editingName, setEditingName] = useState(false);
  const [editName, setEditName] = useState('');
  const [settingsError, setSettingsError] = useState('');
  const [flagError, setFlagError] = useState('');
  const [mutatingFlagKey, setMutatingFlagKey] = useState<string | null>(null);

  const saveName = useMutation({
    mutationFn: () =>
      updateClinic(clinicId, { name: editName.trim(), isActive: clinic.data!.isActive }),
    onSuccess: () => {
      setEditingName(false);
      setSettingsError('');
      queryClient.invalidateQueries({ queryKey: ['clinic', clinicId] });
      queryClient.invalidateQueries({ queryKey: ['clinics'] });
    },
    onError: (err) => setSettingsError(getApiError(err)),
  });

  const toggleStatus = useMutation({
    mutationFn: (newActive: boolean) =>
      updateClinic(clinicId, { name: clinic.data!.name, isActive: newActive }),
    onMutate: () => setSettingsError(''),
    onSuccess: () => {
      setSettingsError('');
      queryClient.invalidateQueries({ queryKey: ['clinic', clinicId] });
      queryClient.invalidateQueries({ queryKey: ['clinics'] });
    },
    onError: (err) => setSettingsError(getApiError(err)),
  });

  const toggleFlag = useMutation({
    mutationFn: ({ featureKey, isEnabled }: { featureKey: string; isEnabled: boolean }) =>
      setFeatureFlag(clinicId, featureKey, isEnabled),
    onMutate: ({ featureKey }) => setMutatingFlagKey(featureKey),
    onSettled: () => setMutatingFlagKey(null),
    onSuccess: () => {
      setFlagError('');
      queryClient.invalidateQueries({ queryKey: ['clinic', clinicId] });
    },
    onError: (err) => setFlagError(getApiError(err)),
  });

  function startEditingName() {
    if (!clinic.data) return;
    setEditName(clinic.data.name);
    setEditingName(true);
    setSettingsError('');
  }

  function cancelEdit() {
    setEditingName(false);
    setSettingsError('');
  }

  function handleSaveName() {
    const trimmed = editName.trim();
    if (!trimmed || trimmed.length < 3) {
      setSettingsError('Clinic name must be at least 3 characters.');
      return;
    }
    saveName.mutate();
  }

  function handleNameKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') handleSaveName();
    if (e.key === 'Escape') cancelEdit();
  }

  if (clinic.isLoading) return <p className="text-dim">Loading...</p>;
  if (clinic.error) return <p className="text-error">{getApiError(clinic.error)}</p>;
  if (!clinic.data) return null;

  const c = clinic.data;

  const sortedFlags = [...c.featureFlags].sort(
    (a, b) =>
      (FEATURE_ORDER.indexOf(a.featureKey) === -1 ? 999 : FEATURE_ORDER.indexOf(a.featureKey)) -
      (FEATURE_ORDER.indexOf(b.featureKey) === -1 ? 999 : FEATURE_ORDER.indexOf(b.featureKey)),
  );

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

      <div className="clinic-detail-columns">
        <div className="clinic-detail-section">
          <h3>Clinic Settings</h3>

          <div className="settings-field">
            <span className="settings-field-label">NAME</span>
            {editingName ? (
              <div className="settings-field-editing">
                <Input
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  onKeyDown={handleNameKeyDown}
                  autoFocus
                  required
                  maxLength={200}
                />
                <Button variant="primary" size="sm" onClick={handleSaveName} disabled={saveName.isPending}>
                  Save
                </Button>
                <Button variant="ghost" size="sm" onClick={cancelEdit} ariaLabel="Cancel editing">
                  &#x2715;
                </Button>
              </div>
            ) : (
              <button className="settings-field-value" onClick={startEditingName}>
                {c.name}
                <span className="settings-field-pencil" aria-hidden="true">&#x270E;</span>
              </button>
            )}
            {settingsError && <p className="settings-field-error" role="alert">{settingsError}</p>}
          </div>

          <div className="settings-field">
            <span className="settings-field-label">STATUS</span>
            <ToggleSwitch
              checked={c.isActive}
              onChange={(newVal) => toggleStatus.mutate(newVal)}
              loading={toggleStatus.isPending}
              label={c.isActive ? 'Active' : 'Inactive'}
              ariaLabel="Set clinic active status"
            />
          </div>
        </div>

        <div className="clinic-detail-section">
          <h3>Feature Flags</h3>
          {flagError && <p className="settings-field-error" role="alert">{flagError}</p>}
          {sortedFlags.length === 0 ? (
            <p className="flag-cards-empty">No feature flags configured.</p>
          ) : (
            <div className="flag-cards">
              {sortedFlags.map((flag) => {
                const meta = FEATURE_META[flag.featureKey];
                return (
                  <div
                    key={flag.featureKey}
                    className={['flag-card', flag.isEnabled && 'flag-card--enabled']
                      .filter(Boolean)
                      .join(' ')}
                  >
                    <div className="flag-card-info">
                      <span className="flag-card-label">{meta?.label ?? flag.featureKey}</span>
                      <span className="flag-card-desc">{meta?.description ?? ''}</span>
                    </div>
                    <ToggleSwitch
                      checked={flag.isEnabled}
                      onChange={(v) =>
                        toggleFlag.mutate({ featureKey: flag.featureKey, isEnabled: v })
                      }
                      loading={mutatingFlagKey === flag.featureKey && toggleFlag.isPending}
                      ariaLabel={`${flag.isEnabled ? 'Disable' : 'Enable'} ${meta?.label ?? flag.featureKey}`}
                    />
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
