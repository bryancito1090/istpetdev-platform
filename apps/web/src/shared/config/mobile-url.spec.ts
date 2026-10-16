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
});
