import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardSubtitle,
  IonCardContent,
  IonButton,
  IonBadge,
  IonIcon,
  IonItem,
  IonLabel,
  IonList
} from '@ionic/angular/standalone';
import { PingResponseDto } from '@istpetdev/shared-core';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardSubtitle,
    IonCardContent,
    IonButton,
    IonBadge,
    IonItem,
    IonLabel,
    IonList
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar color="primary">
        <ion-title>IstpetDev Móvil</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true" class="ion-padding">
      <ion-card>
        <ion-card-header>
          <div class="flex items-center justify-between">
            <ion-card-title>Operaciones Móviles PWA</ion-card-title>
            <ion-badge [color]="apiStatus() === 'online' ? 'success' : apiStatus() === 'offline' ? 'danger' : 'warning'">
              {{ apiStatus() === 'online' ? 'Online' : apiStatus() === 'offline' ? 'Offline' : 'Verificando...' }}
            </ion-badge>
          </div>
          <ion-card-subtitle>Recepción de Entregas y Custodia</ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <p class="text-sm text-slate-600 mb-4">
            Cliente PWA optimizado para trabajo en campo con soporte offline y validación de custodia digital.
          </p>

          <ion-button expand="block" (click)="checkApi()" [disabled]="checking()">
            {{ checking() ? 'Comprobando conexión...' : 'Verificar API (localhost:5000)' }}
          </ion-button>
        </ion-card-content>
      </ion-card>

      @if (pingData(); as data) {
        <ion-card>
          <ion-card-header>
            <ion-card-title class="text-base">Datos de Conexión API</ion-card-title>
          </ion-card-header>
          <ion-card-content>
            <ion-list lines="inset">
              <ion-item>
                <ion-label>
                  <h3>Servicio</h3>
                  <p>{{ data.service }}</p>
                </ion-label>
              </ion-item>
              <ion-item>
                <ion-label>
                  <h3>Versión</h3>
                  <p>v{{ data.version }}</p>
                </ion-label>
              </ion-item>
              <ion-item>
                <ion-label>
                  <h3>Entorno</h3>
                  <p>{{ data.environment }}</p>
                </ion-label>
              </ion-item>
              <ion-item>
                <ion-label>
                  <h3>Marca Temporal</h3>
                  <p>{{ data.timestamp | date:'medium' }}</p>
                </ion-label>
              </ion-item>
            </ion-list>
          </ion-card-content>
        </ion-card>
      }

      <ion-card>
        <ion-card-header>
          <ion-card-title class="text-base">Flujo Offline Preparado</ion-card-title>
        </ion-card-header>
        <ion-card-content>
          <ul class="text-xs space-y-2 text-slate-600">
            <li>✓ Cola de recepción protegida por idempotency key (UUID v7/v4).</li>
            <li>✓ Captura fotográfica y de firmas local lista para sincronización.</li>
            <li>✓ Manejo de autenticación OIDC Cognito conforme a ADR-10.</li>
          </ul>
        </ion-card-content>
      </ion-card>
    </ion-content>
  `
})
export class HomePage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly apiStatus = signal<'checking' | 'online' | 'offline'>('checking');
  readonly pingData = signal<PingResponseDto | null>(null);
  readonly checking = signal(false);

  ngOnInit() {
    this.checkApi();
  }

  checkApi() {
    this.checking.set(true);
    this.http.get<PingResponseDto>('http://localhost:5000/api/v1/ping').subscribe({
      next: (res) => {
        this.apiStatus.set('online');
        this.pingData.set(res);
        this.checking.set(false);
      },
      error: () => {
        this.apiStatus.set('offline');
        this.pingData.set(null);
        this.checking.set(false);
      }
    });
  }
}
