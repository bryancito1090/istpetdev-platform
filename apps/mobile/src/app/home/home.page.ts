import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
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
  IonButtons,
  IonInput,
  AlertController
} from '@ionic/angular/standalone';
import { RECEIPT_REJECTION_REASONS, MobileAssignmentDelivery } from '@istpetdev/shared-core';
import { FIELD_ROLE_LABELS, MobileAuthService } from '../auth';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { FIELD_API, IFieldApi, ReceiverUnit } from '../field/field-api.interface';
import { parseUnitQr } from '../field/unit-qr';
import { MockFieldApiService } from '../field/mock/mock-field-api.service';
import { WarehouseComponent } from '../field/pages/warehouse/warehouse.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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
    IonButtons,
    IonInput,
    WarehouseComponent
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
          <ion-button (click)="logout()">
            Salir
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true" class="ion-padding bg-slate-50">
      <!-- Insignia fija: Datos Simulados (F7 / Spec 004) solo si usa mock -->
      @if (isMockApi()) {
        <div class="mb-3 flex items-center justify-between p-2.5 bg-blue-50 border border-blue-200 rounded-lg text-xs text-blue-900 shadow-sm">
          <div class="flex items-center gap-1.5">
            <span class="w-2 h-2 rounded-full bg-blue-600 animate-ping"></span>
            <span class="font-bold">Datos simulados (Fase Frontend-First)</span>
          </div>
          <span class="text-[10px] bg-blue-200 text-blue-800 px-2 py-0.5 rounded font-mono font-bold">
            useMockApi
          </span>
        </div>
      }

      <!-- 015: la app se abrió sin señal; la cola se envía sola al renovar la sesión -->
      @if (authService.offline()) {
        <div role="status" class="mb-3 p-2.5 bg-amber-50 border border-amber-300 rounded-lg text-xs text-amber-900 shadow-sm">
          <span class="font-bold">Sin conexión: tus capturas se guardan en el teléfono.</span>
          Se enviarán solas cuando vuelva la señal.
        </div>
      }

      <!-- Tarjeta de Perfil Activo -->
      <ion-card class="m-0 mb-4 border border-slate-200 shadow-sm bg-white">
        <ion-card-header class="pb-2">
          <div class="flex items-center justify-between">
            <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">
              {{ roleSubtitle() }}
            </span>
            <ion-badge [color]="roleBadgeColor()">
              {{ roleEntityBadge() }}
            </ion-badge>
          </div>
          <ion-card-title class="text-lg text-slate-900 font-bold">
            {{ authService.me()?.displayName ?? '' }}
          </ion-card-title>
        </ion-card-header>
      </ion-card>

      <!-- ======================================================== -->
      <!-- CASO 1: RECEPTOR HOSPITALARIO (Solo Escanear QR y Novedades) -->
      <!-- ======================================================== -->
      @if (authService.currentRole() === 'Receptor') {
        <ion-card class="m-0">
          <ion-card-header><ion-card-title>Recibir insumos</ion-card-title></ion-card-header>
          <ion-card-content class="space-y-4">
            <ion-button expand="block" (click)="openReceiverScan()">Escanear QR con cámara</ion-button>
            <label class="block">Enlace completo del QR
              <ion-input [(ngModel)]="qrInputText" placeholder="Pega el enlace del bulto"></ion-input>
            </label>
            <ion-button (click)="searchReceiverUnit()" [disabled]="receiverLoading()">Buscar bulto</ion-button>
            @if (receiverError()) { <p role="alert" class="text-rose-700">{{ receiverError() }}</p> }
            @if (receiverCached()) {
              <p class="text-amber-800">Última consulta guardada. La recepción se validará al recuperar señal.</p>
            }
            @if (receiverActiveBox(); as box) {
              <h2 class="font-bold">{{ box.code }} · {{ box.destinationName }}</h2>
              <p class="text-xs text-slate-600">
                Recepción por <strong>entrega completa</strong>: {{ box.boxes.length }} bulto(s)
                ({{ box.boxes.map(b => b.code).join(', ') }}). Confirma todas las líneas enviadas.
              </p>
              @for (line of receiverLines; track line.deliveryLineId) {
                <div class="space-y-3 rounded border p-3">
                  <h3 class="font-bold">{{ line.productName }}</h3>
                  <p>Lote {{ line.lotCode }} · Enviadas {{ line.quantity }} {{ line.unit }}</p>
                  <label class="block">Cantidad aceptada
                    <input class="w-full border rounded p-2" type="number" min="0" [max]="line.quantity" step="0.001"
                      [(ngModel)]="line.quantityAccepted" />
                  </label>
                  <p>Rechazadas: {{ line.quantity - line.quantityAccepted }} {{ line.unit }}</p>
                  @if (line.quantityAccepted < line.quantity) {
                    <label class="block">Motivo del rechazo *
                      <select class="w-full border rounded p-2" [(ngModel)]="line.rejectionReason">
                        <option value="">Selecciona un motivo</option>
                        @for (reason of receiverReasons; track reason.code) {
                          <option [value]="reason.code">{{ reason.label }}</option>
                        }
                      </select>
                    </label>
                  }
                  <label class="block">Stock en percha antes de recibir ({{ line.unit }})
                    <input class="w-full border rounded p-2" type="number" min="0" step="0.001" [(ngModel)]="line.remainingQuantity" />
                  </label>
                  <label class="block">Cantidad solicitada para reabastecimiento ({{ line.unit }})
                    <input class="w-full border rounded p-2" type="number" min="0" step="0.001" [(ngModel)]="line.nextRequestedQuantity" />
                  </label>
                </div>
              }
              <label class="block">Foto de la novedad (obligatoria si rechazas unidades)
                <input type="file" accept="image/jpeg,image/png,image/webp" capture="environment" (change)="onReceiverPhoto($event)" />
              </label>
              @if (receiverPhoto(); as photo) { <p>Foto adjunta: {{ photo.name }}</p> }
              <ion-button expand="block" color="success" (click)="confirmReceiverAction()"
                [disabled]="receiverLoading() || receiverSaved() || box.status !== 'Delivered'">
                Guardar recepción de la entrega completa
              </ion-button>
            }
            @if (receiverConfirmedMessage()) {
              <div class="mt-3 p-3 bg-emerald-50 border border-emerald-300 rounded-lg text-emerald-950 text-xs space-y-2" role="status">
                <div class="flex items-center gap-1.5 font-bold">
                  <span class="text-emerald-700 font-bold text-sm">✓</span>
                  <span>Recepción guardada</span>
                </div>
                <p class="leading-relaxed">{{ receiverConfirmedMessage() }}</p>
                <div class="flex flex-col sm:flex-row gap-2 pt-1">
                  <ion-button size="small" color="primary" class="flex-1 font-bold" (click)="resetReceiverScreen()">
                    Registrar otro bulto / Limpiar
                  </ion-button>
                  <ion-button size="small" fill="outline" color="primary" class="flex-1 font-bold" (click)="openSyncQueue()">
                    Ver cola de sincronización
                  </ion-button>
                </div>
              </div>
            }
          </ion-card-content>
        </ion-card>
      }

      <!-- ======================================================== -->
      <!-- CASO 2: OPERADOR DE BODEGA (Móvil / Real)                -->
      <!-- ======================================================== -->
      @else if (authService.currentRole() === 'Bodega') {
        <app-warehouse />
      }

      <!-- ======================================================== -->
      <!-- CASO 3: CONDUCTOR DE CUSTODIA (Móvil)                     -->
      <!-- ======================================================== -->
      @else {
        <!-- Banner de Aviso de Revisión de Ruta (PA-02 / FR-12 / F5) -->
        @if (outboxService.activeDriverNotice(); as notice) {
          <div class="mb-4 p-3.5 bg-amber-50 border-2 border-amber-400 text-amber-950 rounded-xl shadow-sm flex items-start gap-3">
            <svg class="w-6 h-6 text-amber-600 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <div class="flex-1">
              <div class="flex items-center justify-between mb-1">
                <span class="font-black text-xs uppercase tracking-wider text-amber-900">
                  Aviso de Revisión de Ruta (Rev {{ notice.revision }})
                </span>
                <span class="text-[10px] font-mono bg-amber-200/80 text-amber-900 px-1.5 py-0.5 rounded font-bold">
                  {{ notice.routePlanId }}
                </span>
              </div>
              <p class="text-xs font-medium text-amber-900 leading-relaxed">{{ notice.message }}</p>
              <div class="mt-2.5">
                <button
                  type="button"
                  (click)="outboxService.acknowledgeNotice(notice.revision)"
                  class="text-xs font-bold px-3 py-1.5 bg-amber-900 text-white rounded-lg active:bg-amber-950 transition shadow-sm">
                  Entendido / Reconocer Aviso
                </button>
              </div>
            </div>
          </div>
        }

        <!-- Acciones del Conductor -->
        <ion-card class="m-0 mb-4 border border-slate-200 shadow-sm">
          <ion-card-content class="p-3">
            <div class="flex gap-2">
              <ion-button expand="block" size="small" class="flex-1 font-bold" (click)="openScan()">
                Escanear QR
              </ion-button>
              <ion-button
                expand="block"
                size="small"
                fill="outline"
                class="flex-1 font-bold"
                [disabled]="loadingRoute()"
                (click)="downloadRoute()">
                {{ loadingRoute() ? 'Cargando...' : 'Descargar Ruta' }}
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
          <span class="text-[11px] text-slate-500 font-bold">
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
                @if (route.originWarehouseName) {
                  <div class="text-xs text-slate-600 px-1" data-origin-warehouse>
                    Sale de: <strong>{{ route.originWarehouseName }}</strong>
                  </div>
                }

                @for (del of route.deliveries; track del.deliveryId) {
                  <ion-card class="m-0 border shadow-sm" [class.border-amber-300]="del.kind === 'Pickup'">
                    <ion-card-header class="pb-2">
                      <div class="flex items-center justify-between">
                        <div class="flex items-center gap-1.5">
                          <ion-badge [color]="del.kind === 'Pickup' ? 'warning' : 'primary'">
                            #{{ del.sequence }} {{ del.kind === 'Pickup' ? '📦 Recogida' : '🏥 Entrega' }}
                          </ion-badge>
                          @if (del.kind === 'Pickup') {
                            <span class="text-[10px] bg-amber-100 text-amber-900 font-bold px-1.5 py-0.5 rounded">Bodega</span>
                          }
                        </div>
                        <span class="text-xs font-semibold text-slate-500">ID: {{ del.pointCode }}</span>
                      </div>
                      <ion-card-title class="text-base">{{ del.pointName }}</ion-card-title>
                      <ion-card-subtitle>{{ del.address }}</ion-card-subtitle>
                    </ion-card-header>

                    <ion-card-content class="pt-0 space-y-2">
                      <div class="p-2.5 rounded-lg border flex items-center justify-between"
                           [class.bg-amber-50]="del.kind === 'Pickup'"
                           [class.border-amber-200]="del.kind === 'Pickup'"
                           [class.bg-slate-50]="del.kind !== 'Pickup'"
                           [class.border-slate-200]="del.kind !== 'Pickup'">
                        <div>
                          <span class="text-xs font-bold block" [class.text-amber-950]="del.kind === 'Pickup'" [class.text-slate-800]="del.kind !== 'Pickup'">
                            {{ del.kind === 'Pickup' ? 'Carga / Retiro' : 'Entrega' }} {{ del.deliveryId }}
                          </span>
                          <span class="text-[11px] text-slate-500">
                            Bultos HU: {{ del.handlingUnits.length }} • {{ driverStageLabel(del) }}
                          </span>
                        </div>

                        <ion-button
                          size="small"
                          [color]="driverActionColor(del)"
                          [disabled]="!driverActionable(del)"
                          (click)="goToDelivery(del.deliveryId)">
                          {{ driverActionLabel(del) }}
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
      }
    </ion-content>
  `
})
export class HomePage implements OnInit {
  readonly authService = inject(MobileAuthService);
  readonly outboxService = inject(FieldOutboxService);
  private readonly fieldApi = inject<IFieldApi>(FIELD_API);
  readonly isMockApi = computed(() => this.fieldApi instanceof MockFieldApiService);
  private readonly router = inject(Router);
  private readonly alerts = inject(AlertController);

  readonly loadingRoute = signal<boolean>(false);
  readonly downloadMessage = signal<string | null>(null);


  // --- Estado para el Receptor Hospitalario ---
  qrInputText = '';
  readonly receiverActiveBox = signal<ReceiverUnit | null>(null);
  readonly receiverQr = signal<{ unitId: string; token: string } | null>(null);
  readonly receiverLoading = signal<boolean>(false);
  readonly receiverError = signal<string | null>(null);
  readonly receiverCached = signal(false);
  readonly receiverSaved = signal(false);
  readonly receiverPhoto = signal<File | null>(null);
  readonly receiverConfirmedMessage = signal<string | null>(null);
  readonly receiverReasons = RECEIPT_REJECTION_REASONS;
  receiverLines: (ReceiverUnit['lines'][number] & {
    quantityAccepted: number;
    rejectionReason: string;
    remainingQuantity: number | null;
    nextRequestedQuantity: number | null;
  })[] = [];

  readonly pendingCount = this.outboxService.pendingCount;

  readonly totalDeliveries = computed(() => {
    const a = this.outboxService.activeAssignment();
    if (!a || !a.routes) return 0;
    return a.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0);
  });

  readonly roleSubtitle = computed(() => {
    const role = this.authService.currentRole();
    return role ? FIELD_ROLE_LABELS[role] : '';
  });

  readonly roleEntityBadge = computed(() => this.roleSubtitle());

  readonly roleBadgeColor = computed(() => {
    const role = this.authService.currentRole();
    if (role === 'Receptor') return 'success';
    if (role === 'Bodega') return 'tertiary';
    return 'primary';
  });

  constructor() {
    let accountId = this.authService.me?.()?.accountId ?? null;
    effect(() => {
      const nextAccountId = this.authService.me?.()?.accountId ?? null;
      if (nextAccountId === accountId) return;
      accountId = nextAccountId;
      this.qrInputText = '';
      this.receiverQr.set(null);
      this.receiverActiveBox.set(null);
      this.receiverPhoto.set(null);
      this.receiverLines = [];
      this.receiverSaved.set(false);
      this.receiverCached.set(false);
      this.receiverError.set(null);
      this.receiverConfirmedMessage.set(null);
    });
  }

  async ngOnInit(): Promise<void> {
    // Si es conductor y no hay ruta en caché, intentar descargarla
    if (this.authService.currentRole() === 'Conductor' && !this.outboxService.activeAssignment()) {
      await this.downloadRoute();
    }

  }

  ionViewWillEnter(): void {
    const qr = history.state?.receiverQr;
    if (this.authService.currentRole() === 'Receptor' && qr?.unitId && qr?.token) {
      void this.loadReceiverUnit(qr.unitId, qr.token);
    }
  }

  openReceiverScan(): void {
    this.router.navigate(['/scan'], { queryParams: { mode: 'receiver' } });
  }

  async searchReceiverUnit(): Promise<void> {
    this.receiverError.set(null);
    this.receiverConfirmedMessage.set(null);
    const parsed = parseUnitQr(this.qrInputText);
    if (!parsed) {
      this.receiverError.set('QR no válido o vencido');
      this.receiverActiveBox.set(null);
      this.receiverQr.set(null);
      return;
    }
    await this.loadReceiverUnit(parsed.unitId, parsed.token);
  }

  async loadReceiverUnit(unitId: string, token: string): Promise<void> {
    this.receiverLoading.set(true);
    this.receiverError.set(null);
    this.receiverConfirmedMessage.set(null);
    this.receiverQr.set({ unitId, token });
    this.receiverActiveBox.set(null);
    this.receiverPhoto.set(null);
    this.receiverSaved.set(false);
    this.receiverCached.set(false);
    this.receiverLines = [];

    try {
      const { unit, cached } = await this.outboxService.loadReceiverUnit(unitId, token);
      this.receiverCached.set(cached);
      this.receiverActiveBox.set(unit);
      this.receiverLines = unit.lines.map(line => ({ ...line, quantityAccepted: line.quantity,
        rejectionReason: '', remainingQuantity: null, nextRequestedQuantity: null }));
      const stateMessage = this.receiverStateMessage(unit.status);
      if (stateMessage) {
        this.receiverError.set(stateMessage);
      }
    } catch (err: unknown) {
      this.receiverActiveBox.set(null);
      if (err instanceof HttpErrorResponse) {
        if (err.status === 404) {
          this.receiverError.set('QR no válido o vencido');
        } else if (err.status === 409) {
          this.receiverError.set('Este bulto ya fue recibido');
        } else if (err.status === 0 || (typeof navigator !== 'undefined' && !navigator.onLine)) {
          this.receiverError.set('Necesitas señal para confirmar');
        } else {
          this.receiverError.set(err.error?.detail || err.message || 'Error al cargar bulto');
        }
      } else {
        this.receiverError.set(err instanceof Error ? err.message : 'QR no válido o vencido');
      }
    } finally {
      this.receiverLoading.set(false);
    }
  }

  onReceiverPhoto(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.receiverPhoto.set(null);
    if (file && (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 10 * 1024 * 1024 || file.size === 0)) {
      this.receiverError.set('Selecciona una imagen JPEG, PNG o WebP de hasta 10 MB.');
      return;
    }
    this.receiverError.set(null);
    this.receiverPhoto.set(file);
  }

  async confirmReceiverAction(): Promise<void> {
    const box = this.receiverActiveBox();
    const qr = this.receiverQr();
    if (!box || !qr || box.status !== 'Delivered' || this.receiverLoading() || this.receiverSaved()) return;
    this.receiverError.set(null);
    if (!this.receiverLines.length || this.receiverLines.some(l =>
      !Number.isFinite(l.quantityAccepted) || l.quantityAccepted < 0 || l.quantityAccepted > l.quantity ||
      [l.remainingQuantity, l.nextRequestedQuantity].some(q => q !== null && (!Number.isFinite(q) || q < 0)))) {
      this.receiverError.set('Revisa las cantidades: no pueden ser negativas ni superar lo enviado.');
      return;
    }
    if (this.receiverLines.some(l => l.quantityAccepted < l.quantity && !this.receiverReasons.some(r => r.code === l.rejectionReason))) {
      this.receiverError.set('Selecciona un motivo de catálogo por cada línea rechazada.');
      return;
    }
    if (this.receiverLines.some(l => l.quantityAccepted < l.quantity) && !this.receiverPhoto()) {
      this.receiverError.set('Adjunta una foto cuando hay rechazo.');
      return;
    }
    this.receiverLoading.set(true);
    try {
      await this.outboxService.saveReceiptAtomic({
        deliveryId: box.deliveryId,
        expectedDeliveryVersion: box.deliveryVersion,
        recipientName: this.authService.me()?.displayName ?? 'Receptor',
        receiverContext: { unitId: qr.unitId, traceToken: qr.token },
        lines: this.receiverLines.map(l => ({
          deliveryLineId: l.deliveryLineId, lotId: l.lotId, unit: l.unit,
          quantityAccepted: l.quantityAccepted,
          quantityRejected: Number((l.quantity - l.quantityAccepted).toFixed(3)),
          rejectionReason: l.quantityAccepted < l.quantity ? l.rejectionReason : undefined,
          remainingQuantity: l.remainingQuantity ?? undefined,
          nextRequestedQuantity: l.nextRequestedQuantity ?? undefined
        })),
        photoBlob: this.receiverPhoto() ?? undefined
      });
      this.receiverSaved.set(true);
      this.receiverConfirmedMessage.set('Guardado en este teléfono para tu cuenta. Consulta la cola para ver la confirmación del servidor y el envío de la foto.');
    } catch (error) {
      this.receiverError.set(error instanceof Error ? error.message : 'No se pudo guardar la recepción.');
    } finally {
      this.receiverLoading.set(false);
    }
  }

  resetReceiverScreen(): void {
    this.qrInputText = '';
    this.receiverActiveBox.set(null);
    this.receiverQr.set(null);
    this.receiverLines = [];
    this.receiverPhoto.set(null);
    this.receiverError.set(null);
    this.receiverSaved.set(false);
    this.receiverConfirmedMessage.set(null);
    this.receiverCached.set(false);
  }

  private receiverStateMessage(status: string): string | null {
    switch (status) {
      case 'Delivered':
        return null;
      case 'Received':
      case 'Partial':
        return 'Este bulto ya fue recibido.';
      case 'Sealed':
      case 'Prepared':
        return 'El bulto aún está en bodega. El Conductor aún no entrega este bulto.';
      default:
        return 'El Conductor aún no entrega este bulto.';
    }
  }

  // #162: el conductor solo recoge/entrega; el estado es monotónico (snapshot + comandos confirmados + bultos).
  private driverStage(del: MobileAssignmentDelivery): string {
    const status = this.outboxService.effectiveDeliveryStatus(
      del.deliveryId,
      del.status,
      del.handlingUnits.map((hu) => hu.status),
    );
    if (status === 'Dispatched') {
      const unitsReady = del.handlingUnits.length > 0 && del.handlingUnits.every((hu) => hu.status === 'Dispatched');
      return unitsReady ? 'Dispatched' : 'WaitingWarehouse';
    }
    return status;
  }

  driverStageLabel(del: MobileAssignmentDelivery): string {
    switch (this.driverStage(del)) {
      case 'WaitingWarehouse': return 'Esperando a Bodega';
      case 'Draft': return 'En preparación';
      case 'Dispatched': return 'Listo para recoger';
      case 'InTransit': return 'En ruta';
      case 'Delivered': return 'Entregado';
      case 'Received': return 'Recibido';
      case 'Partial': return 'Recibido parcialmente';
      default: return del.status;
    }
  }

  driverActionable(del: MobileAssignmentDelivery): boolean {
    const stage = this.driverStage(del);
    return stage === 'Dispatched' || stage === 'InTransit';
  }

  driverActionLabel(del: MobileAssignmentDelivery): string {
    switch (this.driverStage(del)) {
      case 'WaitingWarehouse': return 'Esperando a Bodega';
      case 'Dispatched': return 'Recoger →';
      case 'InTransit': return 'Entregar →';
      case 'Delivered': return 'Esperando receptor';
      case 'Received':
      case 'Partial': return 'Recibido';
      default: return 'Abrir →';
    }
  }

  driverActionColor(del: MobileAssignmentDelivery): string {
    switch (this.driverStage(del)) {
      case 'Dispatched': return 'warning';
      case 'InTransit': return 'primary';
      case 'Delivered':
      case 'Received':
      case 'Partial': return 'success';
      default: return 'medium';
    }
  }

  async downloadRoute(): Promise<void> {
    this.loadingRoute.set(true);
    this.downloadMessage.set(null);
    try {
      const assignment = await this.outboxService.downloadRouteAssignment();
      const total = assignment.routes ? assignment.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0) : 0;
      this.downloadMessage.set(
        `Ruta cargada con éxito: ${total} entrega(s) asignada(s) para el vehículo.`
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

  async logout(): Promise<void> {
    const pending = this.outboxService.pendingCount();
    if (pending > 0) {
      const alert = await this.alerts.create({
        header: 'Capturas sin sincronizar',
        message: `Tienes ${pending} ${pending === 1 ? 'captura pendiente' : 'capturas pendientes'}. Se guardarán en este teléfono para tu cuenta y se enviarán cuando vuelvas a entrar.`,
        buttons: [{ text: 'Cancelar', role: 'cancel' }, { text: 'Cerrar sesión', role: 'confirm' }],
      });
      await alert.present();
      if ((await alert.onDidDismiss()).role !== 'confirm') return;
    }
    await this.authService.logout();
  }
}
