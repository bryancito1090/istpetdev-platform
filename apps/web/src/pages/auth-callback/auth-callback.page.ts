import { Component, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSession } from '@entities/session';

/** Vuelta de Cognito: el inicializador ya canjeó el código; aquí se decide el destino. */
@Component({
  selector: 'page-auth-callback',
  standalone: true,
  template: `<p class="p-8 text-center text-sm text-slate-500">Verificando tu cuenta…</p>`,
})
export class AuthCallbackPage {
  private readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  constructor() {
    effect(() => {
      const status = this.session.status();
      if (status === 'loading') return;
      const target = status === 'authenticated' ? this.session.takeReturnUrl() : '/login';
      void this.router.navigateByUrl(target, { replaceUrl: true });
    });
  }
}
