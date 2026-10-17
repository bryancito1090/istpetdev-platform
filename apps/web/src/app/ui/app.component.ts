import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { HeaderWidget } from '@widgets/header/header.widget';
import { FooterWidget } from '@widgets/footer/footer.widget';
import { SidebarWidget } from '@widgets/sidebar/sidebar.widget';
import { ApiService } from '@shared/api';
import { AuthSession } from '@entities/session';
import { AlertNotificationService, AlertToastComponent } from '@features/alert-notification';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, HeaderWidget, FooterWidget, SidebarWidget, AlertToastComponent],
  template: `
    @if (isSystemArea() && session.status() === 'authenticated') {
      <!-- Layout con Barra Lateral Estilo Stripe Dashboard para el Sistema Autenticado -->
      <div class="min-h-screen flex bg-white text-slate-800">
        <!-- Barra Lateral Izquierda Stripe -->
        <widget-sidebar></widget-sidebar>

        <!-- Contenedor Principal Derecho -->
        <div class="flex-1 flex flex-col min-w-0 min-h-screen">
          <!-- Barra Superior con Buscador e Iconos de Utilidad Estilo Stripe -->
          <header class="h-14 border-b border-[#E3E8EE] px-6 sm:px-8 flex items-center justify-between gap-4 bg-white sticky top-0 z-30">
            <!-- Buscador Stripe -->
            <div class="relative w-72 sm:w-80">
              <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-[#8792A2]">
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Buscar"
                class="w-full h-8 pl-8 pr-3 bg-[#F1F3F5] hover:bg-slate-100 focus:bg-white border border-transparent focus:border-[#635BFF] rounded-md text-xs text-[#1A2C44] placeholder-[#8792A2] outline-none transition-all shadow-2xs" />
            </div>

            <!-- Acciones de Utilidad Derecha Fieles a Stripe -->
            <div class="flex items-center gap-2 sm:gap-2.5 text-[#697386]">
              <!-- Icono Cuadrícula / Módulos -->
              <a
                routerLink="/inicio"
                class="p-1.5 hover:text-[#1A2C44] hover:bg-[#F1F3F5] rounded-md transition-colors"
                title="Panel de Módulos">
                <svg class="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM11 13a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                </svg>
              </a>

              <!-- Icono Ayuda -->
              <a
                routerLink="/novedades"
                class="p-1.5 hover:text-[#1A2C44] hover:bg-[#F1F3F5] rounded-md transition-colors"
                title="Ayuda y Novedades">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M12 18h.01" />
                </svg>
              </a>

              <!-- Campana de Alertas -->
              <a
                routerLink="/alertas"
                class="relative p-1.5 hover:text-[#1A2C44] hover:bg-[#F1F3F5] rounded-md transition-colors"
                [title]="alertNotification?.hasAlerts() ? 'Tienes ' + alertNotification.alertCount() + ' alerta(s) de stock activa(s)' : 'Sin alertas de stock'"
                aria-label="Alertas">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                </svg>
                @if (alertNotification?.hasAlerts()) {
                  <span class="absolute top-1 right-1 flex h-2 w-2">
                    @if (alertNotification.hasNewAlert()) {
                      <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
                    }
                    <span class="relative inline-flex rounded-full h-2 w-2 bg-rose-600"></span>
                  </span>
                }
              </a>

              <!-- Icono Engranaje / Ajustes -->
              <a
                routerLink="/users"
                class="p-1.5 hover:text-[#1A2C44] hover:bg-[#F1F3F5] rounded-md transition-colors"
                title="Ajustes y Usuarios">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </a>

              <!-- Botón Circular '+' -->
              <a
                routerLink="/datos"
                class="w-6 h-6 rounded-full bg-[#635BFF] hover:bg-[#5248E0] text-white flex items-center justify-center transition-colors ml-0.5 shadow-2xs"
                title="Cargar o actualizar catálogos">
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
              </a>

              <!-- Píldora de Estado / Operación -->
              <div class="hidden md:flex items-center gap-1.5 pl-2 ml-1 border-l border-[#E3E8EE] text-xs text-[#4F566B]">
                <span>Modo operativo</span>
                <span class="w-3.5 h-3.5 rounded-full border-2 border-[#635BFF] border-t-transparent animate-spin inline-block"></span>
              </div>

              <!-- Avatar de Usuario -->
              <div class="w-7 h-7 rounded-full bg-[#1A2C44] text-white font-bold text-xs flex items-center justify-center shrink-0 shadow-2xs font-sans ml-1">
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
    } @else if (isLandingPage()) {
      <!-- Layout Inmersivo de Landing Page: 100% fiel al repo de referencia (sin cabecera ni pie blancos) -->
      <main class="min-h-screen w-full bg-[#060D1A] text-[#F0F4F8] overflow-x-clip">
        @if (apiErrorMessages().length) {
          <div role="alert" aria-live="assertive" class="mx-4 my-3 rounded-lg border border-rose-300 bg-rose-50 p-4 text-sm text-rose-900">
            @for (message of apiErrorMessages(); track message) {
              <p>{{ message }}</p>
            }
          </div>
        }
        <router-outlet></router-outlet>
      </main>
    } @else {
      <!-- Layout Público con Cabecera y Pie Estándar para otras páginas públicas (ej. login) -->
      <div class="min-h-screen flex flex-col bg-slate-50 text-charcoal-800 overflow-x-clip">
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
    <ui-alert-toast></ui-alert-toast>
  `
})
export class AppComponent {
  private readonly api = inject(ApiService);
  readonly session = inject(AuthSession);
  readonly alertNotification = inject(AlertNotificationService, { optional: true });
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

  readonly isLandingPage = computed(() => {
    const url = this.currentUrl();
    const cleanUrl = url.split('?')[0].split('#')[0];
    return cleanUrl === '/' || cleanUrl === '';
  });

  readonly isSystemArea = computed(() => {
    const url = this.currentUrl();
    const cleanUrl = url.split('?')[0].split('#')[0];
    return cleanUrl !== '/' && cleanUrl !== '' && cleanUrl !== '/login';
  });
}
