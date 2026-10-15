import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { InjectionToken, inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, take, tap } from 'rxjs';
import { isPrivateApiRequest } from '@istpetdev/shared-core';
import { environment } from '../config/environment';

/** Qué hace la app cuando la API privada responde 401: la sesión murió (refresh fallido o cuenta revocada). */
export const SESSION_EXPIRED = new InjectionToken<() => void>('SESSION_EXPIRED', { factory: () => () => undefined });

/** FR-04: Bearer solo hacia la API configurada; la traza pública y terceros no lo reciben. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isPrivateApiRequest(request.url, environment.apiUrl)) return next(request);
  const oidc = inject(OidcSecurityService);
  const sessionExpired = inject(SESSION_EXPIRED);
  return oidc.getAccessToken().pipe(
    take(1),
    switchMap((token) => next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request)),
    tap({ error: (error: unknown) => { if (error instanceof HttpErrorResponse && error.status === 401) sessionExpired(); } }),
  );
};
