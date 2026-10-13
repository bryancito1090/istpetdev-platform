import { Routes } from '@angular/router';
import { roleGuard, homeRedirectGuard } from './providers/role.guard';

/**
 * Rutas de la web.
 * - Planificador (SUPERVISOR): Inicio → Datos → Alertas → Rutas, en el orden del proceso.
 * - Gerencia (ADMIN): mismas páginas en solo lectura (sin Datos).
 * - Auditor: solo Trazabilidad QR.
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
    path: 'inicio',
    canActivate: [roleGuard(['SUPERVISOR', 'ADMIN'])],
    loadComponent: () => import('@pages/inicio').then(m => m.InicioPage),
    title: 'Inicio — IstpetDev'
  },
  {
    path: 'datos',
    canActivate: [roleGuard(['SUPERVISOR'])],
    loadComponent: () => import('@pages/datos').then(m => m.DatosPage),
    title: 'Cargar datos — IstpetDev'
  },
  {
    path: 'alertas',
    canActivate: [roleGuard(['SUPERVISOR', 'ADMIN'])],
    loadComponent: () => import('@pages/alertas').then(m => m.AlertasPage),
    title: 'Alertas — IstpetDev'
  },
  {
    path: 'rutas',
    canActivate: [roleGuard(['SUPERVISOR', 'ADMIN'])],
    loadComponent: () => import('@pages/rutas').then(m => m.RutasPage),
    title: 'Rutas — IstpetDev'
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
    // Compatibilidad con enlaces antiguos: envía a la página de inicio del rol.
    path: 'dashboard',
    canActivate: [homeRedirectGuard],
    children: []
  },
  {
    path: '**',
    redirectTo: ''
  }
];
