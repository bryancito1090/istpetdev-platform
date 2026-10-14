import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { HumanRole } from '@istpetdev/shared-core';
import { AuthSession } from '../model/auth-session';

/** Usar después de authGuard: los roles permitidos se declaran en `route.data['roles']`. */
export const roleGuard: CanActivateFn = route => {
  const allowed = (route.data['roles'] as HumanRole[] | undefined) ?? [];
  const role = inject(AuthSession).role();
  return role !== null && allowed.includes(role) ? true : inject(Router).createUrlTree(['/dashboard']);
};

/** El acceso legado a /dashboard lleva a inicio si hay sesión y a login si no. */
export const homeRedirectGuard: CanActivateFn = () => {
  const session = inject(AuthSession);
  return inject(Router).parseUrl(session.status() === 'authenticated' ? '/inicio' : '/login');
};

/** Si ya hay sesión activa, la raíz (/) redirige a /inicio; si no, permite ver la landing. */
export const landingRedirectGuard: CanActivateFn = () => {
  const session = inject(AuthSession);
  return session.status() === 'authenticated' ? inject(Router).parseUrl('/inicio') : true;
};
