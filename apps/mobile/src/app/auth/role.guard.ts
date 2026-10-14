import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { MobileAuthService } from './auth.service';
import { FieldRole } from './auth.models';

/** FR-08: rutas privadas de la PWA. `data: { roles: [...] }` restringe además por rol de campo. */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(MobileAuthService);
  const router = inject(Router);
  if (auth.status() !== 'authenticated') {
    return auth.status() === 'anonymous'
      ? router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })
      : router.createUrlTree(['/login']);
  }
  const allowed = route.data['roles'] as FieldRole[] | undefined;
  const role = auth.currentRole();
  return !allowed?.length || (role !== null && allowed.includes(role)) ? true : router.createUrlTree(['/home']);
};
