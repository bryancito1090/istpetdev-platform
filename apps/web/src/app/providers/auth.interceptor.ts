import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SessionStore } from '@entities/session';

/**
 * Interceptor HTTP funcional para inyectar token Bearer en peticiones al API backend de IstpetDev.
 * Ubicado en la capa 'app/providers' conforme a la jerarquía Feature-Sliced Design (FSD).
 * Cumple con ADR-10, ADR-14 y Spec 004/005 de seguridad RBAC.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const sessionStore = inject(SessionStore);
  const user = sessionStore.currentUser();
  const token = user?.token;

  // No inyectar Authorization en rutas anónimas explícitas
  const isAnonymous = 
    req.url.includes('/api/v1/ping') || 
    req.url.includes('/api/v1/auth/login') ||
    req.url.includes('/health/') || 
    req.url.includes('/api/v1/public/trace/');

  if (token && !isAnonymous && !req.headers.has('Authorization')) {
    const authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(authReq);
  }

  return next(req);
};
