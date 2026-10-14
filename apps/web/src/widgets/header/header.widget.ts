import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, NavigationEnd, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';

interface NavItem {
  path: string;
  label: string;
}

/** Menú por rol, en el orden en que se usa el sistema. */
const PLANNER_NAV: NavItem[] = [
  { path: '/inicio', label: '1. Inicio' },
  { path: '/datos', label: '2. Datos' },
  { path: '/rutas', label: '3. Rutas' },
  { path: '/bodegas', label: '4. Bodegas & Rebalanceo' }
];

const MANAGER_NAV: NavItem[] = [
  { path: '/inicio', label: 'Inicio' },
  { path: '/datos', label: 'Datos' },
  { path: '/alertas', label: 'Alertas' },
  { path: '/rutas', label: 'Rutas' },
  { path: '/bodegas', label: 'Bodegas' }
];

const AUDITOR_NAV: NavItem[] = [
  { path: '/inicio', label: 'Inicio' },
  { path: '/alertas', label: 'Alertas' },
  { path: '/rutas', label: 'Rutas' }
];

@Component({
  selector: 'widget-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <header class="w-full bg-white sticky top-0 z-30 border-b border-slate-200">
      <div class="max-w-6xl mx-auto px-4 sm:px-6 h-14 flex items-center justify-between gap-6">
        <div class="flex items-center gap-8 min-w-0">
          <a routerLink="/" class="flex items-center gap-1 shrink-0">
            <span class="text-xl font-bold tracking-[-0.04em] text-charcoal-900">istpetdev</span>
            <span class="h-1.5 w-1.5 rounded-full bg-primary-500 inline-block mb-1"></span>
          </a>

          @if (isSystemArea() && session.status() === 'authenticated') {
            <nav class="flex items-center gap-1 text-sm overflow-x-auto">
              @for (item of navItems(); track item.path) {
                <a
                  [routerLink]="item.path"
                  routerLinkActive="bg-primary-50 text-primary-700 font-semibold"
                  class="px-3 py-1.5 rounded-md text-slate-600 hover:text-slate-900 whitespace-nowrap">
                  {{ item.label }}
                </a>
              }
            </nav>
          }
        </div>

        <div class="flex items-center gap-3 sm:gap-4 shrink-0 text-sm">
          @if (session.status() === 'authenticated') {
            @if (session.role() === 'Gerencia') {
              <a routerLink="/users" class="text-xs font-semibold text-primary-600 hover:text-primary-700">Usuarios</a>
            }
            <span class="hidden sm:inline text-slate-700">
              {{ session.me()?.displayName }}
              <span class="text-slate-400">· {{ roleName() }}</span>
            </span>

            <!-- Campana de alertas al lado del botón salir -->
            <a
              routerLink="/alertas"
              [title]="hasAlerts() ? 'Tienes ' + alertCount() + ' alerta(s) de stock activa(s)' : 'Sin alertas de stock'"
              class="relative p-1.5 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-full transition-colors flex items-center justify-center"
              aria-label="Alertas">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
              </svg>
              @if (hasAlerts()) {
                <span class="absolute top-1 right-1 flex h-2.5 w-2.5">
                  <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
                  <span class="relative inline-flex rounded-full h-2.5 w-2.5 bg-rose-600 ring-2 ring-white"></span>
                </span>
              }
            </a>

            <button type="button" (click)="logout()" class="text-slate-500 hover:text-rose-600 font-medium">
              Salir
            </button>
          } @else {
            <a
              routerLink="/login"
              class="inline-flex items-center rounded-full px-4 py-1.5 bg-primary-600 hover:bg-primary-700 text-white font-semibold">
              Iniciar sesión
            </a>
          }
        </div>
      </div>
    </header>
  `
})
export class HeaderWidget implements OnInit {
  readonly session = inject(AuthSession);
  private readonly router = inject(Router);
  private readonly apiService = inject(ApiService);

  readonly currentUrl = signal(this.router.url);
  readonly alertCount = signal(0);
  readonly hasAlerts = computed(() => this.alertCount() > 0);

  constructor() {
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd)
    ).subscribe((e) => {
      this.currentUrl.set(e.urlAfterRedirects);
      this.checkAlerts();
    });
  }

  ngOnInit(): void {
    this.checkAlerts();
  }

  readonly isSystemArea = computed(() => {
    const cleanUrl = this.currentUrl().split('?')[0].split('#')[0];
    return cleanUrl !== '/' && cleanUrl !== '' && cleanUrl !== '/login';
  });

  readonly navItems = computed<NavItem[]>(() => {
    const role = this.session.role();
    if (role === 'Planificador') return PLANNER_NAV;
    if (role === 'Gerencia') return MANAGER_NAV;
    return AUDITOR_NAV;
  });

  readonly roleName = computed(() => {
    return this.session.role() ?? 'Usuario';
  });

  private checkAlerts(): void {
    if (this.session.status() !== 'authenticated') {
      this.alertCount.set(0);
      return;
    }
    this.apiService.getRisks().subscribe(res => {
      if (res?.items) {
        const criticalOrRisky = res.items.filter(
          item => item.level === 'Critical' || item.level === 'High' || item.level === 'Critico' || item.level === 'Alto' || item.level === 'Vigilar'
        );
        this.alertCount.set(criticalOrRisky.length > 0 ? criticalOrRisky.length : res.items.length);
      } else {
        this.alertCount.set(0);
      }
    });
  }

  logout(): void {
    this.session.logout();
  }
}
