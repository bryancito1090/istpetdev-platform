import { Routes } from '@angular/router';
import { roleGuard } from './auth';

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
    path: 'home',
    loadComponent: () => import('./home/home.page').then((m) => m.HomePage),
    canActivate: [roleGuard],
    title: 'Operaciones de Campo — IstpetDev Móvil'
  },
  {
    path: 'scan',
    loadComponent: () => import('./field/pages/scan/scan.page').then((m) => m.ScanPage),
    canActivate: [roleGuard],
    data: { roles: ['CarrierDriver', 'WarehouseOperator', 'LogisticsManager', 'HospitalReceiver'] },
    title: 'Escanear QR de Custodia — IstpetDev Móvil'
  },
  {
    path: 'receipt/:deliveryId',
    loadComponent: () => import('./field/pages/receipt/receipt.page').then((m) => m.ReceiptPage),
    canActivate: [roleGuard],
    data: { roles: ['CarrierDriver', 'WarehouseOperator', 'LogisticsManager', 'HospitalReceiver'] },
    title: 'Recepción y Firma — IstpetDev Móvil'
  },
  {
    path: 'sync',
    loadComponent: () => import('./field/pages/sync/sync.page').then((m) => m.SyncPage),
    canActivate: [roleGuard],
    data: { roles: ['CarrierDriver', 'WarehouseOperator', 'LogisticsManager', 'HospitalReceiver'] },
    title: 'Cola de Sincronización Outbox — IstpetDev Móvil'
  },
  {
    path: '**',
    redirectTo: 'home'
  }
];
