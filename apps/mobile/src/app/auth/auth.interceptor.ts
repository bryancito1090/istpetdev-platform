import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MobileAuthService } from './auth.service';

/**
 * Interceptor HTTP funcional para inyectar token Bearer en peticiones al API.
 * En modo simulado, adjunta el token JWT sintético correspondiente al perfil activo.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(MobileAuthService);
  const token = authService.accessToken();

  // No agregar token en peticiones de assets o traza pública sin autenticación
  const isPublicOrAsset = req.url.includes('/assets/') || req.url.includes('/public-trace');

  if (token && !isPublicOrAsset && !req.headers.has('Authorization')) {
    const authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(authReq);
  }

  return next(req);
};
