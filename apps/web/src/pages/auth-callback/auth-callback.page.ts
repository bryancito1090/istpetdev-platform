import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSession } from '@entities/session';

/** Vuelta de Cognito: el inicializador ya canjeó el código; aquí se decide el destino. */
@Component({
  selector: 'page-auth-callback',
  standalone: true,
  template: `<p class="p-8 text-center text-sm text-slate-500">Verificando tu cuenta…</p>`,
})
export class AuthCallbackPage implements OnInit {
  private readonly session = inject(AuthSession);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const target = this.session.status() === 'authenticated' ? this.session.takeReturnUrl() : '/login';
    void this.router.navigateByUrl(target, { replaceUrl: true });
  }
}
