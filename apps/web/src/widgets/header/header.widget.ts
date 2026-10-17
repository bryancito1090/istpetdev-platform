import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, NavigationEnd, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { getMobileAppUrl } from '@shared/config';

interface NavItem {
  path: string;
  label: string;
}

/** Menú por rol, en el orden en que se usa el sistema. */
const PLANNER_NAV: NavItem[] = [
  { path: '/inicio', label: '1. Inicio' },
  { path: '/datos', label: '2. Datos' },
  { path: '/alertas', label: '3. Alertas' },
  { path: '/rutas', label: '4. Rutas' },
  { path: '/novedades', label: '5. Novedades de campo' },
  { path: '/bodegas', label: '6. Bodegas & Rebalanceo' },
  { path: '/simulation', label: '7. Simulador 90d' }
];

const MANAGER_NAV: NavItem[] = [
  { path: '/inicio', label: 'Inicio' },
  { path: '/datos', label: 'Datos' },
  { path: '/alertas', label: 'Alertas' },
  { path: '/rutas', label: 'Rutas' },
  { path: '/novedades', label: 'Novedades' },
  { path: '/bodegas', label: 'Bodegas' },
  { path: '/users', label: 'Usuarios' },
  { path: '/simulation', label: 'Simulador 90d' }
];

const AUDITOR_NAV: NavItem[] = [
  { path: '/inicio', label: 'Inicio' },
  { path: '/alertas', label: 'Alertas' },
  { path: '/rutas', label: 'Rutas' },
  { path: '/novedades', label: 'Novedades' },
  { path: '/simulation', label: 'Simulador 90d' }
];

@Component({
  selector: 'widget-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <header class="w-full bg-white sticky top-0 z-30 border-b border-slate-200/80 backdrop-blur-md">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between gap-6">
        <div class="flex items-center gap-8 min-w-0">
          <a [routerLink]="session.status() === 'authenticated' ? '/inicio' : '/'" class="flex items-center gap-1.5 shrink-0">
            <span class="text-[20px] font-bold tracking-tight text-[#0A2540]">istpetdev</span>
            <span class="h-1.5 w-1.5 rounded-full bg-cyan-500 inline-block mb-1"></span>
          </a>

          @if (session.status() === 'authenticated') {
            <nav class="flex items-center gap-1 text-xs overflow-x-auto">
              @for (item of navItems(); track item.path) {
                <a
                  [routerLink]="item.path"
                  routerLinkActive="bg-slate-100 text-slate-900 font-semibold shadow-2xs"
                  class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50/80 transition-all whitespace-nowrap font-medium">
                  {{ item.label }}
                </a>
              }
            </nav>
          } @else {
            <nav class="hidden md:flex items-center gap-8 text-[14px] font-medium text-[#425466]">
              <a href="#soluciones" class="hover:text-[#0A2540] flex items-center gap-1 transition-colors">
                <span>Soluciones</span>
                <svg class="w-2.5 h-2.5 text-slate-400 mt-0.5" viewBox="0 0 10 10" fill="currentColor"><path d="M2 3.5L5 6.5L8 3.5H2Z"/></svg>
              </a>
              <a href="#flujo" class="hover:text-[#0A2540] flex items-center gap-1 transition-colors">
                <span>Operaciones</span>
                <svg class="w-2.5 h-2.5 text-slate-400 mt-0.5" viewBox="0 0 10 10" fill="currentColor"><path d="M2 3.5L5 6.5L8 3.5H2Z"/></svg>
              </a>
              <a href="#metricas" class="hover:text-[#0A2540] transition-colors">Métricas 90d</a>
              <a routerLink="/public-trace" class="hover:text-[#0A2540] transition-colors">Trazabilidad QR</a>
              <a [href]="mobileAppUrl" target="_blank" rel="noopener noreferrer" class="hover:text-[#0A2540] transition-colors">App Móvil PWA</a>
            </nav>
          }
        </div>

        <div class="flex items-center gap-3 sm:gap-4 shrink-0 text-sm">
          @if (session.status() === 'authenticated') {
            @if (session.role() === 'Gerencia') {
              <a routerLink="/users" class="text-xs font-semibold text-cyan-600 hover:text-cyan-700">Usuarios</a>
            }
            <div class="hidden sm:flex items-center gap-2 text-xs">
              <span class="w-7 h-7 rounded-full bg-slate-100 border border-slate-200 text-slate-700 font-semibold flex items-center justify-center font-mono">
                {{ session.me()?.displayName ? session.me()!.displayName.charAt(0).toUpperCase() : 'U' }}
              </span>
              <span class="text-slate-800 font-medium">
                {{ session.me()?.displayName }}
              </span>
              <span class="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-100 text-slate-600 border border-slate-200/60 font-semibold">
                {{ roleName() }}
              </span>
            </div>

            <!-- Campana de alertas al lado del botón salir -->
            <a
              routerLink="/alertas"
              [title]="hasAlerts() ? 'Tienes ' + alertCount() + ' alerta(s) de stock activa(s)' : 'Sin alertas de stock'"
              class="relative p-2 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-lg transition-colors flex items-center justify-center cursor-pointer"
              aria-label="Alertas">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
              </svg>
              @if (hasAlerts()) {
                <span class="absolute top-1.5 right-1.5 flex h-2 w-2">
                  <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
                  <span class="relative inline-flex rounded-full h-2 w-2 bg-rose-600"></span>
                </span>
              }
            </a>

            <button type="button" (click)="logout()" class="text-xs font-semibold text-slate-500 hover:text-rose-600 transition-colors cursor-pointer px-2 py-1 rounded-md hover:bg-slate-50">
              Salir
            </button>
          } @else {
            <div class="flex items-center gap-2 sm:gap-3">
              <button
                type="button"
                (click)="login()"
                class="inline-flex items-center justify-center rounded-[6px] bg-expo-blue hover:bg-expo-blueDark px-4 py-1.5 text-white font-medium text-[13px] shadow-xs transition-all hover:shadow hover:-translate-y-0.5 active:translate-y-0 cursor-pointer">
                Iniciar sesión
              </button>
            </div>
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
  readonly mobileAppUrl = getMobileAppUrl();

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

  login(): void {
    this.session.login('/inicio');
  }

  logout(): void {
    this.session.logout();
  }
}
