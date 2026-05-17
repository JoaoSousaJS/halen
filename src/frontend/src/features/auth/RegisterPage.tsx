import { useState } from 'react';
import type { SubmitEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { register } from '../../shared/api/auth';
import { getApiError } from '../../shared/api/errors';
import { useAuth } from '../../shared/components/AuthProvider';
import { Button, Field } from '../../shared/components';

interface FormState {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export default function RegisterPage() {
  const { saveToken } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // rerender-functional-setstate: functional update prevents stale closure bugs
  function setField<K extends keyof FormState>(field: K, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { token } = await register({ ...form, role: 0 });
      saveToken(token);
      navigate('/dashboard');
    } catch (err: unknown) {
      setError(getApiError(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-name">Halen</div>
            <div className="brand-sub">care · on call</div>
          </div>
        </div>

        <h1 className="auth-heading">
          Create your<br /><em>account.</em>
        </h1>

        <form onSubmit={handleSubmit} className="auth-form">
          <Field label="" row>
            <Field label="First name">
              <input
                type="text"
                required
                value={form.firstName}
                onChange={(e) => setField('firstName', e.target.value)}
                placeholder="Maya"
              />
            </Field>
            <Field label="Last name">
              <input
                type="text"
                required
                value={form.lastName}
                onChange={(e) => setField('lastName', e.target.value)}
                placeholder="Chen"
              />
            </Field>
          </Field>

          <Field label="Email">
            <input
              type="email"
              required
              autoComplete="email"
              value={form.email}
              onChange={(e) => setField('email', e.target.value)}
              placeholder="you@example.com"
            />
          </Field>

          <Field label="Password">
            <input
              type="password"
              required
              autoComplete="new-password"
              value={form.password}
              onChange={(e) => setField('password', e.target.value)}
              placeholder="8+ characters, include a digit"
            />
          </Field>

          {error ? <p className="auth-error">{error}</p> : null}

          <Button variant="primary" block type="submit" disabled={loading}>
            {loading ? 'Creating account…' : 'Create account'}
          </Button>
        </form>

        <p className="auth-foot">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </div>
    </div>
  );
}
