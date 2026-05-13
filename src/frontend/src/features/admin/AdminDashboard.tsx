import { useState } from 'react';
import type { SubmitEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { createDoctor } from '../../shared/api/admin';
import { getApiError } from '../../shared/api/errors';
import { useAuth } from '../../shared/components/AuthProvider';

interface DoctorForm {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  specialty: string;
  licenseNumber: string;
  consultationFee: string;
  yearsOfExperience: string;
}

const empty: DoctorForm = {
  firstName: '', lastName: '', email: '', password: '',
  specialty: '', licenseNumber: '', consultationFee: '', yearsOfExperience: '0',
};

export default function AdminDashboard() {
  const { user, logout } = useAuth();
  const [form, setForm] = useState<DoctorForm>(empty);
  const [success, setSuccess] = useState('');

  // rerender-functional-setstate: functional update prevents stale closures
  function setField<K extends keyof DoctorForm>(field: K, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  const mutation = useMutation({
    mutationFn: () => createDoctor({
      ...form,
      consultationFee: parseFloat(form.consultationFee) || 0,
      yearsOfExperience: parseInt(form.yearsOfExperience, 10) || 0,
    }),
    onSuccess: () => {
      setForm(empty);
      setSuccess(`Doctor account created for ${form.email}`);
      setTimeout(() => setSuccess(''), 4000);
    },
  });

  function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault();
    setSuccess('');
    mutation.mutate();
  }

  return (
    <div style={{ padding: 32, maxWidth: 600, margin: '0 auto' }}>
      <div className="brand" style={{ marginBottom: 24 }}>
        <div className="brand-mark" />
        <div>
          <div className="brand-name">Halen</div>
          <div className="brand-sub">Admin · {user?.given_name}</div>
        </div>
        <button
          className="btn btn-primary"
          onClick={logout}
          style={{ marginLeft: 'auto', padding: '6px 14px', fontSize: 13 }}
        >
          Sign out
        </button>
      </div>

      <h1 className="auth-heading" style={{ marginBottom: 24 }}>
        Create a<br /><em>doctor account.</em>
      </h1>

      <div className="auth-card">
        <form onSubmit={handleSubmit} className="auth-form">
          <div className="field-row">
            <label className="field">
              <span>First name</span>
              <input
                type="text" required value={form.firstName}
                onChange={(e) => setField('firstName', e.target.value)}
                placeholder="James"
              />
            </label>
            <label className="field">
              <span>Last name</span>
              <input
                type="text" required value={form.lastName}
                onChange={(e) => setField('lastName', e.target.value)}
                placeholder="Wilson"
              />
            </label>
          </div>

          <label className="field">
            <span>Email</span>
            <input
              type="email" required value={form.email}
              onChange={(e) => setField('email', e.target.value)}
              placeholder="doctor@halen.dev"
            />
          </label>

          <label className="field">
            <span>Temporary password</span>
            <input
              type="password" required value={form.password}
              onChange={(e) => setField('password', e.target.value)}
              placeholder="8+ characters, include a digit"
            />
          </label>

          <label className="field">
            <span>Specialty</span>
            <input
              type="text" required value={form.specialty}
              onChange={(e) => setField('specialty', e.target.value)}
              placeholder="Cardiology"
            />
          </label>

          <label className="field">
            <span>License number</span>
            <input
              type="text" required value={form.licenseNumber}
              onChange={(e) => setField('licenseNumber', e.target.value)}
              placeholder="MED-12345"
            />
          </label>

          <div className="field-row">
            <label className="field">
              <span>Consultation fee ($)</span>
              <input
                type="number" required min="1" step="0.01" value={form.consultationFee}
                onChange={(e) => setField('consultationFee', e.target.value)}
                placeholder="150"
              />
            </label>
            <label className="field">
              <span>Years of experience</span>
              <input
                type="number" required min="0" value={form.yearsOfExperience}
                onChange={(e) => setField('yearsOfExperience', e.target.value)}
                placeholder="5"
              />
            </label>
          </div>

          {mutation.isError ? (
            <p className="auth-error">{getApiError(mutation.error)}</p>
          ) : null}
          {success ? (
            <p style={{ color: 'var(--accent)', fontSize: 13 }}>{success}</p>
          ) : null}

          <button
            type="submit"
            className="btn btn-primary btn-block"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? 'Creating account…' : 'Create doctor account'}
          </button>
        </form>
      </div>
    </div>
  );
}
