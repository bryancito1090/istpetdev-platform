import { Component, inject, signal, computed } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { HeaderWidget } from '@widgets/header/header.widget';
import { ApiService } from '@shared/api';
import { FooterWidget } from '@widgets/footer/footer.widget';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderWidget, FooterWidget],
  template: `
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
      <!-- El pie solo se muestra en el portal público; dentro del sistema estorba. -->
      @if (!isSystemArea()) {
        <widget-footer></widget-footer>
      }
    </div>
  `
})
export class AppComponent {
  private readonly api = inject(ApiService);
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
