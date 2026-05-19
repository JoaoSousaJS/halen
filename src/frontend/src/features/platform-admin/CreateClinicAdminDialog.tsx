import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { createClinicAdmin } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';
import { Button, Field, Input, Dialog, DialogActions } from '../../shared/components';

interface CreateClinicAdminDialogProps {
  clinicId: string;
  onClose: () => void;
  onCreated: () => void;
}

export default function CreateClinicAdminDialog({ clinicId, onClose, onCreated }: CreateClinicAdminDialogProps) {
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const create = useMutation({
    mutationFn: () => createClinicAdmin(clinicId, {
      email,
      firstName,
      lastName,
      temporaryPassword: password,
    }),
    onSuccess: onCreated,
    onError: (err) => setError(getApiError(err)),
  });

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    create.mutate();
  }

  return (
    <Dialog title="Create Clinic Admin" subtitle="Add an administrator to this clinic" onClose={onClose} wide>
      <form onSubmit={handleSubmit} className="dialog-body">
        <Field label="" row>
          <Field label="First name">
            <Input value={firstName} onChange={(e) => setFirstName(e.target.value)} required placeholder="Jane" />
          </Field>
          <Field label="Last name">
            <Input value={lastName} onChange={(e) => setLastName(e.target.value)} required placeholder="Doe" />
          </Field>
        </Field>
        <Field label="Email">
          <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required placeholder="admin@clinic.com" />
        </Field>
        <Field label="Temporary password" hint="Min 8 characters, one uppercase letter, one digit">
          <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} placeholder="Min. 8 characters" />
        </Field>

        {error && <p className="dialog-error" role="alert">{error}</p>}

        <DialogActions>
          <Button variant="ghost" type="button" onClick={onClose}>Cancel</Button>
          <Button variant="primary" type="submit" disabled={create.isPending}>
            {create.isPending ? 'Creating...' : 'Create admin'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
