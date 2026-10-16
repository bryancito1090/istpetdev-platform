import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { HeaderWidget } from '@widgets/header/header.widget';
import { FooterWidget } from '@widgets/footer/footer.widget';
import { SidebarWidget } from '@widgets/sidebar/sidebar.widget';
import { ApiService } from '@shared/api';
import { AuthSession } from '@entities/session';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, HeaderWidget, FooterWidget, SidebarWidget],
  template: `
    @if (isSystemArea() && session.status() === 'authenticated') {
      <!-- Layout con Barra Lateral Estilo Stripe Dashboard para el Sistema Autenticado -->
      <div class="min-h-screen flex bg-white text-slate-800">
        <!-- Barra Lateral Izquierda Stripe -->
        <widget-sidebar></widget-sidebar>

        <!-- Contenedor Principal Derecho -->
        <div class="flex-1 flex flex-col min-w-0 min-h-screen">
          <!-- Barra Superior con Buscador e Iconos de Utilidad Estilo Stripe -->
          <header class="h-14 border-b border-slate-200/80 px-6 sm:px-8 flex items-center justify-between gap-4 bg-white sticky top-0 z-30">
            <!-- Buscador Stripe -->
            <div class="relative w-72 sm:w-80">
              <span class="absolute inset-y-0 left-0 pl-2.5 flex items-center pointer-events-none text-slate-400">
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Buscar en el sistema..."
                class="w-full pl-8 pr-3 py-1.5 bg-[#F4F6F8] hover:bg-slate-100/90 focus:bg-white border border-transparent focus:border-slate-300 rounded-lg text-xs text-slate-800 placeholder-slate-400 outline-none transition-all" />
            </div>

            <!-- Acciones de Utilidad Derecha -->
            <div class="flex items-center gap-3 text-slate-500">

              <!-- Campana de Alertas -->
              <a
                routerLink="/alertas"
                class="relative p-1.5 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
                title="Alertas del sistema">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                </svg>
              </a>

              <!-- Avatar de Usuario -->
              <div class="w-7 h-7 rounded-full bg-slate-900 text-white font-bold text-xs flex items-center justify-center shrink-0 shadow-2xs font-mono ml-1">
                {{ session.me()?.displayName ? session.me()!.displayName.charAt(0).toUpperCase() : 'B' }}
              </div>
            </div>
          </header>

          <!-- Mensajes de Error de API Global -->
          @if (apiErrorMessages().length) {
            <div role="alert" aria-live="assertive" class="mx-6 my-3 rounded-lg border border-rose-200 bg-rose-50 p-3.5 text-xs text-rose-900 shadow-2xs">
              @for (message of apiErrorMessages(); track message) {
                <p class="font-medium">{{ message }}</p>
              }
            </div>
          }

          <!-- Contenido de Página -->
          <main class="flex-1 w-full bg-white">
            <router-outlet></router-outlet>
          </main>
        </div>
      </div>
    } @else {
      <!-- Layout Público con Cabecera y Pie Estándar -->
      <div class="min-h-screen flex flex-col bg-slate-50 text-charcoal-800 overflow-x-hidden">
        <widget-header></widget-header>
        @if (apiErrorMessages().length) {
          <div role="alert" aria-live="assertive" class="mx-4 my-3 rounded-lg border border-rose-300 bg-rose-50 p-4 text-sm text-rose-900">
            @for (message of apiErrorMessages(); track message) {
              <p>{{ message }}</p>
            }
          </div>
        }
        <main class="flex-1 w-full flex flex-col">
          <router-outlet></router-outlet>
        </main>
        <widget-footer></widget-footer>
      </div>
    }
  `
})
export class AppComponent {
  private readonly api = inject(ApiService);
  readonly session = inject(AuthSession);
  readonly apiErrorMessages = computed(() => Object.values(this.api.loadErrors()));
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
}
