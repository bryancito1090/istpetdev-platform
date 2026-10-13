import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { MobileAuthService } from './auth.service';
import { FieldUserRole } from './auth.models';

/**
 * Guard funcional para restringir acceso a rutas según roles autorizados.
 * Uso en rutas:
 *   canActivate: [roleGuard],
 *   data: { roles: ['CarrierDriver'] }
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(MobileAuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/home']);
    return false;
  }

  const allowedRoles = route.data['roles'] as FieldUserRole[] | undefined;
  if (!allowedRoles || allowedRoles.length === 0) {
    return true;
  }

  const currentRole = authService.currentRole();
  if (currentRole && allowedRoles.includes(currentRole)) {
    return true;
  }

  // Si no cuenta con el rol requerido, redirige al inicio de la app
  router.navigate(['/home']);
  return false;
};
