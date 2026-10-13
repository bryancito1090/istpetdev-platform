import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
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
  IonButtons
} from '@ionic/angular/standalone';
import { MobileAuthService } from '../auth/auth.service';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';

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
    IonButtons
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar color="primary">
        <ion-title>IstpetDev Móvil</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="openSyncQueue()">
            Cola
            @if (pendingCount() > 0) {
              <ion-badge color="danger" class="ml-1">{{ pendingCount() }}</ion-badge>
            }
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true" class="ion-padding">
      <!-- Insignia fija: Datos Simulados (F7 / Spec 004) -->
      <div class="mb-3 flex items-center justify-between p-2.5 bg-blue-50 border border-blue-200 rounded-lg text-xs text-blue-900">
        <div class="flex items-center gap-1.5">
          <span class="w-2 h-2 rounded-full bg-blue-600 animate-ping"></span>
          <span class="font-bold">Datos simulados (Fase Frontend-First)</span>
        </div>
        <span class="text-[10px] bg-blue-200 text-blue-800 px-2 py-0.5 rounded font-mono font-bold">
          useMockApi
        </span>
      </div>

      <!-- Tarjeta de Sesión Activa del Conductor -->
      <ion-card class="m-0 mb-4">
        <ion-card-header class="pb-2">
          <div class="flex items-center justify-between">
            <ion-card-subtitle class="text-xs">Conductor de Custodia</ion-card-subtitle>
            <ion-badge color="success">Vehículo: {{ authService.currentVehicleId() || 'VH-EC-101' }}</ion-badge>
          </div>
          <ion-card-title class="text-lg">
            {{ authService.currentUser()?.name || 'Carlos Chofer (Demo)' }}
          </ion-card-title>
        </ion-card-header>
        <ion-card-content class="pt-0">
          <div class="flex gap-2 pt-2">
            <ion-button expand="block" size="small" class="flex-1" (click)="openScan()">
              📷 Escanear QR
            </ion-button>
            <ion-button
              expand="block"
              size="small"
              fill="outline"
              class="flex-1"
              [disabled]="loadingRoute()"
              (click)="downloadRoute()">
              {{ loadingRoute() ? 'Cargando...' : '🔄 Descargar Ruta' }}
            </ion-button>
          </div>
        </ion-card-content>
      </ion-card>

      <!-- Mensaje si se descarga la ruta -->
      @if (downloadMessage()) {
        <div class="mb-4 p-3 bg-emerald-50 border border-emerald-200 rounded-lg text-xs text-emerald-800">
          {{ downloadMessage() }}
        </div>
      }

      <!-- Lista de Paradas Asignadas -->
      <div class="mb-2 flex items-center justify-between px-1">
        <span class="text-xs font-bold uppercase tracking-wider text-slate-600">
          Paradas de la Ruta Activa
        </span>
        <span class="text-[11px] text-slate-500">
          {{ totalDeliveries() }} entregas
        </span>
      </div>

      @if (outboxService.activeAssignment(); as assignment) {
        <div class="space-y-3">
          @for (route of assignment.routes; track route.routePlanId) {
            <div class="space-y-3">
              <div class="flex items-center justify-between text-xs text-slate-500 font-mono px-1">
                <span>Ruta: {{ route.routePlanId }} (Rev {{ route.revision }})</span>
                <span>Vehículo: {{ route.vehicleCode }}</span>
              </div>

              @for (del of route.deliveries; track del.deliveryId) {
                <ion-card class="m-0 border shadow-sm">
                  <ion-card-header class="pb-2">
                    <div class="flex items-center justify-between">
                      <ion-badge color="medium">Parada {{ del.sequence }}</ion-badge>
                      <span class="text-xs font-semibold text-slate-500">ID: {{ del.pointCode }}</span>
                    </div>
                    <ion-card-title class="text-base">{{ del.pointName }}</ion-card-title>
                    <ion-card-subtitle>{{ del.address }}</ion-card-subtitle>
                  </ion-card-header>

                  <ion-card-content class="pt-0 space-y-2">
                    <div class="p-2.5 bg-slate-50 rounded-lg border border-slate-200 flex items-center justify-between">
                      <div>
                        <span class="text-xs font-bold text-slate-800 block">Entrega {{ del.deliveryId }}</span>
                        <span class="text-[11px] text-slate-500">Bultos HU: {{ del.handlingUnits.length }} • Estado: {{ del.status }}</span>
                      </div>

                      <ion-button
                        size="small"
                        color="primary"
                        (click)="goToDelivery(del.deliveryId)">
                        Recibir →
                      </ion-button>
                    </div>
                  </ion-card-content>
                </ion-card>
              }
            </div>
          }
        </div>
      } @else {
        <ion-card class="m-0">
          <ion-card-content class="text-center py-8">
            <p class="text-sm text-slate-500">No hay ruta descargada en el dispositivo.</p>
            <ion-button class="mt-3" size="small" (click)="downloadRoute()">
              Descargar Ruta de Asignación
            </ion-button>
          </ion-card-content>
        </ion-card>
      }
    </ion-content>
  `
})
export class HomePage implements OnInit {
  readonly authService = inject(MobileAuthService);
  readonly outboxService = inject(FieldOutboxService);
  private readonly router = inject(Router);

  readonly loadingRoute = signal<boolean>(false);
  readonly downloadMessage = signal<string | null>(null);

  readonly pendingCount = computed(() => {
    return this.outboxService.outboxItems().filter((i) => i.status !== 'Complete').length;
  });

  readonly totalDeliveries = computed(() => {
    const a = this.outboxService.activeAssignment();
    if (!a || !a.routes) return 0;
    return a.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0);
  });

  async ngOnInit(): Promise<void> {
    // Si no hay ruta en caché, intentar descargarla
    if (!this.outboxService.activeAssignment()) {
      await this.downloadRoute();
    }
  }

  async downloadRoute(): Promise<void> {
    this.loadingRoute.set(true);
    this.downloadMessage.set(null);
    try {
      const assignment = await this.outboxService.downloadRouteAssignment();
      const total = assignment.routes ? assignment.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0) : 0;
      this.downloadMessage.set(
        `✓ Ruta cargada con éxito: ${total} entrega(s) asignada(s) para el vehículo.`
      );
    } catch {
      this.downloadMessage.set('Aviso: Utilizando asignación almacenada localmente.');
    } finally {
      this.loadingRoute.set(false);
    }
  }

  openScan(): void {
    this.router.navigate(['/scan']);
  }

  openSyncQueue(): void {
    this.router.navigate(['/sync']);
  }

  goToDelivery(deliveryId: string): void {
    this.router.navigate(['/receipt', deliveryId]);
  }
}
