import { Component, computed, effect, inject, signal } from '@angular/core';
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
  IonIcon,
  IonInput,
  AlertController,
  ToastController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { logOutOutline, mapOutline, syncOutline } from 'ionicons/icons';
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
    IonIcon,
    IonInput,
    WarehouseComponent
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar color="primary">
        <ion-title class="desktop-brand"><span class="toolbar-brand">istpet<span>dev.</span><small>CAMPO / ECUADOR</small></span></ion-title>
        <ion-buttons slot="end" class="header-actions">
          <ion-button fill="clear" aria-label="Ruta" title="Ruta" (click)="downloadRoute()">
            <ion-icon slot="icon-only" name="map-outline"></ion-icon>
            @if (pendingCount() > 0) { <ion-badge color="warning">{{ pendingCount() }}</ion-badge> }
          </ion-button>
          <ion-button fill="clear" aria-label="Pendientes" title="Pendientes" (click)="openSyncQueue()">
            <ion-icon slot="icon-only" name="sync-outline"></ion-icon>
            @if (pendingCount() > 0) { <ion-badge color="danger">{{ pendingCount() }}</ion-badge> }
          </ion-button>
          <ion-button fill="clear" aria-label="Salir" title="Salir" (click)="logout()">
            <ion-icon slot="icon-only" name="log-out-outline"></ion-icon>
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding field-home">
      <section class="field-hero field-hero-compact" aria-label="Identidad">
        <div class="home-identity-line">
          <span class="eyebrow">EXPO CLEAN ? ECUADOR 2026</span>
          <span class="driver-name">{{ authService.me()?.displayName ?? '' }}</span>
        </div>
      </section>
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

      @if (pendingCount() > 0) {
      <button type="button" class="pending-summary" (click)="openSyncQueue()">
        <span class="summary-icon" aria-hidden="true">↗</span>
        <span><strong>{{ pendingCount() }} operaciones pendientes</strong><small>Consulta el estado de tus registros</small></span>
        <span class="summary-arrow" aria-hidden="true">→</span>
      </button>
      }

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
              <div class="flex items-center justify-between pt-1">
                <span class="text-xs font-bold text-slate-700 uppercase tracking-wide">Líneas a Recibir (Doble Validación)</span>
                <button type="button" class="text-xs font-bold text-sky-700 hover:text-sky-900 underline"
                        (click)="markAllReceiverLinesConform(true)">
                  ✓ Marcar todos conformes
                </button>
              </div>

              @for (line of receiverLines; track line.deliveryLineId) {
                <div class="space-y-3 rounded-xl border p-3 transition select-none"
                     [class.bg-emerald-50]="line.isConform !== false"
                     [class.border-emerald-300]="line.isConform !== false"
                     [class.bg-slate-50]="line.isConform === false"
                     [class.border-slate-200]="line.isConform === false">
                  <div class="flex items-start justify-between gap-2">
                    <div class="flex items-start gap-2.5">
                      <input type="checkbox"
                             class="mt-1 w-4 h-4 text-emerald-600 rounded border-slate-300 focus:ring-emerald-500 cursor-pointer"
                             [checked]="line.isConform !== false"
                             (change)="line.isConform = !line.isConform; onConformToggle(line)" />
                      <div>
                        <h3 class="font-bold text-sm text-slate-900">{{ line.productName }}</h3>
                        <p class="text-xs text-slate-600 font-mono">
                          Lote: <strong class="text-sky-800">{{ line.lotCode }}</strong> · Enviadas: <strong>{{ line.quantity }} {{ line.unit }}</strong>
                        </p>
                      </div>
                    </div>
                    <span class="text-[10px] font-bold px-2 py-0.5 rounded"
                          [class.bg-emerald-200]="line.isConform !== false"
                          [class.text-emerald-900]="line.isConform !== false"
                          [class.bg-amber-200]="line.isConform === false"
                          [class.text-amber-900]="line.isConform === false">
                      {{ line.isConform !== false ? '✓ Conforme' : 'Con Novedad' }}
                    </span>
                  </div>

                  @if (line.isConform === false) {
                    <div class="pt-2 border-t border-slate-200 space-y-2">
                      <label class="block text-xs font-bold text-slate-700">Cantidad aceptada
                        <input class="w-full border rounded p-2 text-sm mt-1" type="number" min="0" [max]="line.quantity" step="0.001"
                          [(ngModel)]="line.quantityAccepted" />
                      </label>
                      <p class="text-xs text-rose-700 font-semibold">Rechazadas: {{ line.quantity - line.quantityAccepted }} {{ line.unit }}</p>
                      @if (line.quantityAccepted < line.quantity) {
                        <label class="block text-xs font-bold text-slate-700">Motivo del rechazo *
                          <select class="w-full border rounded p-2 text-sm mt-1" [(ngModel)]="line.rejectionReason">
                            <option value="">Selecciona un motivo</option>
                            @for (reason of receiverReasons; track reason.code) {
                              <option [value]="reason.code">{{ reason.label }}</option>
                            }
                          </select>
                        </label>
                      }
                    </div>
                  }

                  <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1 border-t border-slate-200/60 text-xs">
                    <label class="block text-slate-600">Stock en percha antes de recibir ({{ line.unit }})
                      <input class="w-full border rounded p-1.5 text-xs mt-0.5" type="number" min="0" step="0.001" [(ngModel)]="line.remainingQuantity" />
                    </label>
                    <label class="block text-slate-600">Solicitado para reabastecimiento ({{ line.unit }})
                      <input class="w-full border rounded p-1.5 text-xs mt-0.5" type="number" min="0" step="0.001" [(ngModel)]="line.nextRequestedQuantity" />
                    </label>
                  </div>
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
        @if (outboxService.activeAssignment()) {
          <div class="driver-actions mb-4">
            <ion-button expand="block" class="driver-action-tile" (click)="showAssignedRoute.set(!showAssignedRoute())">
              {{ showAssignedRoute() ? 'Ocultar Ruta Asignada' : 'Ver Ruta Asignada' }}
            </ion-button>
            <ion-button expand="block" fill="outline" class="driver-action-tile"
              [disabled]="loadingRoute()" (click)="downloadRoute()">
              {{ loadingRoute() ? 'Actualizando...' : 'Actualizar Ruta' }}
            </ion-button>
          </div>
        } @else {
          <div class="driver-actions mb-4">
            <ion-button expand="block" class="driver-action-tile" [disabled]="loadingRoute()" (click)="downloadRoute()">
              {{ loadingRoute() ? 'Cargando...' : 'Descargar Ruta' }}
            </ion-button>
            <ion-button expand="block" fill="outline" class="driver-action-tile" (click)="openIncident()">
              Reportar Incidente
            </ion-button>
          </div>
        }

        <!-- Mensaje si se descarga la ruta -->
        @if (showAssignedRoute() && outboxService.activeAssignment()) {

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
                @for (pickup of route.pickupStops ?? []; track pickup.stopId) {
                  <ion-card class="m-0 border border-amber-300 shadow-sm bg-amber-50">
                    <ion-card-header class="pb-2">
                      <ion-badge color="warning">#{{ pickup.sequence }} - Recogida</ion-badge>
                      <ion-card-title class="text-base">{{ pickup.pointName }}</ion-card-title>
                      @if (pickup.address) { <ion-card-subtitle>{{ pickup.address }}</ion-card-subtitle> }
                    </ion-card-header>
                    <ion-card-content class="pt-0 text-xs font-semibold text-amber-950">
                      Punto de recogida - {{ pickup.pointCode }}
                    </ion-card-content>
                  </ion-card>
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
      }
    </ion-content>
  `
})
export class HomePage {
  readonly authService = inject(MobileAuthService);
  readonly outboxService = inject(FieldOutboxService);
  private readonly fieldApi = inject<IFieldApi>(FIELD_API);
  readonly isMockApi = computed(() => this.fieldApi instanceof MockFieldApiService);
  private readonly router = inject(Router);
  private readonly alerts = inject(AlertController);
  private readonly toastController = inject(ToastController);

  readonly loadingRoute = signal<boolean>(false);
  readonly showAssignedRoute = signal(false);


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
    isConform?: boolean;
  })[] = [];

  onConformToggle(line: (typeof this.receiverLines)[number]): void {
    if (line.isConform) {
      line.quantityAccepted = line.quantity;
      line.rejectionReason = '';
    } else {
      line.quantityAccepted = 0;
    }
  }

  markAllReceiverLinesConform(conform: boolean): void {
    for (const line of this.receiverLines) {
      line.isConform = conform;
      if (conform) {
        line.quantityAccepted = line.quantity;
        line.rejectionReason = '';
      }
    }
  }

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
    addIcons({ logOutOutline, mapOutline, syncOutline });
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
        rejectionReason: '', remainingQuantity: null, nextRequestedQuantity: null, isConform: true }));
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
    try {
      const assignment = await this.outboxService.downloadRouteAssignment();
      const total = assignment.routes ? assignment.routes.reduce((sum: number, r) => sum + r.deliveries.length, 0) : 0;
      this.showAssignedRoute.set(false);
      await this.showToast(`Ruta cargada con éxito: ${total} entrega(s) asignada(s) para el vehículo.`, 'success');
    } catch {
      await this.showToast('Aviso: Utilizando asignación almacenada localmente.', 'warning');
    } finally {
      this.loadingRoute.set(false);
    }
  }

  openScan(): void {
    this.router.navigate(['/scan']);
  }

  openIncident(): void {
    this.router.navigate(['/scan']);
  }

  private async showToast(message: string, color: 'success' | 'warning'): Promise<void> {
    const toast = await this.toastController.create({ message, duration: 3000, position: 'top', color });
    await toast.present();
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
