import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'widget-footer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <footer class="bg-white border-t border-slate-200 mt-auto">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-8 mb-8">
          <!-- Columna 1: Identidad del Proyecto -->
          <div class="md:col-span-2 space-y-3">
            <div class="flex items-center gap-2">
              <div class="h-7 w-7 rounded-lg bg-primary-600 flex items-center justify-center text-white font-bold text-sm">
                I
              </div>
              <span class="font-bold text-slate-900 tracking-tight text-base">IstpetDev Platform</span>
              <span class="text-[10px] bg-secondary-100 text-secondary-900 border border-secondary-300 font-semibold px-2 py-0.5 rounded">Reto 1 • 2026</span>
            </div>
            <p class="text-xs text-slate-500 max-w-md leading-relaxed">
              Optimización y trazabilidad logística para el abastecimiento de insumos a nivel nacional.
              Desarrollado para Expo Clean Ecuador 2026 bajo estándares de Clean Architecture y Feature-Sliced Design.
            </p>
            <div class="flex items-center gap-3 text-xs text-slate-500 font-mono">
              <span class="inline-flex items-center gap-1.5">
                <span class="h-2 w-2 rounded-full bg-secondary-500"></span>
                PostGIS 3.4
              </span>
              <span>•</span>
              <span>UUID v7 Ledger</span>
              <span>•</span>
              <span>Angular 22.2 & .NET 8</span>
            </div>
          </div>

          <!-- Columna 2: Navegación Rápida -->
          <div>
            <h4 class="text-xs font-semibold text-slate-900 uppercase tracking-wider mb-3">Navegación</h4>
            <ul class="space-y-2 text-xs">
              <li>
                <a routerLink="/" class="text-slate-600 hover:text-primary-600 transition-colors">Portal Institucional</a>
              </li>
              <li>
                <a routerLink="/dashboard" class="text-slate-600 hover:text-primary-600 transition-colors">Panel Operativo</a>
              </li>
              <li>
                <a routerLink="/public-trace" class="text-slate-600 hover:text-primary-600 transition-colors">Consulta de Custodia</a>
              </li>
              <li>
                <a routerLink="/login" class="text-slate-600 hover:text-primary-600 transition-colors">Acceso Operativo</a>
              </li>
            </ul>
          </div>

          <!-- Columna 3: Arquitectura y Cumplimiento -->
          <div>
            <h4 class="text-xs font-semibold text-slate-900 uppercase tracking-wider mb-3">Seguridad & Normativa</h4>
            <ul class="space-y-2 text-xs text-slate-500">
              <li class="flex items-center gap-1.5">
                <span class="text-slate-400">Auth:</span>
                <span class="font-medium text-slate-700">Amazon Cognito OIDC</span>
              </li>
              <li class="flex items-center gap-1.5">
                <span class="text-slate-400">Canal:</span>
                <span class="font-medium text-slate-700">SSM Port Forwarding</span>
              </li>
              <li class="flex items-center gap-1.5">
                <span class="text-slate-400">Políticas:</span>
                <span class="font-medium text-slate-700">Least Privilege DML</span>
              </li>
              <li class="flex items-center gap-1.5">
                <span class="text-slate-400">Patrón:</span>
                <span class="font-medium text-slate-700">Clean Architecture</span>
              </li>
            </ul>
          </div>
        </div>

        <!-- Barra inferior -->
        <div class="pt-6 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-400">
          <p>© 2026 IstpetDev — Todos los derechos reservados. Hackathon Expo Clean Ecuador.</p>
          <div class="flex items-center gap-4">
            <span class="font-mono text-[11px] bg-slate-100 text-slate-600 px-2.5 py-1 rounded">
              v1.0.0-rc2 (Build 2026.10)
            </span>
          </div>
        </div>
      </div>
    </footer>
  `
})
export class FooterWidget {}
