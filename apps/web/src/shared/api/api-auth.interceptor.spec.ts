import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '../config/environment';
import { SESSION_EXPIRED, apiAuthInterceptor } from './api-auth.interceptor';

describe('apiAuthInterceptor (FR-04)', () => {
  const sessionExpired = vi.fn();
  beforeEach(() => TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([apiAuthInterceptor])),
      provideHttpClientTesting(),
      { provide: OidcSecurityService, useValue: { getAccessToken: () => of('token-de-prueba') } },
      { provide: SESSION_EXPIRED, useValue: sessionExpired },
    ],
  }));

  it('añade Bearer solo a la API privada', () => {
    const http = TestBed.inject(HttpClient);
    const backend = TestBed.inject(HttpTestingController);
    const privateUrl = `${environment.apiUrl}/api/v1/me`;
    const publicUrl = `${environment.apiUrl}/api/v1/public/trace/abc`;
    const tileUrl = 'https://tiles.example.org/1/2/3.png';

    http.get(privateUrl).subscribe();
    http.get(publicUrl).subscribe();
    http.get(tileUrl).subscribe();

    expect(backend.expectOne(privateUrl).request.headers.get('Authorization')).toBe('Bearer token-de-prueba');
    expect(backend.expectOne(publicUrl).request.headers.has('Authorization')).toBe(false);
    expect(backend.expectOne(tileUrl).request.headers.has('Authorization')).toBe(false);
  });

  it('un 401 de la API privada avisa que la sesión murió; un 403 no', () => {
    sessionExpired.mockClear();
    const http = TestBed.inject(HttpClient);
    const backend = TestBed.inject(HttpTestingController);
    const url = `${environment.apiUrl}/api/v1/routes`;

    http.get(url).subscribe({ error: () => undefined });
    backend.expectOne(url).flush(null, { status: 403, statusText: 'Forbidden' });
    expect(sessionExpired).not.toHaveBeenCalled();

    http.get(url).subscribe({ error: () => undefined });
    backend.expectOne(url).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(sessionExpired).toHaveBeenCalledTimes(1);
  });
});
