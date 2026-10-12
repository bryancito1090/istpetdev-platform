import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('@pages/landing').then(m => m.LandingPage),
    title: 'IstpetDev — Plataforma Logística y Abastecimiento Hospitalario'
  },
  {
    path: 'login',
    loadComponent: () => import('@pages/login').then(m => m.LoginPage),
    title: 'Iniciar Sesión — IstpetDev Logistics'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('@pages/operations-dashboard').then(m => m.OperationsDashboardPage),
    title: 'Panel Operativo — IstpetDev Logistics'
  },
  {
    path: 'public-trace',
    loadComponent: () => import('@pages/public-trace').then(m => m.PublicTracePage),
    title: 'Trazabilidad Pública — IstpetDev Logistics'
  },
  {
    path: '**',
    redirectTo: ''
  }
];
