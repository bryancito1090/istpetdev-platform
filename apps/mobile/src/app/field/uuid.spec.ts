import { afterEach, describe, expect, it, vi } from 'vitest';
import { generateUuidV7 } from './uuid';

describe('generateUuidV7', () => {
  afterEach(() => vi.restoreAllMocks());

  it('es monótono dentro del mismo milisegundo (la cola ordena por operationId al empatar)', () => {
    vi.spyOn(Date, 'now').mockReturnValue(1_792_000_000_000);
    const ids = Array.from({ length: 500 }, () => generateUuidV7());
    expect([...ids].sort()).toEqual(ids);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('no retrocede si el reloj del dispositivo vuelve atrás', () => {
    const now = vi.spyOn(Date, 'now');
    now.mockReturnValue(1_792_000_100_000);
    const first = generateUuidV7();
    now.mockReturnValue(1_792_000_099_000);
    expect(generateUuidV7() > first).toBe(true);
  });

  it('respeta versión 7 y variante RFC 9562', () => {
    expect(generateUuidV7()).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
  });
});
