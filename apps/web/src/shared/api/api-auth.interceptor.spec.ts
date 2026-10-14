import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../config/environment';
import { apiAuthInterceptor } from './api-auth.interceptor';

describe('apiAuthInterceptor (FR-04)', () => {
  beforeEach(() => TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([apiAuthInterceptor])),
      provideHttpClientTesting(),
      { provide: OidcSecurityService, useValue: { getAccessToken: () => of('token-de-prueba') } },
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
});
