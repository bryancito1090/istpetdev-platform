import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSession } from '../model/auth-session';

/** FR-08: rutas privadas exigen una cuenta web activa; el destino original se valida al volver. */
export const authGuard: CanActivateFn = (_route, state) => {
  const session = inject(AuthSession);
  const router = inject(Router);
  if (session.status() === 'authenticated') return true;
  return session.status() === 'anonymous'
    ? router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })
    : router.createUrlTree(['/login']);
};
