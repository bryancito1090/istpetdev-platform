import { Routes } from '@angular/router';
import { roleGuard } from './auth';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full'
  },
  {
    path: 'home',
    loadComponent: () => import('./home/home.page').then((m) => m.HomePage),
    title: 'Operaciones de Campo — IstpetDev Móvil'
  },
  {
    path: 'scan',
    loadComponent: () => import('./field/pages/scan/scan.page').then((m) => m.ScanPage),
    canActivate: [roleGuard],
    data: { roles: ['CarrierDriver', 'WarehouseOperator', 'LogisticsManager'] },
    title: 'Escanear QR de Custodia — IstpetDev Móvil'
  },
  {
    path: 'receipt/:deliveryId',
    loadComponent: () => import('./field/pages/receipt/receipt.page').then((m) => m.ReceiptPage),
    canActivate: [roleGuard],
    data: { roles: ['CarrierDriver', 'WarehouseOperator', 'LogisticsManager'] },
    title: 'Recepción y Firma — IstpetDev Móvil'
  },
  {
    path: 'sync',
    loadComponent: () => import('./field/pages/sync/sync.page').then((m) => m.SyncPage),
    canActivate: [roleGuard],
    data: { roles: ['CarrierDriver', 'WarehouseOperator', 'LogisticsManager'] },
    title: 'Cola de Sincronización Outbox — IstpetDev Móvil'
  },
  {
    path: '**',
    redirectTo: 'home'
  }
];
