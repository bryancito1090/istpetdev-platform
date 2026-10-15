import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { environment } from '../config/environment';
import { HttpWebFieldApiService } from './http-web-field-api.service';

describe('HttpWebFieldApiService', () => {
  const configuredApiUrl = environment.apiUrl;

  afterEach(() => {
    environment.apiUrl = configuredApiUrl;
    TestBed.resetTestingModule();
  });

  it('usa la API del mismo origen cuando apiUrl está vacío', () => {
    environment.apiUrl = '';
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), HttpWebFieldApiService],
    });

    const api = TestBed.inject(HttpWebFieldApiService);
    const backend = TestBed.inject(HttpTestingController);
    api.getDeliveries().subscribe();

    const request = backend.expectOne('/api/v1/deliveries');
    request.flush({ items: [] });
    backend.verify();
  });

  it('consulta capturas rechazadas con o sin filtro de servicePointId', () => {
    environment.apiUrl = '';
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), HttpWebFieldApiService],
    });

    const api = TestBed.inject(HttpWebFieldApiService);
    const backend = TestBed.inject(HttpTestingController);

    api.getRejectedCaptures('sp-123').subscribe(res => {
      expect(res.count).toBe(1);
    });

    const req = backend.expectOne('/api/v1/deliveries/rejected?servicePointId=sp-123');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [{ receiptId: 'rcpt-1' }], count: 1 });
    backend.verify();
  });
});
