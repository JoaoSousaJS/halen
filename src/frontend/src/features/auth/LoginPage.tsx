import { useState } from 'react';
import type { SubmitEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { login } from '../../shared/api/auth';
import { getApiError } from '../../shared/api/errors';
import { useAuth } from '../../shared/components/AuthProvider';
import { Button, Field } from '../../shared/components';

export default function LoginPage() {
  const { saveToken } = useAuth();
  const navigate = useNavigate();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { token } = await login(email, password);
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
          Welcome<br /><em>back.</em>
        </h1>

        <form onSubmit={handleSubmit} className="auth-form">
          <Field label="Email">
            <input
              type="email"
              required
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
            />
          </Field>

          <Field label="Password">
            <input
              type="password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
            />
          </Field>

          <div className="auth-error-slot" aria-live="polite">
            {error ? <p className="auth-error">{error}</p> : null}
          </div>

          <Button variant="primary" block type="submit" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </Button>
        </form>

        <p className="auth-foot">
          No account? <Link to="/register">Create one</Link>
        </p>
      </div>
    </div>
  );
}
