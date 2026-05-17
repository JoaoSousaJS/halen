import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getMyProfile, updateMyProfile, changePassword } from '../../shared/api/profile';
import type { ProfileDto } from '../../shared/api/profile';
import { getApiError } from '../../shared/api/errors';
import { DashboardShell } from '../../shared/components/DashboardShell';
import { Button, Field, Input, Chip } from '../../shared/components';

// ── Helpers ─────────────────────────────────────

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function formatCurrency(amount: number): string {
  return `$${amount.toFixed(2)}`;
}

function subtitleForRole(role: string): string {
  switch (role) {
    case 'Doctor':
      return 'Doctor Portal';
    case 'Patient':
      return 'Patient Portal';
    case 'ClinicAdmin':
      return 'Clinic Admin';
    case 'PlatformAdmin':
      return 'Platform Admin';
    default:
      return 'Portal';
  }
}

// ── ProfileForm ─────────────────────────────────

interface ProfileFormProps {
  initialData: ProfileDto;
}

function ProfileForm({ initialData }: ProfileFormProps) {
  const queryClient = useQueryClient();

  // Profile fields
  const [firstName, setFirstName] = useState(initialData.firstName);
  const [lastName, setLastName] = useState(initialData.lastName);
  const [dateOfBirth, setDateOfBirth] = useState(initialData.dateOfBirth ?? '');
  const [city, setCity] = useState(initialData.city ?? '');
  const [profileSuccess, setProfileSuccess] = useState('');
  const [profileError, setProfileError] = useState('');

  // Password fields
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordSuccess, setPasswordSuccess] = useState('');
  const [passwordError, setPasswordError] = useState('');

  const isDoctor = initialData.role === 'Doctor';
  const isPatient = initialData.role === 'Patient';

  const profileMutation = useMutation({
    mutationFn: updateMyProfile,
    onSuccess: () => {
      setProfileError('');
      setProfileSuccess('Profile updated successfully.');
      setTimeout(() => setProfileSuccess(''), 4000);
      queryClient.invalidateQueries({ queryKey: ['my-profile'] });
    },
    onError: (err: unknown) => {
      setProfileSuccess('');
      setProfileError(getApiError(err));
    },
  });

  const passwordMutation = useMutation({
    mutationFn: changePassword,
    onSuccess: () => {
      setPasswordError('');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setPasswordSuccess('Password changed successfully.');
      setTimeout(() => setPasswordSuccess(''), 4000);
    },
    onError: (err: unknown) => {
      setPasswordSuccess('');
      setPasswordError(getApiError(err));
    },
  });

  function handleProfileSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setProfileSuccess('');
    setProfileError('');

    profileMutation.mutate({
      firstName,
      lastName,
      dateOfBirth: isPatient && dateOfBirth ? dateOfBirth : null,
      city: isPatient && city ? city : null,
    });
  }

  function handlePasswordSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setPasswordSuccess('');
    setPasswordError('');

    if (newPassword !== confirmPassword) {
      setPasswordError('New password and confirmation do not match.');
      return;
    }

    passwordMutation.mutate({ currentPassword, newPassword });
  }

  return (
    <DashboardShell
      subtitle={subtitleForRole(initialData.role)}
      userName={`${initialData.firstName} ${initialData.lastName}`}
    >
      {/* ── Profile info card ────────────────────── */}
      <section>
        <h2 className="section-heading">Profile</h2>

        <div className="auth-card">
          <form onSubmit={handleProfileSubmit} className="auth-form">
            <Field label="First name">
              <Input
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                required
              />
            </Field>

            <Field label="Last name">
              <Input
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                required
              />
            </Field>

            <Field label="Email">
              <Input value={initialData.email} disabled />
            </Field>

            <Field label="Role">
              <Chip status={initialData.role} />
            </Field>

            <Field label="Member since">
              <span className="text-dim">{formatDate(initialData.createdAt)}</span>
            </Field>

            {isPatient && (
              <>
                <Field label="Date of birth">
                  <Input
                    type="date"
                    value={dateOfBirth}
                    onChange={(e) => setDateOfBirth(e.target.value)}
                  />
                </Field>

                <Field label="City">
                  <Input
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                    placeholder="Your city"
                  />
                </Field>
              </>
            )}

            {isDoctor && (
              <>
                <Field label="Specialty">
                  <Input value={initialData.specialty ?? ''} disabled />
                </Field>

                <Field label="Consultation fee">
                  <Input
                    value={
                      initialData.consultationFee != null
                        ? formatCurrency(initialData.consultationFee)
                        : ''
                    }
                    disabled
                  />
                </Field>

                <Field label="Years of experience">
                  <Input
                    value={
                      initialData.yearsOfExperience != null
                        ? String(initialData.yearsOfExperience)
                        : ''
                    }
                    disabled
                  />
                </Field>

                {initialData.languages && initialData.languages.length > 0 && (
                  <Field label="Languages">
                    <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                      {initialData.languages.map((lang) => (
                        <Chip key={lang} status={lang} />
                      ))}
                    </div>
                  </Field>
                )}
              </>
            )}

            {profileSuccess && (
              <p className="text-dim" style={{ color: 'var(--accent)' }}>
                {profileSuccess}
              </p>
            )}
            {profileError && <p className="auth-error">{profileError}</p>}

            <Button
              variant="primary"
              block
              type="submit"
              disabled={profileMutation.isPending}
            >
              {profileMutation.isPending ? 'Saving...' : 'Save changes'}
            </Button>
          </form>
        </div>
      </section>

      {/* ── Change password card ─────────────────── */}
      <section>
        <h2 className="section-heading">Change password</h2>

        <div className="auth-card">
          <form onSubmit={handlePasswordSubmit} className="auth-form">
            <Field label="Current password">
              <Input
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                autoComplete="current-password"
                required
              />
            </Field>

            <Field label="New password">
              <Input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                autoComplete="new-password"
                required
              />
            </Field>

            <Field label="Confirm new password">
              <Input
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
                required
              />
            </Field>

            {passwordSuccess && (
              <p className="text-dim" style={{ color: 'var(--accent)' }}>
                {passwordSuccess}
              </p>
            )}
            {passwordError && <p className="auth-error">{passwordError}</p>}

            <Button
              variant="primary"
              block
              type="submit"
              disabled={passwordMutation.isPending}
            >
              {passwordMutation.isPending ? 'Changing...' : 'Change password'}
            </Button>
          </form>
        </div>
      </section>
    </DashboardShell>
  );
}

// ── ProfilePage (top-level) ─────────────────────

export default function ProfilePage() {
  const { data: profile, isLoading, isError, isSuccess } = useQuery({
    queryKey: ['my-profile'],
    queryFn: getMyProfile,
  });

  if (isLoading) return <p className="text-dim">Loading...</p>;
  if (isError) return <p className="auth-error">Failed to load profile.</p>;
  if (!isSuccess || !profile) return null;

  return <ProfileForm initialData={profile} />;
}
