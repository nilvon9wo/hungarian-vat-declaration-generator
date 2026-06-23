import { afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';

// Mock triggerBrowserDownload to suppress JSDOM navigation warnings
vi.mock('../utils/download', () => ({
  triggerBrowserDownload: vi.fn()
}));

afterEach(() => {
  cleanup();
});
