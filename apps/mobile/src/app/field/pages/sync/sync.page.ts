import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonBackButton,
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
  IonToggle
} from '@ionic/angular/standalone';
import { FieldOutboxService, OutboxItem } from '../../outbox/field-outbox.service';

@Component({
  selector: 'app-sync',
  standalone: true,
  imports: [
    CommonModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonBackButton,
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
    IonToggle
  ],
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-back-button defaultHref="/home"></ion-back-button>
        </ion-buttons>
        <ion-title>Cola de Sincronización</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <!-- Tarjeta de Control y Estado de Red -->
      <ion-card class="mb-4">
        <ion-card-header>
          <div class="flex items-center justify-between">
            <ion-card-title class="text-base">Control de Conectividad</ion-card-title>
            <ion-badge [color]="outboxService.isSimulatingOffline() ? 'danger' : 'success'">
              {{ outboxService.isSimulatingOffline() ? 'Sin señal (prueba)' : 'Online' }}
            </ion-badge>
          </div>
          <ion-card-subtitle>Pruebe el comportamiento desconectado de campo</ion-card-subtitle>
        </ion-card-header>
        <ion-card-content class="space-y-3">
          <ion-item lines="none" class="bg-slate-50 rounded-lg">
            <ion-label>
              <h3>Simular Sin Conexión (Offline)</h3>
              <p>Mantiene las capturas en almacenamiento IndexedDB local</p>
            </ion-label>
            <ion-toggle
              [checked]="outboxService.isSimulatingOffline()"
              (ionChange)="toggleOffline()">
            </ion-toggle>
          </ion-item>

          <div class="flex gap-2">
            <ion-button
              expand="block"
              class="flex-1"
              [disabled]="outboxService.isSyncing() || outboxService.isSimulatingOffline()"
              (click)="syncAll()">
              {{ outboxService.isSyncing() ? 'Sincronizando...' : 'Sincronizar Todo Ahora' }}
            </ion-button>
          </div>
        </ion-card-content>
      </ion-card>

      <!-- Lista de Ítems en Cola -->
      <div class="mb-2 flex items-center justify-between px-1">
        <span class="text-xs font-bold uppercase tracking-wider text-slate-600">
          Operaciones Registradas ({{ outboxService.outboxItems().length }})
        </span>
        <span class="text-[11px] text-slate-500">Clave atómica: UUID v7</span>
      </div>

      @if (outboxService.outboxItems().length === 0) {
        <ion-card>
          <ion-card-content class="text-center py-8">
            <p class="text-sm text-slate-500">No hay operaciones pendientes en la cola.</p>
            <p class="text-xs text-slate-400 mt-1">Realice una entrega desde Inicio o Escanear QR.</p>
          </ion-card-content>
        </ion-card>
      }

      <div class="space-y-3">
        @for (item of outboxService.outboxItems(); track item.operationId) {
          <ion-card class="m-0 border shadow-sm">
            <ion-card-header class="pb-2">
              <div class="flex items-center justify-between mb-1">
                <span class="text-[11px] font-mono text-slate-500">Op: {{ item.operationId.slice(0, 13) }}...</span>
                <span [class]="getBadgeClass(item.status)">
                  {{ getStatusLabel(item.status) }}
                </span>
              </div>
              <ion-card-title class="text-sm font-bold">{{ item.recipientName }}</ion-card-title>
              <ion-card-subtitle class="text-xs">Entrega: {{ item.deliveryId }}</ion-card-subtitle>
            </ion-card-header>

            <ion-card-content class="pt-0 space-y-2 text-xs">
              <div class="p-2 bg-slate-50 rounded text-slate-600 space-y-1">
                <div class="flex justify-between">
                  <span>Capturado en dispositivo:</span>
                  <span class="font-mono">{{ item.capturedAtUtc | date:'shortTime' }}</span>
                </div>
                @if (item.photoSha256) {
                  <div class="flex justify-between text-[11px]">
                    <span>Foto SHA-256:</span>
                    <span class="font-mono text-slate-500">{{ item.photoSha256.slice(0, 10) }}...</span>
                  </div>
                }
                @if (item.signatureSha256) {
                  <div class="flex justify-between text-[11px]">
                    <span>Firma SHA-256:</span>
                    <span class="font-mono text-slate-500">{{ item.signatureSha256.slice(0, 10) }}...</span>
                  </div>
                }
                @if (item.receiptResponse) {
                  <div class="flex justify-between text-emerald-700 font-semibold pt-1 border-t border-slate-200">
                    <span>Recibo Servidor:</span>
                    <span class="font-mono">{{ item.receiptResponse.receiptId }}</span>
                  </div>
                }
                @if (item.custodyResponse) {
                  <div class="flex justify-between text-emerald-700 font-semibold pt-1 border-t border-slate-200">
                    <span>Estado confirmado:</span>
                    <span class="font-mono">{{ item.custodyResponse.status }}</span>
                  </div>
                }
              </div>

              <!-- Mensaje de Conflicto 409 (FR-12 / F5) -->
              @if (item.status === 'Conflict' && item.errorMessage) {
                <div class="p-3 bg-rose-50 border border-rose-300 rounded-lg text-rose-900 text-xs space-y-1.5">
                  <div class="flex items-center justify-between">
                    <span class="font-bold">Conflicto de Servidor ({{ item.errorCode }}):</span>
                    <span class="text-[10px] bg-rose-200 text-rose-900 px-1.5 py-0.5 rounded font-mono font-bold">Datos Protegidos</span>
                  </div>
                  <p class="text-[11px] leading-relaxed">{{ item.errorMessage }}</p>
                  <p class="text-[10px] text-rose-700 italic">
                    La firma y foto capturadas en campo no se han perdido. Puede resolver adaptando a la versión de ruta activa.
                  </p>
                </div>
              }

              <!-- Mensaje de Requiere Revisión (Issue #114 / ESC-23 / ESC-25) -->
              @if (item.status === 'RequiresReview' && item.errorMessage) {
                <div class="p-3 bg-amber-50 border border-amber-300 rounded-lg text-amber-950 text-xs space-y-1.5">
                  <div class="flex items-center justify-between">
                    <span class="font-bold">Aislado en Outbox ({{ item.errorCode || 'Error de Negocio' }}):</span>
                    <span class="text-[10px] bg-amber-200 text-amber-900 px-1.5 py-0.5 rounded font-mono font-bold">Requiere Revisión</span>
                  </div>
                  @if (item.problemType) {
                    <div class="text-[10px] font-mono text-slate-500 break-all">
                      Tipo: {{ item.problemType }}
                    </div>
                  }
                  <p class="text-[11px] leading-relaxed">{{ item.errorMessage }}</p>
                  <p class="text-[10px] text-amber-800 italic">
                    Elemento aislado sin reintento automático para no bloquear la cola (ESC-23). Datos locales preservados (ESC-25).
                  </p>
                </div>
              }

              @if (item.status === 'SessionRequired') {
                <p role="alert" class="p-3 rounded border border-amber-300 text-amber-900 text-xs">
                  {{ item.errorMessage }} Cierra sesión y vuelve a entrar con esta cuenta; después pulsa Reintentar sincronización.
                </p>
              }

              <!-- Botones de Acción -->
              <div class="flex flex-col sm:flex-row gap-2 pt-1">
                @if (item.status === 'Conflict') {
                  <ion-button
                    size="small"
                    color="warning"
                    class="flex-1"
                    (click)="resolveConflict(item)">
                    Resolver con Nueva Versión y Reintentar
                  </ion-button>
                }

                @if (item.status === 'RequiresReview' && !item.receiverContext && !item.commandKind) {
                  <ion-button
                    size="small"
                    color="warning"
                    class="flex-1"
                    (click)="resolveRequiresReview(item)">
                    Revisar y Adaptar Versión
                  </ion-button>
                }

                @if (item.status !== 'Complete') {
                  <ion-button
                    size="small"
                    fill="outline"
                    class="flex-1"
                    [disabled]="outboxService.isSimulatingOffline()"
                    (click)="syncSingle(item.operationId)">
                    Reintentar Sincronización
                  </ion-button>
                }
              </div>
            </ion-card-content>
          </ion-card>
        }
      </div>
    </ion-content>
  `
})
export class SyncPage {
  readonly outboxService = inject(FieldOutboxService);

  toggleOffline(): void {
    this.outboxService.toggleOfflineSimulation();
  }

  syncAll(): void {
    this.outboxService.syncAllPending();
  }

  syncSingle(operationId: string): void {
    this.outboxService.syncItem(operationId).catch(() => {});
  }

  resolveConflict(item: OutboxItem): void {
    const nextVersion = item.expectedDeliveryVersion < 3 ? item.expectedDeliveryVersion + 1 : 1;
    this.outboxService.resolveConflictWithNewVersion(item.operationId, nextVersion).catch(() => {});
  }

  resolveRequiresReview(item: OutboxItem): void {
    const nextVersion = item.expectedDeliveryVersion < 3 ? item.expectedDeliveryVersion + 1 : 1;
    this.outboxService.resolveRequiresReview(item.operationId, nextVersion).catch(() => {});
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'SavedOnDevice':
        return 'Guardado en Teléfono';
      case 'Sending':
        return 'Enviando...';
      case 'AcceptedByServer':
        return 'Aceptado por Servidor';
      case 'EvidencePending':
        return 'Subiendo Evidencias...';
      case 'Complete':
        return 'Completo';
      case 'Conflict':
        return 'Conflicto 409';
      case 'SessionRequired':
        return 'Sesión requerida';
      case 'RequiresReview':
        return 'Requiere Revisión';
      default:
        return status;
    }
  }

  getBadgeClass(status: string): string {
    const base = 'px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ';
    switch (status) {
      case 'SavedOnDevice':
        return base + 'bg-blue-100 text-blue-800';
      case 'Sending':
        return base + 'bg-amber-100 text-amber-800 animate-pulse';
      case 'EvidencePending':
        return base + 'bg-purple-100 text-purple-800';
      case 'Complete':
        return base + 'bg-emerald-100 text-emerald-800';
      case 'Conflict':
        return base + 'bg-rose-100 text-rose-800';
      case 'SessionRequired':
      case 'RequiresReview':
        return base + 'bg-amber-100 text-amber-900 border border-amber-300';
      default:
        return base + 'bg-slate-100 text-slate-800';
    }
  }
}
