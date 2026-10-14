import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import { ButtonComponent, CardComponent } from '@shared/ui';

/** FR-01: esta página no recibe contraseñas; abre la página administrada de Cognito. */
@Component({
  selector: 'page-login',
  standalone: true,
  imports: [RouterLink, ButtonComponent, CardComponent],
  template: `
    <div class="min-h-[75vh] flex flex-col items-center justify-center py-6 sm:py-12">
      <div class="w-full max-w-md space-y-6 text-center">
        <div class="space-y-2">
          <div class="inline-flex items-center justify-center h-12 w-12 rounded-xl bg-primary-600 text-white font-black text-2xl shadow-md">I</div>
          <h1 class="text-2xl font-extrabold text-slate-900 tracking-tight">Acceso al sistema logístico</h1>
        </div>
        <ui-card [padding]="'lg'">
          @switch (session.status()) {
            @case ('wrong-platform') {
              <p role="alert" class="text-sm font-semibold text-slate-800 mb-4">Tu cuenta opera en la app de campo</p>
              <ui-button variant="outline" [fullWidth]="true" (buttonClick)="session.logout()">Cerrar sesión</ui-button>
            }
            @case ('disabled') {
              <p role="alert" class="text-sm font-semibold text-slate-800 mb-4">Tu cuenta no está habilitada; contacta a Gerencia</p>
              <ui-button variant="outline" [fullWidth]="true" (buttonClick)="session.logout()">Cerrar sesión</ui-button>
            }
            @case ('error') {
              <p role="alert" class="text-sm text-slate-800 mb-4">No pudimos verificar tu cuenta. Intenta de nuevo en unos minutos.</p>
              <ui-button variant="primary" [fullWidth]="true" (buttonClick)="session.login()">Reintentar</ui-button>
            }
            @default {
              <ui-button variant="primary" size="lg" [fullWidth]="true" (buttonClick)="session.login()">Iniciar sesión</ui-button>
              <p class="text-xs text-slate-500 mt-3">Te llevaremos a la página segura de inicio de sesión. Allí también puedes recuperar tu contraseña.</p>
            }
          }
        </ui-card>
        <a routerLink="/public-trace" class="text-xs text-slate-500 hover:text-primary-600">Consultar trazabilidad sin ingresar</a>
      </div>
    </div>
  `,
})
export class LoginPage implements OnInit {
  readonly session = inject(AuthSession);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    if (this.session.status() === 'authenticated') {
      void this.router.navigateByUrl('/dashboard');
      return;
    }
    // Una visita a una ruta privada vuelve a Cognito conservando solo un destino interno validado.
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (this.session.status() === 'anonymous' && returnUrl) this.session.login(returnUrl);
  }
}
