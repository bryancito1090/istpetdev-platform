import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, take } from 'rxjs';
import { isPrivateApiRequest } from '@istpetdev/shared-core';
import { environment } from '../config/environment';

/** FR-04: Bearer solo hacia la API configurada; la traza pública y terceros no lo reciben. */
export const apiAuthInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isPrivateApiRequest(request.url, environment.apiUrl)) return next(request);
  const oidc = inject(OidcSecurityService);
  return oidc.getAccessToken().pipe(
    take(1),
    switchMap(token => next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request)),
  );
};
