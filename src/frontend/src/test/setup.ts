import '@testing-library/jest-dom/vitest';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

// Testing Library auto-cleanup relies on globalThis.afterEach being injected
// by the test runner. Without vitest globals mode, we wire it up explicitly.
afterEach(cleanup);
