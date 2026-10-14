import { Routes } from '@angular/router';
import { authGuard, homeRedirectGuard, roleGuard } from '@entities/session';

const WEB_READ_ROLES = ['Gerencia', 'Planificador', 'Auditor'] as const;
const WEB_WRITE_ROLES = ['Gerencia', 'Planificador'] as const;

/**
 * Rutas de la web.
 * - Planificador: Inicio → Datos → Alertas → Rutas, en el orden del proceso.
 * - Gerencia y Auditor leen Inicio, Alertas y Rutas; Auditor no escribe (FR-21).
 */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('@pages/landing').then(m => m.LandingPage),
    title: 'IstpetDev'
  },
  {
    path: 'login',
    loadComponent: () => import('@pages/login').then(m => m.LoginPage),
    title: 'Iniciar sesión — IstpetDev'
  },
  {
    path: 'auth/callback',
    loadComponent: () => import('@pages/auth-callback').then(m => m.AuthCallbackPage),
    title: 'Verificando sesión — IstpetDev'
  },
  {
    path: 'inicio',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_READ_ROLES },
    loadComponent: () => import('@pages/inicio').then(m => m.InicioPage),
    title: 'Inicio — IstpetDev'
  },
  {
    path: 'datos',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_WRITE_ROLES },
    loadComponent: () => import('@pages/datos').then(m => m.DatosPage),
    title: 'Cargar datos — IstpetDev'
  },
  {
    path: 'alertas',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_READ_ROLES },
    loadComponent: () => import('@pages/alertas').then(m => m.AlertasPage),
    title: 'Alertas — IstpetDev'
  },
  {
    path: 'rutas',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_READ_ROLES },
    loadComponent: () => import('@pages/rutas').then(m => m.RutasPage),
    title: 'Rutas — IstpetDev'
  },
  {
    path: 'bodegas',
    canActivate: [authGuard, roleGuard],
    data: { roles: WEB_WRITE_ROLES },
    loadComponent: () => import('@pages/bodegas').then(m => m.BodegasPage),
    title: 'Bodegas & Rebalanceo — IstpetDev'
  },
  {
    // Pública: el QR impreso en el bulto apunta aquí.
    path: 'public-trace',
    loadComponent: () => import('@pages/public-trace').then(m => m.PublicTracePage),
    title: 'Trazabilidad QR — IstpetDev'
  },
  {
    path: 'public-trace/:unitId',
    loadComponent: () => import('@pages/public-trace').then(m => m.PublicTracePage),
    title: 'Trazabilidad QR — IstpetDev'
  },
  {
    // Compatibilidad con enlaces antiguos: una sesión va a Inicio; una visita anónima va al login.
    path: 'dashboard',
    canActivate: [homeRedirectGuard],
    children: []
  },
  {
    path: '**',
    redirectTo: ''
  }
];
