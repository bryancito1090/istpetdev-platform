import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('@pages/operations-dashboard/operations-dashboard.page').then(m => m.OperationsDashboardPage)
  },
  {
    path: 'public-trace',
    loadComponent: () => import('@pages/public-trace/public-trace.page').then(m => m.PublicTracePage)
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
