import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonButton, IonContent } from '@ionic/angular/standalone';
import { MobileAuthService } from '../../auth.service';

/** FR-01: esta página no recibe contraseñas; abre la página administrada de Cognito. */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [IonContent, IonButton],
  template: `
    <ion-content class="ion-padding">
      <div class="max-w-md mx-auto py-10 space-y-6 text-center">
        <h1 class="text-2xl font-black text-slate-900">IstpetDev Campo</h1>
        @switch (auth.status()) {
          @case ('wrong-platform') {
            <p role="alert" class="text-sm font-semibold text-slate-800">Tu cuenta opera en la plataforma web</p>
            <ion-button expand="block" fill="outline" (click)="auth.logout()">Cerrar sesión</ion-button>
          }
          @case ('disabled') {
            <p role="alert" class="text-sm font-semibold text-slate-800">Tu cuenta no está habilitada; contacta a Gerencia</p>
            <ion-button expand="block" fill="outline" (click)="auth.logout()">Cerrar sesión</ion-button>
          }
          @case ('error') {
            <p role="alert" class="text-sm text-slate-800">No pudimos verificar tu cuenta. Revisa la señal e intenta de nuevo.</p>
            <ion-button expand="block" (click)="auth.login()">Reintentar</ion-button>
          }
          @default {
            <ion-button expand="block" (click)="auth.login()">Iniciar sesión</ion-button>
            <p class="text-xs text-slate-500">Te llevaremos a la página segura de inicio de sesión. Allí también puedes recuperar tu contraseña.</p>
          }
        }
      </div>
    </ion-content>
  `
})
export class LoginPage implements OnInit {
  readonly auth = inject(MobileAuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    if (this.auth.status() === 'authenticated') {
      void this.router.navigateByUrl('/home');
      return;
    }
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (this.auth.status() === 'anonymous' && returnUrl) this.auth.login(returnUrl);
  }
}
