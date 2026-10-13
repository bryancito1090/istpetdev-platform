import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { SessionStore } from '@entities/session';

@Component({
  selector: 'widget-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
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

          <!-- Menú Central Desplegable Limpio -->
          <nav class="hidden lg:flex items-center gap-6 text-[13px] font-medium text-charcoal-700">
            <a
              routerLink="/dashboard"
              class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
              <span>Rutas PostGIS</span>
              <svg class="w-3 h-3 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </a>

            <a
              routerLink="/dashboard"
              class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
              <span>Matriz de Stock</span>
              <svg class="w-3 h-3 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </a>

            <a
              routerLink="/warehouse"
              class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
              <span>Bodega & Despacho</span>
              <svg class="w-3 h-3 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </a>

            <a
              routerLink="/public-trace"
              class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
              <span>Trazabilidad QR</span>
              <svg class="w-3 h-3 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </a>

            <a
              routerLink="/dashboard"
              class="flex items-center gap-1 text-charcoal-700 hover:text-primary-600 transition-colors py-1">
              <span>Flota C-01</span>
              <svg class="w-3 h-3 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </a>

            <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-primary-50 text-primary-700 border border-primary-100">
              Expo Clean 2026
            </span>
          </nav>
        </div>

        <!-- Acciones Derecha -->
        <div class="flex items-center gap-4">
          @if (sessionStore.isAuthenticated()) {
            <div class="flex items-center gap-3">
              <span class="text-xs font-semibold text-charcoal-900">
                {{ sessionStore.currentUser()?.name }}
              </span>
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
              class="text-xs sm:text-[13px] font-semibold text-charcoal-700 hover:text-primary-600 transition-colors px-2 py-1">
              Inicia sesión
            </a>

            <a
              routerLink="/dashboard"
              class="inline-flex items-center justify-center font-semibold rounded-full text-xs sm:text-[13px] px-4 sm:px-5 py-2 bg-primary-600 hover:bg-primary-700 text-white shadow-xs hover:shadow-[0_4px_12px_rgba(0,167,196,0.35)] transition-all group">
              <span>Ingresar al panel</span>
              <span class="ml-1 text-white/80 group-hover:translate-x-0.5 transition-transform">›</span>
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

  logout(): void {
    this.sessionStore.logout();
    this.router.navigate(['/']);
  }
}
