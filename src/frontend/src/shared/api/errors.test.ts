import { describe, it, expect } from 'vitest';
import axios from 'axios';
import { getApiError } from './errors';

function makeAxiosError(data: unknown, status: number, message = 'Request failed') {
  const err = new axios.AxiosError(message);
  (err as { response?: unknown }).response = { data, status };
  return err;
}

describe('getApiError', () => {
  it('returns data.error from an axios error response', () => {
    const err = makeAxiosError({ error: 'Invalid credentials' }, 401);
    expect(getApiError(err)).toBe('Invalid credentials');
  });

  it('joins field messages from data.errors array', () => {
    const err = makeAxiosError(
      {
        errors: [
          { field: 'email', message: 'Email is required' },
          { field: 'password', message: 'Password too short' },
        ],
      },
      400,
    );
    expect(getApiError(err)).toBe('Email is required, Password too short');
  });

  it('falls back to err.message when response has no recognisable shape', () => {
    const err = new axios.AxiosError('Network Error');
    expect(getApiError(err)).toBe('Network Error');
  });

  it('returns message from a plain Error', () => {
    expect(getApiError(new Error('Something went wrong'))).toBe('Something went wrong');
  });

  it('returns a fallback string for unknown error types', () => {
    expect(getApiError('oops')).toBe('An unexpected error occurred');
    expect(getApiError(null)).toBe('An unexpected error occurred');
  });
});
