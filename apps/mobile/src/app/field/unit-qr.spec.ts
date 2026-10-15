import { describe, expect, it } from 'vitest';
import { parseUnitQr } from './unit-qr';

describe('parseUnitQr', () => {
  it('extrae unitId y token de una URL válida de traza pública', () => {
    const raw = 'https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=abcdef0123456789abcdef0123456789';
    const result = parseUnitQr(raw);
    expect(result).toEqual({
      unitId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      token: 'abcdef0123456789abcdef0123456789',
    });
  });

  it('devuelve null si el código QR es inválido o está vacío', () => {
    expect(parseUnitQr('')).toBeNull();
    expect(parseUnitQr(null)).toBeNull();
    expect(parseUnitQr('https://example.com/other-path')).toBeNull();
  });
});
