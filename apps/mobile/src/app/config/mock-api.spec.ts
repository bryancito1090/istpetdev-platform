import { describe, expect, it } from 'vitest';
import { shouldMockFieldApi } from './mock-api';

describe('activación explícita del mock de campo', () => {
  it.each(['', '?mock=true', '?mock=false', '?x=1&mock=true', '?mock=true&x=1'])(
    'producción nunca activa el mock: %s', search => {
      expect(shouldMockFieldApi(false, search)).toBe(false);
    }
  );

  it.each(['?mock=true', '?x=1&mock=true', '?mock=true&x=1'])(
    'desarrollo acepta el parámetro exacto: %s', search => {
      expect(shouldMockFieldApi(true, search)).toBe(true);
    }
  );

  it.each(['', '?mock=false', '?mock=trueish', '?notmock=true', '?next=mock=true', '?mock=TRUE'])(
    'rechaza coincidencias parciales: %s', search => {
      expect(shouldMockFieldApi(true, search)).toBe(false);
    }
  );
});
