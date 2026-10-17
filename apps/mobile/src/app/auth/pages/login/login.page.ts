import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonButton, IonContent } from '@ionic/angular/standalone';
import { MobileAuthService } from '../../auth.service';
import { FieldPageHeaderComponent } from '../../../shared/field-page-header/field-page-header.component';

/** FR-01: esta página no recibe contraseñas; abre la página administrada de Cognito. */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [IonContent, IonButton, FieldPageHeaderComponent],
  template: `
    <app-field-page-header [brandOnly]="true" [showHelp]="false" />
    <ion-content class="ion-padding login-screen">
      <div class="login-layout">
        <section class="login-hero">
          <span class="eyebrow">LOGÍSTICA QUE CONECTA</span>
          <h1>Grandes equipos.<br><span>Todo en tus manos.</span></h1>
          <p>De bodega a destino, cada paso cuenta. Gestiona tu jornada desde un solo lugar.</p>
        </section>
        <button type="button" class="login-pass" (click)="login()"
          [disabled]="auth.status() === 'wrong-platform' || auth.status() === 'disabled'"
          aria-label="Ingresar y comenzar tu jornada">
          <span class="pass-top"><span>ISTPETDEV / CAMPO</span><span class="pass-chip">ACCESO SEGURO</span></span>
          <span class="pass-center">
            <span class="pass-symbol" aria-hidden="true">↗</span>
            <strong>Tu operación, conectada.</strong>
            <span class="pass-description">Comienza tu jornada</span>
            <span class="pass-action">{{ auth.status() === 'error' ? 'Reintentar' : 'Ingresar' }} <span aria-hidden="true">→</span></span>
          </span>
          <span class="pass-bottom"><span>BODEGA · RUTA · RECEPCIÓN</span><span class="pass-bars" aria-hidden="true"></span></span>
        </button>
        <section class="login-feedback" aria-label="Estado de acceso">
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
          }
          @default {
            <p class="text-xs text-slate-500">Te llevaremos a la página segura de inicio de sesión. Allí también puedes recuperar tu contraseña.</p>
          }
        }
        </section>
        <p class="login-footnote">Diseñado para acompañarte en campo.</p>
      </div>
    </ion-content>
  `,
  styles: [`:host { display: flex; flex-direction: column; height: 100%; } ion-content { flex: 1; }`]
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
  }

  login(): void {
    this.auth.login(this.route.snapshot.queryParamMap.get('returnUrl'));
  }
}
