import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { createUserInClinic } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';
import { Button, Field, Input, Dialog, DialogActions } from '../../shared/components';

interface CreateUserDialogProps {
  onClose: () => void;
  onCreated: () => void;
}

export default function CreateUserDialog({ onClose, onCreated }: CreateUserDialogProps) {
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const create = useMutation({
    mutationFn: () => createUserInClinic({
      email,
      firstName,
      lastName,
      temporaryPassword: password,
      role: 0,
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
    <Dialog title="Create Patient" subtitle="Add a new patient to this clinic" onClose={onClose} wide>
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
          <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required placeholder="patient@clinic.com" />
        </Field>
        <Field label="Temporary password" hint="The user will be asked to change this on first login.">
          <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} placeholder="Min. 8 characters" />
        </Field>

        {error && <p className="dialog-error" role="alert">{error}</p>}

        <DialogActions>
          <Button variant="ghost" type="button" onClick={onClose}>Cancel</Button>
          <Button variant="primary" type="submit" disabled={create.isPending}>
            {create.isPending ? 'Creating...' : 'Create patient'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
