import { Component, inject, signal, computed } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { HeaderWidget } from '@widgets/header/header.widget';
import { FooterWidget } from '@widgets/footer/footer.widget';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderWidget, FooterWidget],
  template: `
    <div class="min-h-screen flex flex-col bg-white text-charcoal-800 overflow-x-hidden">
      <!-- Barra Sutil F7: Entorno Frontend-First con Datos Simulados (Solo dentro del sistema) -->
      @if (isSystemArea()) {
        <aside aria-label="Aviso de entorno simulado" class="bg-charcoal-950 text-slate-300 text-[11px] py-1 px-4 border-b border-charcoal-800 select-none">
          <div class="max-w-7xl mx-auto flex items-center justify-between gap-2">
            <div class="flex items-center gap-2">
              <span class="inline-flex h-2 w-2 rounded-full bg-primary-400 animate-pulse"></span>
              <span>
                Entorno Frontend-First: <strong class="text-white">Datos simulados (useMockApi)</strong>
              </span>
            </div>
            <div class="flex items-center gap-4">
              <span class="font-mono text-[10px] text-slate-400 bg-white/10 px-2 py-0.5 rounded">
                Torre de Control Web • Expo Clean 2026
              </span>
            </div>
          </div>
        </aside>
      }

      <widget-header></widget-header>
      <main class="flex-1 w-full flex flex-col">
        <router-outlet></router-outlet>
      </main>
      <widget-footer></widget-footer>
    </div>
  `
})
export class AppComponent {
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
