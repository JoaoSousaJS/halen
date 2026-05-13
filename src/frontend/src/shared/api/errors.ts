import axios from 'axios';

export function getApiError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data;
    if (data?.error) return data.error;
    if (Array.isArray(data?.errors) && data.errors.length > 0) {
      return data.errors
        .map((e: unknown) => (e && typeof e === 'object' && 'message' in e ? String((e as { message: unknown }).message) : null))
        .filter(Boolean)
        .join(', ') || err.message;
    }
    return err.message;
  }
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred';
}
