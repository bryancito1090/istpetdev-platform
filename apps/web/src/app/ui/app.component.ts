import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { HeaderWidget } from '@widgets/header/header.widget';
import { FooterWidget } from '@widgets/footer/footer.widget';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, HeaderWidget, FooterWidget],
  template: `
    <div class="min-h-screen flex flex-col bg-white text-charcoal-800 overflow-x-hidden">
      <!-- Barra Sutil F7: Entorno Frontend-First con Datos Simulados -->
      <aside aria-label="Aviso de entorno simulado" class="bg-charcoal-950 text-slate-300 text-[11px] py-1 px-4 border-b border-charcoal-800 select-none">
        <div class="max-w-7xl mx-auto flex items-center justify-between gap-2">
          <div class="flex items-center gap-2">
            <span class="inline-flex h-2 w-2 rounded-full bg-primary-400 animate-pulse"></span>
            <span>
              Entorno Frontend-First: <strong class="text-white">Datos simulados (useMockApi)</strong>
            </span>
          </div>
          <div class="flex items-center gap-4">
            <a routerLink="/warehouse" class="text-primary-400 hover:text-white underline font-semibold transition-colors">
              Bodega & QR →
            </a>
            <span class="font-mono text-[10px] text-slate-400 bg-white/10 px-1.5 py-0.5 rounded">
              Spec 004
            </span>
          </div>
        </div>
      </aside>

      <widget-header></widget-header>
      <main class="flex-1 w-full flex flex-col">
        <router-outlet></router-outlet>
      </main>
      <widget-footer></widget-footer>
    </div>
  `
})
export class AppComponent {}
