import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Observable } from 'rxjs';
import { environment } from '../config/environment';
import { ApiService } from './api.service';

describe('fallos de consulta visibles', () => {
  let api: ApiService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ApiService]
    });
    api = TestBed.inject(ApiService);
    backend = TestBed.inject(HttpTestingController);
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    backend.verify();
    vi.restoreAllMocks();
    TestBed.resetTestingModule();
  });

  const queries: Array<[string, string, (api: ApiService) => Observable<unknown>]> = [
    ['puntos de servicio', '/api/v1/service-points', (a: ApiService) => a.getServicePoints()],
    ['bodegas', '/api/v1/service-points/warehouses', (a: ApiService) => a.getWarehouses()],
    ['rutas', '/api/v1/route-plans', (a: ApiService) => a.getRoutePlans()],
    ['decisiones del agente', '/api/v1/agent-decisions', (a: ApiService) => a.getAgentDecisions()],
    ['datos de products', '/api/v1/imports/data/products', (a: ApiService) => a.getEntityData('products')],
    ['capturas rechazadas', '/api/v1/deliveries/rejected', (a: ApiService) => a.getRejectedCaptures()],
    ['alertas de stock', '/api/v1/risks', (a: ApiService) => a.getRisks()]
  ];

  it.each(queries)('expone el fallo de %s y lo resuelve tras una respuesta válida', (resource, path, query) => {
    query(api).subscribe();
    backend.expectOne(environment.apiUrl + path).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(console.error).toHaveBeenCalled();
    expect(api.loadErrors()[resource]).toContain('No se pudo cargar');
    query(api).subscribe();
    backend.expectOne(environment.apiUrl + path).flush([]);
    expect(api.loadErrors()[resource]).toBeUndefined();
  });

  it('un éxito no borra fallos pendientes de otros recursos', () => {
    api.getServicePoints().subscribe();
    api.getWarehouses().subscribe();
    backend.expectOne(environment.apiUrl + '/api/v1/service-points').flush({}, { status: 500, statusText: 'Error' });
    backend.expectOne(environment.apiUrl + '/api/v1/service-points/warehouses').flush([]);
    expect(api.loadErrors()['puntos de servicio']).toBeDefined();
    expect(api.loadErrors()['bodegas']).toBeUndefined();
  });

  it('una lista realmente vacía no se presenta como un fallo', () => {
    api.getServicePoints().subscribe(result => expect(result).toEqual([]));
    backend.expectOne(environment.apiUrl + '/api/v1/service-points').flush([]);
    expect(api.loadErrors()).toEqual({});
  });
});
