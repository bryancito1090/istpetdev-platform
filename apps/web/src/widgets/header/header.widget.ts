import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, NavigationEnd, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthSession } from '@entities/session';

interface NavItem {
  path: string;
  label: string;
}

/** Menú por rol, en el orden en que se usa el sistema. */
const PLANNER_NAV: NavItem[] = [
  { path: '/inicio', label: '1. Inicio' },
  { path: '/datos', label: '2. Datos' },
  { path: '/alertas', label: '3. Alertas' },
  { path: '/rutas', label: '4. Rutas' }
];

const MANAGER_NAV: NavItem[] = [
  { path: '/inicio', label: 'Inicio' },
  { path: '/datos', label: 'Datos' },
  { path: '/alertas', label: 'Alertas' },
  { path: '/rutas', label: 'Rutas' }
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
    <header class="w-full bg-white sticky top-0 z-50 border-b border-slate-200">
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

        <div class="flex items-center gap-4 shrink-0 text-sm">
          @if (session.status() === 'authenticated') {
            @if (session.role() === 'Gerencia') {
              <a routerLink="/users" class="text-xs font-semibold text-primary-600 hover:text-primary-700">Usuarios</a>
            }
            <span class="hidden sm:inline text-slate-700">
              {{ session.me()?.displayName }}
              <span class="text-slate-400">· {{ roleName() }}</span>
            </span>
            <button type="button" (click)="logout()" class="text-slate-500 hover:text-rose-600">
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
export class HeaderWidget {
  readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  readonly currentUrl = signal(this.router.url);

  constructor() {
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd)
    ).subscribe((e) => {
      this.currentUrl.set(e.urlAfterRedirects);
    });
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

  logout(): void {
    this.session.logout();
  }
}
