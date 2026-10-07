import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'widget-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <header class="bg-white border-b border-slate-200 sticky top-0 z-50">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        <div class="flex items-center gap-8">
          <div class="flex items-center gap-2">
            <div class="h-9 w-9 rounded-lg bg-blue-600 flex items-center justify-center text-white font-bold text-lg shadow-sm">
              I
            </div>
            <div>
              <span class="font-bold text-slate-900 tracking-tight text-lg">IstpetDev</span>
              <span class="text-xs bg-blue-100 text-blue-800 font-semibold px-2 py-0.5 rounded ml-2">Expo Clean</span>
            </div>
          </div>

          <nav class="hidden md:flex items-center gap-1">
            <a
              routerLink="/dashboard"
              routerLinkActive="bg-slate-100 text-blue-600 font-semibold"
              class="px-3 py-2 rounded-md text-sm font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors">
              Dashboard Operativo
            </a>
            <a
              routerLink="/public-trace"
              routerLinkActive="bg-slate-100 text-blue-600 font-semibold"
              class="px-3 py-2 rounded-md text-sm font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors">
              Trazabilidad Pública
            </a>
          </nav>
        </div>

        <div class="flex items-center gap-3">
          <span class="text-xs text-slate-500 bg-slate-100 px-3 py-1.5 rounded-full font-mono">
            v1.0.0 (Angular 22.2)
          </span>
        </div>
      </div>
    </header>
  `
})
export class HeaderWidget {}
