import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { SessionStore } from '@entities/session';
import { BadgeComponent } from '@shared/ui';

@Component({
  selector: 'widget-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, BadgeComponent],
  template: `
    <header class="bg-white border-b border-slate-200 sticky top-0 z-50 shadow-xs">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        <!-- Logo e Identidad -->
        <div class="flex items-center gap-8">
          <a routerLink="/" class="flex items-center gap-2.5 group">
            <div class="h-9 w-9 rounded-lg bg-primary-600 flex items-center justify-center text-white font-bold text-lg shadow-sm group-hover:bg-primary-700 transition-colors">
              I
            </div>
            <div class="flex flex-col">
              <div class="flex items-center gap-2">
                <span class="font-bold text-slate-900 tracking-tight text-lg">IstpetDev</span>
                <ui-badge variant="success" size="sm" label="EXPO CLEAN"></ui-badge>
              </div>
              <span class="text-[10px] text-slate-500 font-medium">Logística Hospitalaria Ecuador</span>
            </div>
          </a>

          <!-- Navegación Principal FSD -->
          <nav class="hidden md:flex items-center gap-1">
            <a
              routerLink="/"
              routerLinkActive="bg-primary-50 text-primary-700 font-semibold"
              [routerLinkActiveOptions]="{ exact: true }"
              class="px-3 py-2 rounded-md text-sm font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors">
              Inicio
            </a>
            <a
              routerLink="/dashboard"
              routerLinkActive="bg-primary-50 text-primary-700 font-semibold"
              class="px-3 py-2 rounded-md text-sm font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors">
              Panel Operativo
            </a>
            <a
              routerLink="/public-trace"
              routerLinkActive="bg-primary-50 text-primary-700 font-semibold"
              class="px-3 py-2 rounded-md text-sm font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors">
              Trazabilidad Pública
            </a>
          </nav>
        </div>

        <!-- Acciones y Autenticación -->
        <div class="flex items-center gap-3">
          @if (sessionStore.isAuthenticated()) {
            <!-- Perfil de Usuario Autenticado -->
            <div class="flex items-center gap-3">
              <div class="hidden sm:flex flex-col items-end">
                <span class="text-xs font-bold text-slate-900 leading-tight">
                  {{ sessionStore.currentUser()?.name }}
                </span>
                <div class="flex items-center gap-1.5 mt-0.5">
                  <span class="text-[10px] text-slate-500 truncate max-w-[150px]">
                    {{ sessionStore.currentUser()?.facility }}
                  </span>
                  <ui-badge 
                    [variant]="sessionStore.userRole() === 'SUPERVISOR' ? 'transit' : sessionStore.userRole() === 'AUDITOR' ? 'warning' : 'success'" 
                    size="sm" 
                    [showIcon]="false"
                    [label]="sessionStore.currentUser()?.role">
                  </ui-badge>
                </div>
              </div>

              <!-- Avatar con iniciales -->
              <div class="h-8 w-8 rounded-full bg-slate-800 text-white font-bold text-xs flex items-center justify-center border border-slate-700 shadow-xs">
                {{ sessionStore.currentUser()?.avatarInitials }}
              </div>

              <!-- Botón Cerrar Sesión -->
              <button
                type="button"
                (click)="logout()"
                class="px-2.5 py-1.5 text-xs font-medium text-slate-500 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors border border-transparent hover:border-red-200"
                aria-label="Cerrar sesión">
                Salir
              </button>
            </div>
          } @else {
            <!-- Botón Obligatorio: "Ingresar al sistema" -->
            <div class="flex items-center gap-2">
              <a
                routerLink="/login"
                class="inline-flex items-center justify-center font-medium rounded-lg text-sm px-4 py-2 bg-primary-600 hover:bg-primary-700 text-white shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 transition-all active:scale-[0.98]">
                <span>Ingresar al sistema</span>
                <svg class="w-4 h-4 ml-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
                </svg>
              </a>
            </div>
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
