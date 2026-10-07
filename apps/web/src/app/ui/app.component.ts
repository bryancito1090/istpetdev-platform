import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderWidget } from '@widgets/header/header.widget';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderWidget],
  template: `
    <div class="min-h-screen flex flex-col bg-slate-50">
      <widget-header></widget-header>
      <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <router-outlet></router-outlet>
      </main>
      <footer class="bg-white border-t border-slate-200 py-4 text-center text-xs text-slate-500">
        IstpetDev — Expo Clean Ecuador 2026 • Plataforma Logística y Custodia de Insumos
      </footer>
    </div>
  `
})
export class AppComponent {}
