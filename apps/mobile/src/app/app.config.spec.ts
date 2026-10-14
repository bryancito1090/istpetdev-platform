import { describe, expect, it } from 'vitest';
import { shouldMockFieldApi } from './config/mock-api';

describe('shouldMockFieldApi', () => {
  it('no activa el mock en producción incluso con ?mock=true', () => {
    expect(shouldMockFieldApi(false, '?mock=true')).toBe(false);
    expect(shouldMockFieldApi(false, '?foo=1&mock=true')).toBe(false);
  });

  it('no activa el mock en desarrollo si falta mock=true', () => {
    expect(shouldMockFieldApi(true, '')).toBe(false);
    expect(shouldMockFieldApi(true, '?mock=false')).toBe(false);
    expect(shouldMockFieldApi(true, '?other=true')).toBe(false);
  });

  it('activa el mock solo en desarrollo cuando la query string incluye mock=true', () => {
    expect(shouldMockFieldApi(true, '?mock=true')).toBe(true);
    expect(shouldMockFieldApi(true, '?debug=1&mock=true&lang=es')).toBe(true);
  });
});
