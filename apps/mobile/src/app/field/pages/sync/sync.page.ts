import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent,
  IonToggle,
  AlertController
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
    IonContent,
    IonToggle
  ],
  template: `
    <!-- Barra Superior: Entorno de Prueba (Dark Cyan Bar) -->
    <div class="stripe-top-banner">
      <div class="stripe-banner-left">
        <span class="banner-title">Entorno de prueba</span>
        <button type="button" class="info-bubble-btn" (click)="openTestEnvInfo()" aria-label="Información de entorno">
          <svg class="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>

      <button type="button" class="stripe-verify-pill" (click)="openVerifyModal()">
        Verificar tu empresa
      </button>
    </div>

    <!-- Contenedor Principal con Bordes Redondeados Superiores -->
    <div class="stripe-header-card">
      <div class="stripe-nav-header">
        <button type="button" class="stripe-nav-btn" aria-label="Volver" (click)="goHome()">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.3">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
          </svg>
        </button>

        <h1 class="stripe-user-title">
          Cola de Sincronización
        </h1>

        <div class="stripe-header-actions">
          @if (pendingCount() > 0) {
            <span class="stripe-bell-badge static relative-none">{{ pendingCount() }}</span>
          }
        </div>
      </div>
    </div>

    <!-- Contenido Scrollable con Tarjetas Estilo Stripe -->
    <ion-content class="ion-padding stripe-main-content">
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
          <button
            type="button"
            class="py-2 px-3 bg-rose-600 hover:bg-rose-700 text-white rounded-lg text-xs font-bold transition shadow-xs"
            (click)="disableOfflineAndSync()">
            Desactivar «Sin señal» y sincronizar ahora
          </button>
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
            <button
              type="button"
              class="flex-1 stop-action-btn py-2"
              (click)="claimLegacy()">
              Asignar a mi cuenta
            </button>
            <button
              type="button"
              class="flex-1 py-2 px-3 text-xs font-bold text-rose-700 bg-rose-50 border border-rose-200 hover:bg-rose-100 rounded-lg transition"
              (click)="discardLegacy()">
              Descartar
            </button>
          </div>
        </div>
      }

      <!-- Tarjeta de Control y Estado de Red -->
      <div class="driver-stop-card mb-4">
        <div class="flex items-center justify-between mb-1">
          <span class="stop-tag">Red y Almacenamiento</span>
          <span class="status-badge" [class.is-done]="!outboxService.isSimulatingOffline()">
            {{ outboxService.isSimulatingOffline() ? 'Sin señal (prueba)' : 'Online' }}
          </span>
        </div>
        <h2 class="stop-title">Control de Conectividad</h2>
        <p class="stop-address">Pruebe el comportamiento desconectado de campo.</p>

        <div class="p-3 bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-between mb-3">
          <div>
            <span class="font-bold text-xs text-slate-800 block">Simular Sin Conexión (Offline)</span>
            <span class="text-[11px] text-slate-500">Mantiene las capturas en almacenamiento local</span>
          </div>
          <ion-toggle
            [checked]="outboxService.isSimulatingOffline()"
            (ionChange)="toggleOffline()">
          </ion-toggle>
        </div>

        <div class="flex items-center gap-2">
          <button
            type="button"
            class="stop-action-btn flex-1"
            [disabled]="outboxService.isSyncing() || isSyncingAll()"
            (click)="syncAll()">
            {{ (outboxService.isSyncing() || isSyncingAll()) ? 'Sincronizando cola...' : 'Sincronizar Todo Ahora' }}
          </button>
          @if (outboxService.sendableCount() > 0) {
            <span class="text-xs font-bold bg-amber-100 text-amber-900 border border-amber-300 px-2.5 py-2 rounded-xl shrink-0">
              {{ outboxService.sendableCount() }} pendientes
            </span>
          }
        </div>
      </div>

      <!-- Lista de Ítems en Cola -->
      <div class="mb-2.5 flex items-center justify-between px-1">
        <div class="flex items-center gap-2">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-700">
            Operaciones
          </span>
          @if (outboxService.outboxItems().length > 0) {
            <span class="text-[10px] bg-slate-100 text-slate-700 font-bold px-1.5 py-0.5 rounded-full">
              {{ outboxService.outboxItems().length }}
            </span>
          }
          @if (outboxService.sendableCount() > 0) {
            <span class="text-[10px] bg-blue-100 text-blue-800 font-bold px-1.5 py-0.5 rounded-full">
              {{ outboxService.sendableCount() }} para enviar
            </span>
          }
          @if (outboxService.reviewCount() > 0) {
            <span class="text-[10px] bg-amber-100 text-amber-900 font-bold px-1.5 py-0.5 rounded-full border border-amber-300">
              {{ outboxService.reviewCount() }} revisión
            </span>
          }
        </div>
        <span class="text-[11px] text-slate-500 font-mono">UUID v7</span>
      </div>

      @if (outboxService.outboxItems().length === 0) {
        <div class="driver-stop-card text-center py-8">
          <p class="text-sm text-slate-500">No hay operaciones en la cola para tu cuenta.</p>
          <p class="text-xs text-slate-400 mt-1">Realice una entrega o recepción desde Inicio para generar operaciones.</p>
        </div>
      }

      <div class="space-y-3">
        @for (item of outboxService.outboxItems(); track item.operationId) {
          <div class="driver-stop-card" [class.border-rose-300]="item.status === 'Failed' || (item.status === 'SavedOnDevice' && item.errorCode)">
            <div class="flex items-center justify-between mb-2">
              <span class="text-[11px] font-mono text-slate-500">Op: {{ item.operationId.slice(0, 13) }}...</span>
              <span [class]="getBadgeClass(item.status)">
                {{ getStatusLabel(item.status) }}
              </span>
            </div>

            <h2 class="stop-title">{{ item.recipientName }}</h2>
            <p class="stop-address mb-2">Entrega: {{ item.deliveryId }}</p>

            <div class="space-y-2 text-xs">
              <div class="p-2.5 bg-slate-50 rounded-xl border border-slate-100 text-slate-600 space-y-1.5">
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
                <div class="p-3 bg-rose-50 border border-rose-300 rounded-xl text-rose-950 text-xs space-y-1">
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
                <div class="p-3 bg-rose-50 border border-rose-300 rounded-xl text-rose-950 text-xs space-y-1.5">
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
                <div class="p-3 bg-amber-50 border border-amber-300 rounded-xl text-amber-950 text-xs space-y-1.5">
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
                <p role="alert" class="p-3 rounded-xl border border-amber-300 bg-amber-50 text-amber-900 text-xs">
                  {{ item.errorMessage }} Cierra sesión y vuelve a entrar con esta cuenta; después pulsa Reintentar sincronización.
                </p>
              }

              <!-- Botones de Acción -->
              <div class="flex flex-col sm:flex-row gap-2 pt-1">
                @if (item.status === 'Conflict') {
                  <button
                    type="button"
                    class="flex-1 py-2 px-3 text-xs font-bold text-amber-900 bg-amber-100 hover:bg-amber-200 rounded-lg transition"
                    (click)="resolveConflict(item)">
                    Resolver con Nueva Versión y Reintentar
                  </button>
                }

                @if (item.status === 'RequiresReview' && !item.receiverContext && !item.commandKind) {
                  <button
                    type="button"
                    class="flex-1 py-2 px-3 text-xs font-bold text-amber-900 bg-amber-100 hover:bg-amber-200 rounded-lg transition"
                    (click)="resolveRequiresReview(item)">
                    Revisar y Adaptar Versión
                  </button>
                }

                @if (item.status !== 'Complete') {
                  <button
                    type="button"
                    class="flex-1 py-2 px-3 text-xs font-bold text-[#0A2540] bg-white border border-slate-200 hover:bg-slate-50 rounded-lg transition"
                    [disabled]="outboxService.isSimulatingOffline()"
                    (click)="syncSingle(item.operationId)">
                    Reintentar Sincronización
                  </button>
                }
              </div>
            </div>
          </div>
        }
      </div>
    </ion-content>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      background-color: #04303B;
    }

    .stripe-top-banner {
      background: #04303B;
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 16px 14px;
      min-height: 48px;
    }

    .stripe-banner-left {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .banner-title {
      font-size: 15px;
      font-weight: 700;
      letter-spacing: -0.01em;
      color: #ffffff;
    }

    .info-bubble-btn {
      background: transparent;
      border: none;
      color: rgba(255, 255, 255, 0.85);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2px;
    }

    .stripe-verify-pill {
      background: #00A7C4;
      color: #ffffff;
      border: none;
      border-radius: 9999px;
      padding: 6px 16px;
      font-size: 12.5px;
      font-weight: 700;
      letter-spacing: -0.01em;
      cursor: pointer;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
      transition: background-color 0.2s, transform 0.1s;
    }

    .stripe-verify-pill:hover {
      background: #008CA5;
    }

    .stripe-verify-pill:active {
      transform: scale(0.97);
    }

    .stripe-header-card {
      background: #ffffff;
      border-top-left-radius: 20px;
      border-top-right-radius: 20px;
      padding-top: 14px;
      box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.05);
      z-index: 20;
    }

    .stripe-nav-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 14px 10px;
    }

    .stripe-user-title {
      margin: 0;
      font-size: 17px;
      font-weight: 750;
      color: #0A2540;
      letter-spacing: -0.02em;
      flex: 1;
      margin-left: 8px;
    }

    .stripe-nav-btn {
      background: transparent;
      border: none;
      color: #0A2540;
      cursor: pointer;
      padding: 6px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: background 0.15s;
    }

    .stripe-nav-btn:hover {
      background: #f1f5f9;
    }

    .stripe-header-actions {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .stripe-bell-badge {
      background: #EA601F;
      color: white;
      font-size: 11px;
      font-weight: 750;
      border-radius: 9999px;
      min-width: 20px;
      height: 20px;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0 6px;
    }

    .stripe-main-content {
      --background: #F8FAFC;
      flex: 1;
    }

    .driver-stop-card {
      background: #ffffff;
      border: 1px solid #E3E8EF;
      border-radius: 14px;
      padding: 16px;
      box-shadow: 0 1px 3px rgba(10, 37, 64, 0.05);
      transition: all 0.15s ease-in-out;
    }

    .stop-tag {
      font-size: 11px;
      font-weight: 750;
      color: #64748B;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .status-badge {
      font-size: 11px;
      font-weight: 700;
      padding: 3px 8px;
      border-radius: 9999px;
      background: #F1F5F9;
      color: #475569;
    }

    .status-badge.is-done {
      background: #DCFCE7;
      color: #166534;
    }

    .stop-title {
      font-size: 16px;
      font-weight: 750;
      color: #0A2540;
      margin: 0 0 4px;
      line-height: 1.25;
    }

    .stop-address {
      font-size: 12px;
      color: #64748B;
      margin: 0 0 12px;
      line-height: 1.35;
    }

    .stop-action-btn {
      width: 100%;
      padding: 11px 16px;
      background: #00A7C4;
      color: #ffffff;
      border: none;
      border-radius: 10px;
      font-size: 13px;
      font-weight: 750;
      cursor: pointer;
      transition: background-color 0.15s;
    }

    .stop-action-btn:hover {
      background: #008CA5;
    }

    .stop-action-btn:disabled {
      background: #E2E8F0;
      color: #94A3B8;
      cursor: not-allowed;
    }
  `]
})
export class SyncPage {
  private readonly router = inject(Router);
  readonly outboxService = inject(FieldOutboxService);
  private readonly alerts = inject(AlertController, { optional: true });

  readonly syncFeedback = signal<SyncFeedback | null>(null);
  readonly isSyncingAll = signal<boolean>(false);

  readonly pendingCount = computed(() => {
    return typeof this.outboxService.pendingCount === 'function' ? this.outboxService.pendingCount() : 0;
  });

  goHome(): void {
    this.router.navigate(['/home']);
  }

  async openTestEnvInfo(): Promise<void> {
    if (!this.alerts) return;
    const alert = await this.alerts.create({
      header: 'Entorno de prueba activo',
      subHeader: 'Expo Clean Ecuador 2026',
      message: 'Estás operando en el modo seguro de validación de campo. Las operaciones offline se concilian mediante firma y código QR trazable.',
      buttons: ['Entendido']
    });
    await alert.present();
  }

  async openVerifyModal(): Promise<void> {
    if (!this.alerts) return;
    const alert = await this.alerts.create({
      header: 'Verificación de Empresa',
      subHeader: 'Certificación de Custodia Digital',
      message: 'Empresa: IstpetDev Operaciones Nacionales\nRUC: 1793148291001\nEstado: Validación en proceso (45%)\nProtocolo de custodia digital activo con claves M2M.',
      buttons: ['Aceptar']
    });
    await alert.present();
  }

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
