import { Component, inject, signal } from '@angular/core';
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

interface SyncFeedback {
  type: 'success' | 'warning' | 'error';
  title: string;
  message: string;
}

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
      <!-- Banner de Retroalimentación de Sincronización (#163) -->
      @if (syncFeedback(); as feedback) {
        <div
          class="mb-4 p-3.5 rounded-xl border flex items-start justify-between gap-3 shadow-xs animate-fadeIn"
          [ngClass]="{
            'bg-emerald-50 border-emerald-300 text-emerald-950': feedback.type === 'success',
            'bg-amber-50 border-amber-300 text-amber-950': feedback.type === 'warning',
            'bg-rose-50 border-rose-300 text-rose-950': feedback.type === 'error'
          }"
          role="alert">
          <div class="flex-1 space-y-1">
            <div class="flex items-center gap-2">
              <span class="font-bold text-xs uppercase tracking-wide">
                {{ feedback.type === 'success' ? '✓ ' : feedback.type === 'warning' ? '⚠ ' : '✕ ' }}{{ feedback.title }}
              </span>
            </div>
            <p class="text-xs leading-relaxed">{{ feedback.message }}</p>
          </div>
          <button
            type="button"
            (click)="clearFeedback()"
            class="text-xs font-bold text-slate-500 hover:text-slate-800 p-1"
            aria-label="Cerrar aviso">
            ✕
          </button>
        </div>
      }

      <!-- Aviso si modo Sin señal (prueba) sigue encendido (#163) -->
      @if (outboxService.isSimulatingOffline()) {
        <div class="mb-4 p-3 bg-rose-50 border border-rose-300 rounded-xl text-rose-950 text-xs space-y-2">
          <div class="flex items-center gap-1.5 font-bold">
            <span class="w-2 h-2 rounded-full bg-rose-600 animate-pulse"></span>
            <span>Modo «Sin señal (prueba)» activado</span>
          </div>
          <p class="leading-relaxed">
            Las operaciones se guardan en el teléfono y no se enviarán al servidor hasta que desactives esta simulación.
          </p>
          <ion-button size="small" color="danger" (click)="disableOfflineAndSync()">
            Desactivar «Sin señal» y sincronizar ahora
          </ion-button>
        </div>
      }

      <!-- Capturas Legadas sin dueño (#163) -->
      @if (outboxService.legacyCount() > 0) {
        <div class="mb-4 p-3.5 bg-indigo-50 border border-indigo-200 rounded-xl text-indigo-950 text-xs space-y-2">
          <div class="font-bold text-xs flex items-center justify-between">
            <span>📦 Capturas de versión previa / sin cuenta</span>
            <span class="bg-indigo-200 text-indigo-900 px-2 py-0.5 rounded-full font-bold">
              {{ outboxService.legacyCount() }} pendientes
            </span>
          </div>
          <p class="leading-relaxed text-[11px]">
            Existen operaciones en la memoria local guardadas antes de iniciar sesión o en una versión anterior.
            Puedes asignarlas a tu cuenta activa para sincronizarlas, o descartarlas si ya no son necesarias.
          </p>
          <div class="flex gap-2 pt-1">
            <ion-button size="small" color="primary" class="flex-1 font-bold" (click)="claimLegacy()">
              Asignar a mi cuenta
            </ion-button>
            <ion-button size="small" fill="outline" color="danger" class="flex-1 font-bold" (click)="discardLegacy()">
              Descartar
            </ion-button>
          </div>
        </div>
      }

      <!-- Tarjeta de Control y Estado de Red -->
      <ion-card class="m-0 mb-4 border border-slate-200 shadow-sm">
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

          <div class="flex flex-col sm:flex-row gap-2">
            <ion-button
              expand="block"
              class="flex-1 font-bold"
              [disabled]="outboxService.isSyncing() || isSyncingAll()"
              (click)="syncAll()">
              {{ (outboxService.isSyncing() || isSyncingAll()) ? 'Sincronizando cola...' : 'Sincronizar Todo Ahora' }}
            </ion-button>
          </div>
        </ion-card-content>
      </ion-card>

      <!-- Lista de Ítems en Cola -->
      <div class="mb-2 flex items-center justify-between px-1">
        <div class="flex items-center gap-2">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-700">
            Operaciones ({{ outboxService.outboxItems().length }})
          </span>
          @if (outboxService.sendableCount() > 0) {
            <span class="text-[10px] bg-blue-100 text-blue-800 font-bold px-1.5 py-0.5 rounded">
              {{ outboxService.sendableCount() }} para enviar
            </span>
          }
          @if (outboxService.reviewCount() > 0) {
            <span class="text-[10px] bg-amber-100 text-amber-900 font-bold px-1.5 py-0.5 rounded border border-amber-300">
              {{ outboxService.reviewCount() }} revisión
            </span>
          }
        </div>
        <span class="text-[11px] text-slate-500 font-mono">UUID v7</span>
      </div>

      @if (outboxService.outboxItems().length === 0) {
        <ion-card class="m-0 border shadow-sm">
          <ion-card-content class="text-center py-8">
            <p class="text-sm text-slate-500">No hay operaciones en la cola para tu cuenta.</p>
            <p class="text-xs text-slate-400 mt-1">Realice una entrega o recepción desde Inicio para generar operaciones.</p>
          </ion-card-content>
        </ion-card>
      }

      <div class="space-y-3">
        @for (item of outboxService.outboxItems(); track item.operationId) {
          <ion-card class="m-0 border shadow-sm" [class.border-rose-300]="item.status === 'Failed' || (item.status === 'SavedOnDevice' && item.errorCode)">
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
              <div class="p-2.5 bg-slate-50 rounded-lg text-slate-600 space-y-1.5">
                <div class="flex justify-between">
                  <span>Capturado en dispositivo:</span>
                  <span class="font-mono text-slate-800">{{ item.capturedAtUtc | date:'shortTime' }}</span>
                </div>

                <!-- Visibilidad de Foto Confirmada en Servidor (#163) -->
                @if (item.photoSha256) {
                  <div class="flex items-center justify-between text-[11px] pt-1 border-t border-slate-200">
                    <span class="flex items-center gap-1">
                      @if (item.photoEvidenceId) {
                        <span class="text-emerald-700 font-bold">✓ Foto confirmada</span>
                      } @else {
                        <span class="text-amber-800 font-medium">⏳ Foto pendiente de subida</span>
                      }
                    </span>
                    <span class="font-mono text-slate-500 text-[10px]" [title]="item.photoSha256">
                      SHA: {{ item.photoSha256.slice(0, 10) }}...
                    </span>
                  </div>
                }

                <!-- Visibilidad de Firma Confirmada en Servidor (#163) -->
                @if (item.signatureSha256) {
                  <div class="flex items-center justify-between text-[11px] pt-1 border-t border-slate-200">
                    <span class="flex items-center gap-1">
                      @if (item.signatureEvidenceId) {
                        <span class="text-emerald-700 font-bold">✓ Firma confirmada</span>
                      } @else {
                        <span class="text-amber-800 font-medium">⏳ Firma pendiente de subida</span>
                      }
                    </span>
                    <span class="font-mono text-slate-500 text-[10px]" [title]="item.signatureSha256">
                      SHA: {{ item.signatureSha256.slice(0, 10) }}...
                    </span>
                  </div>
                }

                @if (item.receiptResponse) {
                  <div class="flex justify-between text-emerald-800 font-semibold pt-1 border-t border-slate-200">
                    <span>Recibo Servidor:</span>
                    <span class="font-mono">{{ item.receiptResponse.receiptId }}</span>
                  </div>
                }
                @if (item.custodyResponse) {
                  <div class="flex justify-between text-emerald-800 font-semibold pt-1 border-t border-slate-200">
                    <span>Estado confirmado:</span>
                    <span class="font-mono">{{ item.custodyResponse.status }}</span>
                  </div>
                }
              </div>

              <!-- Mensaje visible de Error Transitorio o Fallido (#163) -->
              @if ((item.status === 'SavedOnDevice' || item.status === 'Failed') && item.errorMessage) {
                <div class="p-3 bg-rose-50 border border-rose-300 rounded-lg text-rose-950 text-xs space-y-1">
                  <div class="flex items-center justify-between">
                    <span class="font-bold">Error en último intento ({{ item.errorCode || 'Fallo de red' }}):</span>
                    <span class="text-[10px] bg-rose-200 text-rose-900 px-1.5 py-0.5 rounded font-mono font-bold">Reintento disponible</span>
                  </div>
                  <p class="text-[11px] leading-relaxed">{{ item.errorMessage }}</p>
                  @if (item.lastAttemptUtc) {
                    <p class="text-[10px] text-rose-700">Hora: {{ item.lastAttemptUtc | date:'shortTime' }}</p>
                  }
                </div>
              }

              <!-- Mensaje de Conflicto 409 (FR-12 / F5) -->
              @if (item.status === 'Conflict' && item.errorMessage) {
                <div class="p-3 bg-rose-50 border border-rose-300 rounded-lg text-rose-950 text-xs space-y-1.5">
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
  readonly syncFeedback = signal<SyncFeedback | null>(null);
  readonly isSyncingAll = signal<boolean>(false);

  clearFeedback(): void {
    this.syncFeedback.set(null);
  }

  toggleOffline(): void {
    const isNowOffline = this.outboxService.toggleOfflineSimulation();
    if (isNowOffline) {
      this.syncFeedback.set({
        type: 'warning',
        title: 'Modo sin señal activado',
        message: 'Las operaciones se acumularán localmente en el teléfono.'
      });
    } else {
      this.syncFeedback.set({
        type: 'success',
        title: 'Modo Online activado',
        message: 'Conectividad restaurada. Iniciando sincronización de pendientes...'
      });
    }
  }

  disableOfflineAndSync(): void {
    this.outboxService.isSimulatingOffline.set(false);
    void this.syncAll();
  }

  async syncAll(): Promise<void> {
    this.isSyncingAll.set(true);
    try {
      const result = await this.outboxService.syncAllPending();
      if (result.blockedBySimulation) {
        this.syncFeedback.set({
          type: 'warning',
          title: 'Modo «Sin señal (prueba)» activo',
          message: 'Desactiva el interruptor para sincronizar las operaciones con el servidor.'
        });
        return;
      }
      if (result.blockedByOffline) {
        this.syncFeedback.set({
          type: 'warning',
          title: 'Sin conexión a Internet',
          message: 'El dispositivo no detecta red activa. Conéctate a WiFi o datos móviles para sincronizar.'
        });
        return;
      }
      if (result.blockedBySessionHeld) {
        this.syncFeedback.set({
          type: 'warning',
          title: 'Sesión retenida offline',
          message: 'Iniciaste sesión sin señal. Renueva la sesión cuando tengas red para validar credenciales y sincronizar.'
        });
        return;
      }
      if (result.attempted === 0) {
        const review = this.outboxService.reviewCount();
        if (review > 0) {
          this.syncFeedback.set({
            type: 'warning',
            title: 'Operaciones en revisión',
            message: `Hay ${review} operación(es) aislada(s) por conflicto o error de negocio que requieren revisión manual.`
          });
        } else {
          this.syncFeedback.set({
            type: 'success',
            title: 'Cola al día',
            message: 'No hay operaciones pendientes por sincronizar.'
          });
        }
        return;
      }

      if (result.failed === 0) {
        this.syncFeedback.set({
          type: 'success',
          title: 'Sincronización exitosa',
          message: `✓ Se sincronizaron ${result.synced} de ${result.attempted} operaciones correctamente.`
        });
      } else {
        const errorList = result.errors
          .map((e) => (e.code ? `[${e.code}] ${e.message}` : e.message))
          .slice(0, 2)
          .join(' · ');
        this.syncFeedback.set({
          type: 'error',
          title: `Sincronización parcial: ${result.synced} enviadas, ${result.failed} con error`,
          message: errorList || 'Revisa los detalles en cada tarjeta para corregir y reintentar.'
        });
      }
    } catch (err: any) {
      this.syncFeedback.set({
        type: 'error',
        title: 'Error inesperado al sincronizar',
        message: err.message || 'Ocurrió un error inesperado al procesar la cola.'
      });
    } finally {
      this.isSyncingAll.set(false);
    }
  }

  async syncSingle(operationId: string): Promise<void> {
    if (this.outboxService.isSimulatingOffline()) {
      this.syncFeedback.set({
        type: 'warning',
        title: 'Modo «Sin señal (prueba)» activo',
        message: 'Desactiva el interruptor para sincronizar esta operación.'
      });
      return;
    }
    if (typeof navigator !== 'undefined' && !navigator.onLine) {
      this.syncFeedback.set({
        type: 'warning',
        title: 'Sin conexión a Internet',
        message: 'Conéctate a una red para sincronizar esta operación.'
      });
      return;
    }

    try {
      const item = await this.outboxService.retryItem(operationId);
      if (item.status === 'Complete') {
        this.syncFeedback.set({
          type: 'success',
          title: 'Operación confirmada',
          message: `✓ La operación ${operationId.slice(0, 8)}... fue confirmada exitosamente por el servidor.`
        });
      } else {
        this.syncFeedback.set({
          type: 'warning',
          title: `Estado: ${this.getStatusLabel(item.status)}`,
          message: item.errorMessage || 'La operación no pudo completarse en este intento.'
        });
      }
    } catch (err: any) {
      this.syncFeedback.set({
        type: 'error',
        title: 'Error al sincronizar operación',
        message: err?.message || err?.detail || 'Falla al contactar al servidor.'
      });
    }
  }

  async claimLegacy(): Promise<void> {
    try {
      const count = await this.outboxService.claimLegacyItems();
      this.syncFeedback.set({
        type: 'success',
        title: 'Capturas reclamadas',
        message: `Se asignaron ${count} operación(es) a tu cuenta activa. Ahora puedes sincronizarlas.`
      });
    } catch (err: any) {
      this.syncFeedback.set({
        type: 'error',
        title: 'Error al reclamar capturas',
        message: err.message
      });
    }
  }

  async discardLegacy(): Promise<void> {
    try {
      const count = await this.outboxService.discardLegacyItems();
      this.syncFeedback.set({
        type: 'warning',
        title: 'Capturas descartadas',
        message: `Se eliminaron ${count} operación(es) legadas del almacenamiento local.`
      });
    } catch (err: any) {
      this.syncFeedback.set({
        type: 'error',
        title: 'Error al descartar capturas',
        message: err.message
      });
    }
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
      case 'Failed':
        return 'Error de Envío';
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
      case 'Failed':
        return base + 'bg-rose-100 text-rose-900 border border-rose-300';
      default:
        return base + 'bg-slate-100 text-slate-800';
    }
  }
}
