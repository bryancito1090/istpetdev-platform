import { Routes } from '@angular/router';
import { FieldRole, roleGuard } from './auth';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./auth/pages/login/login.page').then((m) => m.LoginPage),
    title: 'Iniciar Sesión — IstpetDev Móvil'
  },
  {
    path: 'auth/callback',
    loadComponent: () => import('./auth/pages/callback/callback.page').then((m) => m.CallbackPage),
    title: 'Verificando sesión — IstpetDev Móvil'
  },
  {
    path: 'home',
    loadComponent: () => import('./home/home.page').then((m) => m.HomePage),
    canActivate: [roleGuard],
    data: { roles: ['Conductor', 'Bodega', 'Receptor'] as FieldRole[] },
    title: 'Operaciones de Campo — IstpetDev Móvil'
  },
  {
    path: 'scan',
    loadComponent: () => import('./field/pages/scan/scan.page').then((m) => m.ScanPage),
    canActivate: [roleGuard],
    data: { roles: ['Conductor', 'Bodega', 'Receptor'] as FieldRole[] },
    title: 'Escanear QR de Custodia — IstpetDev Móvil'
  },
  {
    path: 'receipt/:deliveryId',
    loadComponent: () => import('./field/pages/receipt/receipt.page').then((m) => m.ReceiptPage),
    canActivate: [roleGuard],
    data: { roles: ['Conductor', 'Bodega', 'Receptor'] as FieldRole[] },
    title: 'Recepción y Firma — IstpetDev Móvil'
  },
  {
    path: 'sync',
    loadComponent: () => import('./field/pages/sync/sync.page').then((m) => m.SyncPage),
    canActivate: [roleGuard],
    data: { roles: ['Conductor', 'Bodega', 'Receptor'] as FieldRole[] },
    title: 'Cola de Sincronización Outbox — IstpetDev Móvil'
  },
  {
    path: '**',
    redirectTo: 'home'
  }
];
