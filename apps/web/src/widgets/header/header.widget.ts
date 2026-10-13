import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, NavigationEnd, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs/operators';
import { SessionStore } from '@entities/session';

@Component({
  selector: 'widget-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <header class="w-full bg-white/70 backdrop-blur-md sticky top-0 z-50 transition-all border-b border-slate-100">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between gap-6">
        
        <!-- Logo Puro Texto con Token Primario -->
        <div class="flex items-center gap-8 shrink-0">
          <a routerLink="/" class="flex items-center gap-1 focus:outline-none group">
            <span class="text-xl sm:text-2xl font-bold tracking-[-0.04em] text-charcoal-900 group-hover:text-primary-600 transition-colors font-sans">
              istpetdev
            </span>
            <span class="h-1.5 w-1.5 rounded-full bg-primary-500 inline-block mb-1"></span>
          </a>

          <!-- Menú Central: Solo visible DENTRO DEL SISTEMA -->
          @if (isSystemArea()) {
            <nav class="hidden lg:flex items-center gap-6 text-[13px] font-medium text-charcoal-700">
              <a
                routerLink="/dashboard"
                fragment="routes-map"
                routerLinkActive="text-primary-600 font-semibold"
                class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
                <span>Rutas PostGIS</span>
              </a>

              @if (!isAuditor()) {
                <a
                  routerLink="/dashboard"
                  fragment="stock-matrix"
                  routerLinkActive="text-primary-600 font-semibold"
                  class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
                  <span>Matriz de Stock</span>
                </a>
              }

              <a
                routerLink="/public-trace"
                routerLinkActive="text-primary-600 font-semibold"
                class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
                <span>Trazabilidad QR</span>
              </a>

              @if (!isAuditor()) {
                <a
                  routerLink="/dashboard"
                  fragment="routes-map"
                  routerLinkActive="text-primary-600 font-semibold"
                  class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
                  <span>Flota C-01</span>
                </a>
              }

              <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-primary-50 text-primary-700 border border-primary-100">
                Expo Clean 2026
              </span>
            </nav>
          }
        </div>

          <!-- Acciones Derecha -->
          <div class="flex items-center gap-4">
            @if (sessionStore.isAuthenticated()) {
              <div class="flex items-center gap-3">
                <span class="text-xs font-semibold text-charcoal-900">
                  {{ sessionStore.currentUser()?.name }}
                </span>
                @if (!isSystemArea()) {
                  <a
                    routerLink="/dashboard"
                    class="text-xs font-semibold text-primary-600 hover:text-primary-700 transition-colors">
                    Ir al panel →
                  </a>
                }
                <button
                  type="button"
                  (click)="logout()"
                  class="text-xs text-slate-500 hover:text-rose-600 transition-colors">
                  Salir
                </button>
              </div>
            } @else {
              <a
                routerLink="/login"
                class="inline-flex items-center justify-center font-semibold rounded-full text-xs sm:text-[13px] px-4 sm:px-5 py-2 bg-primary-600 hover:bg-primary-700 text-white shadow-xs hover:shadow-[0_4px_12px_rgba(0,167,196,0.35)] transition-all">
                Iniciar sesión
              </a>
            }
          </div>
      </div>
    </header>
  `
})
export class HeaderWidget {
  readonly sessionStore = inject(SessionStore);
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
    const url = this.currentUrl();
    const cleanUrl = url.split('?')[0].split('#')[0];
    return cleanUrl !== '/' && cleanUrl !== '' && cleanUrl !== '/login';
  });

  readonly isAuditor = computed(() => this.sessionStore.userRole() === 'AUDITOR');

  logout(): void {
    this.sessionStore.logout();
    this.router.navigate(['/']);
  }
}
