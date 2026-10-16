import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getMobileAppUrl } from './mobile-url';
import { environment } from './environment';

describe('getMobileAppUrl', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('devuelve http://localhost:8100 en desarrollo cuando el host es localhost', () => {
    environment.production = false;
    environment.mobileAppUrl = 'http://localhost:8100';
    expect(getMobileAppUrl()).toBe('http://localhost:8100');
  });

  it('devuelve /campo/ en producción de forma determinista', () => {
    const originalProd = environment.production;
    const originalUrl = environment.mobileAppUrl;
    try {
      environment.production = true;
      environment.mobileAppUrl = '/campo/';
      expect(getMobileAppUrl()).toBe('/campo/');
    } finally {
      environment.production = originalProd;
      environment.mobileAppUrl = originalUrl;
    }
  });

  it('devuelve /campo/ si el dominio es istpetdev.doicela.dev independientemente de la variable', () => {
    environment.production = false;
    vi.stubGlobal('location', {
      origin: 'https://istpetdev.doicela.dev',
      hostname: 'istpetdev.doicela.dev',
      protocol: 'https:',
    });
    expect(getMobileAppUrl()).toBe('/campo/');
  });

  it('adapta el host a la IP LAN en desarrollo si se accede desde un dispositivo físico', () => {
    environment.production = false;
    environment.mobileAppUrl = 'http://localhost:8100';
    vi.stubGlobal('location', {
      origin: 'http://192.168.1.50:4200',
      hostname: '192.168.1.50',
      protocol: 'http:',
    });
    expect(getMobileAppUrl()).toBe('http://192.168.1.50:8100');
  });
});
