import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { IonContent } from '@ionic/angular/standalone';
import { MobileAuthService } from '../../auth.service';

/** Vuelta de Cognito: el inicializador ya canjeó el código; aquí solo se decide el destino. */
@Component({
  selector: 'app-auth-callback',
  standalone: true,
  imports: [IonContent],
  template: `<ion-content class="ion-padding"><p class="text-center text-sm text-slate-500 pt-10">Verificando tu cuenta…</p></ion-content>`
})
export class CallbackPage implements OnInit {
  private readonly auth = inject(MobileAuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const target = this.auth.status() === 'authenticated' ? this.auth.takeReturnUrl() : '/login';
    void this.router.navigateByUrl(target, { replaceUrl: true });
  }
}
