import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { createDoctor } from '../../shared/api/admin';
import type { CreateDoctorPayload } from '../../shared/api/admin';
import { getApiError } from '../../shared/api/errors';
import { Button, Field } from '../../shared/components';

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

export default function CreateDoctorForm() {
  const [form, setForm] = useState<DoctorForm>(empty);
  const [success, setSuccess] = useState('');

  function setField<K extends keyof DoctorForm>(field: K, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  const mutation = useMutation({
    mutationFn: (payload: CreateDoctorPayload) => createDoctor(payload),
    onSuccess: (_data, variables) => {
      setForm(empty);
      setSuccess(`Doctor account created for ${variables.email}. They will need to submit KYC documents before they can practice.`);
      setTimeout(() => setSuccess(''), 4000);
    },
  });

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSuccess('');
    mutation.mutate({
      ...form,
      consultationFee: parseFloat(form.consultationFee) || 0,
      yearsOfExperience: parseInt(form.yearsOfExperience, 10) || 0,
    });
  }

  return (
    <section className="create-doctor-page">
      <h2 className="section-heading">Create Doctor Account</h2>

      <div className="create-doctor-card">
        <form onSubmit={handleSubmit} className="create-doctor-form">
          <Field label="" row>
            <Field label="First name">
              <input
                type="text" required value={form.firstName}
                onChange={(e) => setField('firstName', e.target.value)}
                placeholder="James"
              />
            </Field>
            <Field label="Last name">
              <input
                type="text" required value={form.lastName}
                onChange={(e) => setField('lastName', e.target.value)}
                placeholder="Wilson"
              />
            </Field>
          </Field>

          <Field label="Email">
            <input
              type="email" required value={form.email}
              onChange={(e) => setField('email', e.target.value)}
              placeholder="doctor@halen.dev"
            />
          </Field>

          <Field label="Temporary password">
            <input
              type="password" required value={form.password}
              onChange={(e) => setField('password', e.target.value)}
              placeholder="8+ characters, include a digit"
            />
          </Field>

          <Field label="Specialty">
            <input
              type="text" required value={form.specialty}
              onChange={(e) => setField('specialty', e.target.value)}
              placeholder="Cardiology"
            />
          </Field>

          <Field label="License number">
            <input
              type="text" required value={form.licenseNumber}
              onChange={(e) => setField('licenseNumber', e.target.value)}
              placeholder="MED-12345"
            />
          </Field>

          <Field label="" row>
            <Field label="Consultation fee ($)">
              <input
                type="number" required min="1" step="0.01" value={form.consultationFee}
                onChange={(e) => setField('consultationFee', e.target.value)}
                placeholder="150"
              />
            </Field>
            <Field label="Years of experience">
              <input
                type="number" required min="0" value={form.yearsOfExperience}
                onChange={(e) => setField('yearsOfExperience', e.target.value)}
                placeholder="5"
              />
            </Field>
          </Field>

          {mutation.isError ? (
            <p className="dialog-error">{getApiError(mutation.error)}</p>
          ) : null}
          {success ? (
            <p className="create-doctor-success">{success}</p>
          ) : null}

          <Button
            variant="primary"
            block
            type="submit"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? 'Creating account…' : 'Create doctor account'}
          </Button>
        </form>
      </div>
    </section>
  );
}
