import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { SessionStore, UserRole } from '@entities/session';

/** Página donde empieza cada rol al entrar al sistema. */
export function homeFor(role: UserRole | null): string {
  return role === 'SUPERVISOR' || role === 'ADMIN' ? '/inicio' : '/public-trace';
}

/** Deja pasar solo a los roles indicados; si no, envía al login o a la página de inicio de su rol. */
export function roleGuard(allowed: UserRole[]): CanActivateFn {
  return (): boolean | UrlTree => {
    const session = inject(SessionStore);
    const router = inject(Router);
    if (!session.isAuthenticated()) return router.parseUrl('/login');
    const role = session.userRole();
    return role && allowed.includes(role) ? true : router.parseUrl(homeFor(role));
  };
}

/** Redirige a la página de inicio del rol autenticado. */
export const homeRedirectGuard: CanActivateFn = (): UrlTree => {
  const session = inject(SessionStore);
  const router = inject(Router);
  return router.parseUrl(session.isAuthenticated() ? homeFor(session.userRole()) : '/login');
};
