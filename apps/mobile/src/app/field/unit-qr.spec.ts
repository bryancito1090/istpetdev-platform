import { describe, expect, it } from 'vitest';
import { extractUnitId, parseUnitQr } from './unit-qr';

describe('parseUnitQr', () => {
  it('parsea una URL válida con unitId y token', () => {
    const raw = 'https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=a1b2c3d4e5';
    const result = parseUnitQr(raw);

    expect(result).toEqual({
      unitId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      token: 'a1b2c3d4e5'
    });
  });

  it('soporta identificadores en mayúsculas y normaliza unitId a minúsculas', () => {
    const raw = 'HTTPS://ISTPETDEV.DOICELA.DEV/PUBLIC-TRACE/3FA85F64-5717-4562-B3FC-2C963F66AFA6#T=A1B2C3D4E5';
    const result = parseUnitQr(raw);

    expect(result).toEqual({
      unitId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      token: 'A1B2C3D4E5'
    });
  });

  it('retorna null si la URL no incluye token efímero (#t=)', () => {
    const raw = 'https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6';
    expect(parseUnitQr(raw)).toBeNull();
  });

  it('retorna null ante texto cualquiera que no sea URL de rastreo', () => {
    expect(parseUnitQr('hu-001')).toBeNull();
    expect(parseUnitQr('https://ejemplo.com/otro-recurso')).toBeNull();
    expect(parseUnitQr('')).toBeNull();
    expect(parseUnitQr('    ')).toBeNull();
  });
});

describe('extractUnitId', () => {
  it('extrae el unitId desde una URL válida de QR', () => {
    const raw = 'https://istpetdev.doicela.dev/public-trace/3fa85f64-5717-4562-b3fc-2c963f66afa6#t=abc';
    expect(extractUnitId(raw)).toBe('3fa85f64-5717-4562-b3fc-2c963f66afa6');
  });

  it('acepta y normaliza un UUID plano ingresado manualmente', () => {
    const raw = '3FA85F64-5717-4562-B3FC-2C963F66AFA6';
    expect(extractUnitId(raw)).toBe('3fa85f64-5717-4562-b3fc-2c963f66afa6');
  });

  it('retorna null para códigos que no corresponden a un UUID o URL válida', () => {
    expect(extractUnitId('hu-001')).toBeNull();
    expect(extractUnitId('')).toBeNull();
  });
});
