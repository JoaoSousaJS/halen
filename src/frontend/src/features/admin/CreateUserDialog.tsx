import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { createUserInClinic } from '../../shared/api/clinics';
import { getApiError } from '../../shared/api/errors';

interface CreateUserDialogProps {
  onClose: () => void;
  onCreated: () => void;
}

export default function CreateUserDialog({ onClose, onCreated }: CreateUserDialogProps) {
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<'Patient' | 'Doctor'>('Patient');
  const [error, setError] = useState('');

  const create = useMutation({
    mutationFn: () => createUserInClinic({
      email,
      firstName,
      lastName,
      temporaryPassword: password,
      role: role === 'Patient' ? 0 : 1,
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
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h3>Create User</h3>
        <form onSubmit={handleSubmit}>
          <label className="field">
            <span>Email</span>
            <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </label>
          <label className="field">
            <span>First name</span>
            <input className="input" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
          </label>
          <label className="field">
            <span>Last name</span>
            <input className="input" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
          </label>
          <label className="field">
            <span>Temporary password</span>
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} />
          </label>
          <label className="field">
            <span>Role</span>
            <select className="input" value={role} onChange={(e) => setRole(e.target.value as 'Patient' | 'Doctor')}>
              <option value="Patient">Patient</option>
              <option value="Doctor">Doctor</option>
            </select>
          </label>
          {error && <p className="text-error">{error}</p>}
          <div className="modal-actions">
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={create.isPending}>
              {create.isPending ? 'Creating...' : 'Create user'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
