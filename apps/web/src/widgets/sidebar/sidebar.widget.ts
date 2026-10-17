import { Component, inject, computed, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { getMobileAppUrl } from '@shared/config';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'widget-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <aside class="w-60 h-screen sticky top-0 bg-[#FBFBFC] border-r border-slate-200/80 flex flex-col justify-between shrink-0 select-none z-40">
      <div class="flex flex-col min-h-0 overflow-y-auto">
        <!-- Espacio de Trabajo / Selector de Cuenta Estilo Stripe -->
        <div class="p-4 border-b border-slate-200/70">
          <div class="flex items-center justify-between group cursor-pointer p-1.5 -m-1.5 rounded-lg hover:bg-slate-100/80 transition-colors">
            <div class="flex items-center gap-2.5 min-w-0">
              <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-slate-900 to-slate-800 text-white font-bold text-sm flex items-center justify-center shadow-xs shrink-0">
                <span>I</span>
              </div>
              <div class="min-w-0">
                <div class="text-[13px] font-bold text-slate-900 truncate leading-tight tracking-tight">
                  istpetdev
                </div>
                <div class="text-[11px] text-slate-500 truncate leading-tight mt-0.5">
                  {{ session.me()?.displayName ?? 'Bryan Baño' }}
                </div>
              </div>
            </div>
            <svg class="w-4 h-4 text-slate-400 group-hover:text-slate-600 transition-colors shrink-0" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 3a.75.75 0 01.55.24l3.25 3.5a.75.75 0 11-1.1 1.02L10 4.852 7.3 7.76a.75.75 0 01-1.1-1.02l3.25-3.5A.75.75 0 0110 3zm-3.75 8.24a.75.75 0 011.1.02L10 14.148l2.7-2.888a.75.75 0 111.1 1.02l-3.25 3.5a.75.75 0 01-1.1 0l-3.25-3.5a.75.75 0 01.02-1.1z" clip-rule="evenodd" />
            </svg>
          </div>
        </div>

        <!-- Navegación Principal Estilo Stripe -->
        <div class="p-3 space-y-0.5">
          <div class="px-2.5 py-1 text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">
            Navegación
          </div>

          <a
            routerLink="/inicio"
            routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
            [routerLinkActiveOptions]="{ exact: true }"
            class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
            <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25" />
            </svg>
            <span>Inicio</span>
          </a>

          <a
            routerLink="/datos"
            routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
            class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
            <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round" d="M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375m16.5 5.625c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125m16.5 5.625c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125" />
            </svg>
            <span>Datos</span>
          </a>

          <a
            routerLink="/alertas"
            routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
            class="flex items-center justify-between px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
            <div class="flex items-center gap-2.5">
              <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
              </svg>
              <span>Monitoreo de Cobertura</span>
            </div>
            @if (hasAlerts()) {
              <span class="inline-flex items-center justify-center px-1.5 py-0.5 rounded-full text-[10px] font-bold bg-rose-50 text-rose-600 border border-rose-200">
                {{ alertCount() }}
              </span>
            }
          </a>

          <a
            routerLink="/rutas"
            routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
            class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
            <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 6.75V15m6-6v8.25m.503 3.498l4.875-2.437c.381-.19.622-.58.622-1.006V4.82c0-.836-.88-1.38-1.628-1.006l-3.869 1.934c-.317.159-.69.159-1.006 0L9.503 3.252a1.125 1.125 0 00-1.006 0L3.622 5.69C3.24 5.88 3 6.27 3 6.695V19.18c0 .836.88 1.38 1.628 1.006l3.869-1.934c.317-.159.69-.159 1.006 0l4.994 2.497c.317.158.69.158 1.006 0z" />
            </svg>
            <span>Rutas</span>
          </a>

          <a
            routerLink="/novedades"
            routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
            class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
            <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round" d="M11.35 3.836c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m8.9-4.414c.376.023.75.05 1.124.08 1.131.09 1.976 1.053 1.976 2.188V19.5a2.25 2.25 0 01-2.25 2.25H6.75A2.25 2.25 0 014.5 19.5V6.25c0-1.135.845-2.098 1.976-2.188.374-.03.748-.057 1.124-.08" />
            </svg>
            <span>Novedades</span>
          </a>

          <a
            routerLink="/bodegas"
            routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
            class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
            <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 18.75a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 01-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 00-3.213-9.193 2.056 2.056 0 00-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 00-10.026 0 1.106 1.106 0 00-.987 1.106v7.635m12-6.635l-1.314 3.942a1.5 1.5 0 01-1.423 1.025H12" />
            </svg>
            <span>Bodegas</span>
          </a>

          @if (session.role() === 'Gerencia') {
            <a
              routerLink="/users"
              routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
              class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
              <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
              </svg>
              <span>Usuarios</span>
            </a>
          }

          <a
            routerLink="/simulation"
            routerLinkActive="text-[#635BFF] font-medium bg-[#635BFF]/[0.07] [&_svg]:text-[#635BFF]"
            class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-[14px] leading-[20px] text-[#4F5B66] hover:text-[#1A2C44] hover:bg-slate-100/60 transition-colors font-normal group">
            <svg class="w-4 h-4 text-[#697386] group-hover:text-[#1A2C44] transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 3v11.25A2.25 2.25 0 006 16.5h2.25M3.75 3h-1.5m1.5 0h16.5m0 0h1.5m-1.5 0v11.25A2.25 2.25 0 0118 16.5h-2.25m-7.5 0h7.5m-7.5 0l-1 3m8.5-3l1 3m0 0l.5 1.5m-.5-1.5h-9.5m0 0l-.5 1.5m.75-9l3-3 2.25 2.25L15 7.5" />
            </svg>
            <span>Simulador 90d</span>
          </a>
        </div>

        <!-- Sección de Operaciones y Campo (como Productos en Stripe) -->
        <div class="p-3 border-t border-slate-200/60 space-y-1">
          <div class="px-2.5 py-1 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
            Operaciones
          </div>

          <a
            routerLink="/public-trace"
            class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-lg text-[13px] text-slate-600 hover:text-slate-900 hover:bg-slate-100/70 transition-colors">
            <svg class="w-4 h-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 4.875c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5A1.125 1.125 0 013.75 9.375v-4.5zM3.75 14.625c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5a1.125 1.125 0 01-1.125-1.125v-4.5zM13.5 4.875c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5A1.125 1.125 0 0113.5 9.375v-4.5z" />
            </svg>
            <span>Trazabilidad QR</span>
          </a>

          <a
            [href]="mobileAppUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="flex items-center justify-between px-2.5 py-1.5 rounded-lg text-[13px] text-slate-600 hover:text-slate-900 hover:bg-slate-100/70 transition-colors">
            <div class="flex items-center gap-2.5">
              <svg class="w-4 h-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 1.5H8.25A2.25 2.25 0 006 3.75v16.5a2.25 2.25 0 002.25 2.25h7.5A2.25 2.25 0 0018 20.25V3.75a2.25 2.25 0 00-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 18.75h3" />
              </svg>
              <span>App Móvil PWA</span>
            </div>
            <svg class="w-3 h-3 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 003 8.25v10.5A2.25 2.25 0 005.25 21h10.5A2.25 2.25 0 0018 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
            </svg>
          </a>
        </div>
      </div>

      <!-- Pie de Barra Lateral (como Desarrolladores en Stripe) -->
      <div class="p-3 border-t border-slate-200/70 space-y-2 bg-[#F8FAFC]">
        <div class="flex items-center justify-between px-2 py-1 text-xs">
          <div class="flex items-center gap-2 text-slate-600 font-mono text-[11px]">
            <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
            <span>AWS EC2</span>
          </div>
          <span class="text-[10px] font-mono text-slate-400 font-semibold">
            {{ session.role() ?? 'Gerencia' }}
          </span>
        </div>

        <button
          type="button"
          (click)="logout()"
          class="w-full flex items-center justify-center gap-2 px-3 py-1.5 rounded-lg text-xs font-semibold text-slate-600 hover:text-rose-600 hover:bg-rose-50/70 border border-slate-200/60 bg-white transition-colors cursor-pointer shadow-2xs">
          <svg class="w-3.5 h-3.5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
          </svg>
          <span>Cerrar sesión</span>
        </button>
      </div>
    </aside>
  `
})
export class SidebarWidget implements OnInit {
  readonly session = inject(AuthSession);
  private readonly apiService = inject(ApiService);
  readonly mobileAppUrl = getMobileAppUrl();

  readonly hasAlerts = signal(false);
  readonly alertCount = signal(0);

  ngOnInit(): void {
    if (this.apiService.getRisks) {
      this.apiService.getRisks().subscribe({
        next: (risks) => {
          if (risks && risks.items) {
            const count = risks.items.filter(i => i.level === 'Critico' || i.level === 'Critical').length;
            this.alertCount.set(count);
            this.hasAlerts.set(count > 0);
          }
        },
        error: () => {
          // Mantener silencioso para no romper vista
        }
      });
    }
  }

  logout(): void {
    this.session.logout();
  }
}
