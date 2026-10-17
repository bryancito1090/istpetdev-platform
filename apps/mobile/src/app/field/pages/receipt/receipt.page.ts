import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  IonContent,
  AlertController
} from '@ionic/angular/standalone';
import { MobileAssignmentDelivery } from '@istpetdev/shared-core';
import { FieldOutboxService, OutboxItem } from '../../outbox/field-outbox.service';
import { FieldPageHeaderComponent } from '../../../shared/field-page-header/field-page-header.component';

type CustodyStage = 'WaitingWarehouse' | 'ToPickup' | 'ToDeliver' | 'Delivered' | 'Received';

@Component({
  selector: 'app-receipt',
  standalone: true,
  imports: [
    CommonModule,
    IonContent,
    FieldPageHeaderComponent
  ],
  template: `
    <app-field-page-header
      title="Custodia en Ruta"
      [pendingCount]="pendingCount()"
      (back)="goHome()"
      (sync)="openSyncQueue()"
      (help)="openTestEnvInfo()" />

    <!-- Contenido Scrollable con Tarjetas Estilo Stripe -->
    <ion-content class="ion-padding stripe-main-content">
      @if (delivery(); as del) {
        <!-- Tarjeta de Información de la Parada -->
        <div class="driver-stop-card mb-4">
          <div class="flex items-center justify-between mb-2">
            <span class="stop-tag">Parada #{{ del.sequence }} · Punto de distribución</span>
            <div class="flex items-center gap-1.5">
              <span class="status-badge"
                    [class.is-done]="stage() === 'Delivered' || stage() === 'Received'"
                    [class.is-intransit]="stage() === 'ToDeliver'">
                {{ stageLabel() }}
              </span>
              <span class="text-[11px] font-mono text-slate-500 bg-slate-100 px-2 py-0.5 rounded-full font-bold">
                #{{ del.sequence }}
              </span>
            </div>
          </div>

          <h2 class="stop-title">{{ del.pointName }}</h2>
          @if (del.address) {
            <p class="stop-address">{{ del.address }}</p>
          }
          <div class="text-[11px] font-mono text-slate-400 mt-1">
            ID: {{ del.deliveryId }}
          </div>
        </div>

        <!-- Bultos de la entrega con Checkbox de Custodia -->
        <div class="driver-stop-card mb-4">
          <div class="flex items-center justify-between mb-2">
            <span class="stop-tag">
              @if (stage() === 'ToPickup') {
                Verificación de Carga (Subida al Vehículo)
              } @else if (stage() === 'ToDeliver') {
                Verificación de Descarga (Entrega en Destino)
              } @else {
                Bultos de la parada
              }
            </span>
            @if (del.handlingUnits.length > 0 && (stage() === 'ToPickup' || stage() === 'ToDeliver')) {
              <span class="text-xs font-bold px-2.5 py-0.5 rounded-full"
                    [class.bg-emerald-100]="allUnitsChecked()"
                    [class.text-emerald-800]="allUnitsChecked()"
                    [class.bg-amber-100]="!allUnitsChecked()"
                    [class.text-amber-800]="!allUnitsChecked()">
                {{ checkedUnits().size }} / {{ del.handlingUnits.length }} verificados
              </span>
            }
          </div>

          <p class="stop-address mb-3">
            @if (stage() === 'ToPickup') {
              Marca los bultos con el checkbox físico o escanéalos con la cámara para certificar la subida.
            } @else if (stage() === 'ToDeliver') {
              Verifica los bultos descargados del vehículo antes de entregarlos al receptor.
            } @else {
              {{ del.handlingUnits.length }} bulto(s) · el receptor confirmará las cantidades con su cuenta
            }
          </p>

          @if (del.handlingUnits.length > 0 && (stage() === 'ToPickup' || stage() === 'ToDeliver')) {
            <div class="flex items-center justify-between pb-3 mb-3 border-b border-slate-100">
              <button
                type="button"
                class="text-xs font-bold text-[#00A7C4] hover:text-[#008CA5] underline"
                (click)="toggleCheckAllUnits()">
                {{ allUnitsChecked() ? 'Desmarcar todos' : '✓ Marcar todos verificados' }}
              </button>
              <button
                type="button"
                class="text-xs font-bold text-slate-700 bg-slate-100 hover:bg-slate-200 px-3 py-1.5 rounded-lg flex items-center gap-1.5 transition"
                (click)="openScanForVerification()">
                📷 Escanear QR
              </button>
            </div>
          }

          @if (verifiedSuccessMsg(); as msg) {
            <div class="p-2.5 mb-3 bg-emerald-50 border border-emerald-300 rounded-lg text-emerald-950 text-xs font-bold flex items-center justify-between">
              <span>✅ {{ msg }}</span>
              <button type="button" class="text-emerald-800 text-sm font-bold" (click)="verifiedSuccessMsg.set(null)">✕</button>
            </div>
          }

          <div class="space-y-2.5">
            @for (hu of del.handlingUnits; track hu.handlingUnitId) {
              <div class="p-3 border rounded-xl flex flex-col gap-2 transition cursor-pointer select-none"
                   [class.bg-emerald-50]="isUnitChecked(hu.handlingUnitId, hu.code)"
                   [class.border-emerald-300]="isUnitChecked(hu.handlingUnitId, hu.code)"
                   [class.bg-slate-50]="!isUnitChecked(hu.handlingUnitId, hu.code)"
                   [class.border-slate-200]="!isUnitChecked(hu.handlingUnitId, hu.code)"
                   (click)="toggleUnitCheck(hu.handlingUnitId)">
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2.5">
                    @if (stage() === 'ToPickup' || stage() === 'ToDeliver') {
                      <input
                        type="checkbox"
                        class="w-4 h-4 text-emerald-600 rounded border-slate-300 focus:ring-emerald-500 cursor-pointer"
                        [checked]="isUnitChecked(hu.handlingUnitId, hu.code)"
                        (click)="$event.stopPropagation()"
                        (change)="toggleUnitCheck(hu.handlingUnitId)" />
                    }
                    <div>
                      <span class="font-bold text-xs text-slate-800 block" [class.line-through]="isUnitChecked(hu.handlingUnitId, hu.code)">
                        {{ hu.code }}
                      </span>
                      <span class="text-[11px] text-slate-500">
                        {{ hu.lines.length }} línea(s) · {{ huStatusLabel(hu.status) }}
                      </span>
                    </div>
                  </div>
                  <span class="text-[11px] font-bold px-2 py-0.5 rounded-full"
                        [class.bg-emerald-200]="isUnitChecked(hu.handlingUnitId, hu.code)"
                        [class.text-emerald-900]="isUnitChecked(hu.handlingUnitId, hu.code)"
                        [class.bg-slate-200]="!isUnitChecked(hu.handlingUnitId, hu.code)"
                        [class.text-slate-700]="!isUnitChecked(hu.handlingUnitId, hu.code)">
                    {{ isUnitChecked(hu.handlingUnitId, hu.code) ? '✓ Verificado' : hu.status }}
                  </span>
                </div>

                <!-- Desglose de productos y líneas del bulto -->
                @if (hu.lines.length > 0) {
                  <div class="mt-1 pt-1.5 border-t border-slate-200/60 space-y-1">
                    @for (l of hu.lines; track l.deliveryLineId) {
                      <div class="product-row">
                        <span class="product-name">• {{ l.name || ('SKU: ' + l.sku) }}</span>
                        <span class="product-qty">{{ l.quantity }} {{ l.unit }}</span>
                      </div>
                    }
                  </div>
                }
              </div>
            }

            @if (del.handlingUnits.length === 0) {
              <div class="p-3 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-900">
                La entrega aún no tiene bultos empacados por Bodega.
              </div>
            }
          </div>
        </div>

        <!-- Acciones de custodia por turno -->
        <div class="space-y-3 pb-8">
          @if (stage() === 'WaitingWarehouse') {
            <div class="driver-stop-card bg-amber-50 border-amber-300 text-center space-y-1">
              <p class="font-bold text-amber-900 text-sm">Esperando a Bodega</p>
              <p class="text-xs text-amber-800">
                Bodega debe empacar, sellar y despachar los bultos antes de que puedas recogerlos.
              </p>
              <button
                type="button"
                class="mt-2 px-3 py-1.5 text-xs font-bold text-amber-900 bg-amber-100 hover:bg-amber-200 border border-amber-300 rounded-lg transition disabled:opacity-50"
                [disabled]="isRefreshing()"
                (click)="refreshData()">
                {{ isRefreshing() ? 'Actualizando...' : '↻ Actualizar estado' }}
              </button>
            </div>
          }

          @if (stage() === 'ToPickup') {
            @if (!allUnitsChecked() && del.handlingUnits.length > 0) {
              <div class="p-3 bg-amber-50 border border-amber-300 rounded-xl text-xs text-amber-950 flex items-start gap-2 shadow-xs">
                <span class="text-amber-700 font-bold text-sm">⚠️</span>
                <span class="leading-relaxed">Doble verificación: Marca o escanea todos los bultos para certificar que subiste la totalidad de la carga al vehículo.</span>
              </div>
            }
            <button
              type="button"
              class="stop-action-btn"
              [disabled]="actionInFlight() || (!allUnitsChecked() && del.handlingUnits.length > 0)"
              (click)="pickup()">
              {{ actionInFlight() ? 'Registrando...' : 'Marcar Recogido (Carga Completa)' }}
            </button>
          }

          @if (stage() === 'ToDeliver') {
            @if (!allUnitsChecked() && del.handlingUnits.length > 0) {
              <div class="p-3 bg-amber-50 border border-amber-300 rounded-xl text-xs text-amber-950 flex items-start gap-2 shadow-xs">
                <span class="text-amber-700 font-bold text-sm">⚠️</span>
                <span class="leading-relaxed">Doble verificación: Verifica los bultos que estás bajando del vehículo antes de confirmar la entrega física.</span>
              </div>
            }
            <button
              type="button"
              class="stop-action-btn"
              [disabled]="actionInFlight() || (!allUnitsChecked() && del.handlingUnits.length > 0)"
              (click)="deliver()">
              {{ actionInFlight() ? 'Registrando...' : 'Marcar Entregado en el Punto' }}
            </button>
          }

          @if (stage() === 'Delivered') {
            <div class="driver-stop-card bg-emerald-50 border-emerald-300 text-center space-y-1">
              <p class="font-bold text-emerald-900 text-sm">Entregado — esperando al Receptor</p>
              <p class="text-xs text-emerald-800">
                El Receptor del punto confirmará la recepción escaneando el QR del bulto.
              </p>
            </div>
          }

          @if (stage() === 'Received') {
            <div class="driver-stop-card bg-emerald-50 border-emerald-300 text-center space-y-1">
              <p class="font-bold text-emerald-900 text-sm">Recibido en el punto</p>
              <p class="text-xs text-emerald-800">La recepción quedó registrada con la cuenta del Receptor.</p>
            </div>
          }

          <!-- Estado real del último comando guardado -->
          @if (activeItem(); as item) {
            <div class="driver-stop-card text-xs space-y-1"
                 [class.bg-sky-50]="item.status === 'SavedOnDevice'"
                 [class.border-sky-200]="item.status === 'SavedOnDevice'"
                 [class.bg-emerald-50]="item.status === 'Complete'"
                 [class.border-emerald-200]="item.status === 'Complete'"
                 [class.bg-amber-50]="item.status === 'RequiresReview' || item.status === 'SessionRequired'"
                 [class.border-amber-300]="item.status === 'RequiresReview' || item.status === 'SessionRequired'">
              <p class="font-bold text-slate-800">
                {{ commandTitle(item) }}: {{ outboxService.statusLabel(item.status) }}
              </p>
              @if (item.status === 'SavedOnDevice') {
                <p class="text-slate-600">Guardado en este teléfono; aún no confirmado por el servidor. Reintenta desde la cola.</p>
              }
              @if (item.status === 'Complete') {
                <p class="text-emerald-800 font-medium">Confirmado por el servidor a las {{ item.custodyResponse?.changedAtUtc | date:'shortTime' }}.</p>
              }
              @if (item.errorMessage) {
                <p class="text-amber-900">{{ item.errorMessage }}</p>
              }
            </div>
          }

          @if (validationError(); as error) {
            <div class="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-700 font-medium">{{ error }}</div>
          }

          <button
            type="button"
            class="w-full py-2.5 px-4 text-xs font-bold text-[#0A2540] bg-white border border-slate-200 hover:bg-slate-50 rounded-xl transition shadow-xs"
            (click)="openSyncQueue()">
            Ver Cola de Sincronización
          </button>
        </div>
      } @else if (loadError(); as loadErrorMsg) {
        <div class="driver-stop-card text-center py-10 space-y-3">
          <p class="text-sm text-slate-600">{{ loadErrorMsg }}</p>
          <button
            type="button"
            class="stop-action-btn"
            (click)="goHome()">
            Volver a Inicio
          </button>
        </div>
      } @else {
        <div class="driver-stop-card text-center py-10">
          <p class="text-sm text-slate-500">Cargando entrega seleccionada...</p>
        </div>
      }
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
      position: absolute;
      top: 2px;
      right: 2px;
      background: #EA601F;
      color: white;
      font-size: 9px;
      font-weight: 750;
      border-radius: 9999px;
      min-width: 16px;
      height: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0 4px;
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

    .status-badge.is-intransit {
      background: #E0F2FE;
      color: #0369A1;
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

    .product-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      font-size: 13px;
      padding: 3px 0;
    }

    .product-name {
      color: #1E293B;
      font-weight: 600;
      flex: 1;
    }

    .product-qty {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      font-weight: 750;
      color: #0A2540;
      background: #F8FAFC;
      border: 1px solid #E2E8F0;
      padding: 2px 8px;
      border-radius: 6px;
      font-size: 12px;
      white-space: nowrap;
    }

    .stop-action-btn {
      width: 100%;
      padding: 12px 16px;
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
export class ReceiptPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly outboxService = inject(FieldOutboxService);
  private readonly alerts = inject(AlertController, { optional: true });

  readonly delivery = signal<MobileAssignmentDelivery | null>(null);
  readonly validationError = signal<string | null>(null);
  readonly loadError = signal<string | null>(null);
  readonly busy = signal<boolean>(false);
  readonly pendingCount = computed(() => {
    return typeof this.outboxService.pendingCount === 'function' ? this.outboxService.pendingCount() : 0;
  });

  // Doble verificación / checklist de custodia física del conductor
  readonly checkedUnits = signal<Set<string>>(new Set<string>());
  readonly verifiedSuccessMsg = signal<string | null>(null);

  readonly allUnitsChecked = computed(() => {
    const del = this.delivery();
    if (!del || !del.handlingUnits || del.handlingUnits.length === 0) return true;
    const checked = this.checkedUnits();
    return del.handlingUnits.every((hu) => checked.has(hu.handlingUnitId) || checked.has(hu.code));
  });

  toggleUnitCheck(unitId: string): void {
    const next = new Set(this.checkedUnits());
    if (next.has(unitId)) {
      next.delete(unitId);
    } else {
      next.add(unitId);
    }
    this.checkedUnits.set(next);
  }

  toggleCheckAllUnits(): void {
    const del = this.delivery();
    if (!del) return;
    if (this.allUnitsChecked()) {
      this.checkedUnits.set(new Set());
    } else {
      this.checkedUnits.set(new Set(del.handlingUnits.map((u) => u.handlingUnitId)));
    }
  }

  isUnitChecked(unitId: string, code?: string): boolean {
    const set = this.checkedUnits();
    return set.has(unitId) || (!!code && set.has(code));
  }

  openScanForVerification(): void {
    this.router.navigate(['/scan']);
  }

  readonly stage = computed<CustodyStage>(() => {
    const del = this.delivery();
    if (!del) return 'WaitingWarehouse';

    // Estado monotónico: snapshot + comandos confirmados + estado de los bultos.
    const status = this.outboxService.effectiveDeliveryStatus(
      del.deliveryId,
      del.status,
      del.handlingUnits.map((hu) => hu.status),
    );

    if (status === 'Delivered') return 'Delivered';
    if (status === 'Received' || status === 'Partial') return 'Received';
    if (status === 'InTransit') return 'ToDeliver';
    if (status === 'Dispatched') {
      const unitsOk = del.handlingUnits.length > 0 && del.handlingUnits.every((hu) => hu.status === 'Dispatched');
      return unitsOk ? 'ToPickup' : 'WaitingWarehouse';
    }
    return 'WaitingWarehouse';
  });

  readonly activeItem = computed<OutboxItem | null>(() => {
    const del = this.delivery();
    if (!del) return null;
    return this.outboxService.outboxItems()
      .filter((i) => i.deliveryId === del.deliveryId && i.commandKind)
      .sort((a, b) => new Date(b.capturedAtUtc).getTime() - new Date(a.capturedAtUtc).getTime())[0] ?? null;
  });

  readonly actionInFlight = computed(() => {
    if (this.busy()) return true;
    const item = this.activeItem();
    if (!item) return false;
    return item.status === 'Sending';
  });

  readonly isRefreshing = signal(false);

  ngOnInit(): void {
    const deliveryId = this.route.snapshot.paramMap?.get('deliveryId');
    if (deliveryId) {
      this.loadDeliveryData(deliveryId);
      const del = this.delivery();
      if (
        del &&
        (del.status === 'Draft' || del.handlingUnits.length === 0) &&
        typeof navigator !== 'undefined' &&
        navigator.onLine &&
        !this.outboxService.isSimulatingOffline?.() &&
        typeof this.outboxService.downloadRouteAssignment === 'function'
      ) {
        void this.refreshData();
      }
    }
  }

  refreshData(): void {
    const deliveryId = this.route.snapshot.paramMap.get('deliveryId');
    if (!deliveryId) return;
    if (
      typeof navigator !== 'undefined' &&
      navigator.onLine &&
      !this.outboxService.isSimulatingOffline?.() &&
      typeof this.outboxService.downloadRouteAssignment === 'function'
    ) {
      this.isRefreshing.set(true);
      this.outboxService
        .downloadRouteAssignment()
        .then(() => {
          this.loadDeliveryData(deliveryId);
        })
        .catch(() => {
          this.loadDeliveryData(deliveryId);
        })
        .finally(() => {
          this.isRefreshing.set(false);
        });
    } else {
      this.loadDeliveryData(deliveryId);
    }

    const verifiedUnitId = this.route.snapshot.queryParamMap?.get('verifiedUnitId');
    const scannedFromStorage = typeof sessionStorage !== 'undefined' ? sessionStorage.getItem('lastScannedUnitId') : null;
    const unitToVerify = verifiedUnitId || scannedFromStorage;

    if (unitToVerify) {
      if (typeof sessionStorage !== 'undefined') {
        sessionStorage.removeItem('lastScannedUnitId');
      }
      const next = new Set(this.checkedUnits());
      next.add(unitToVerify);
      this.checkedUnits.set(next);
      this.verifiedSuccessMsg.set('Bulto verificado correctamente mediante código QR.');
    }
  }

  private loadDeliveryData(deliveryId: string): void {
    const assignment = this.outboxService.activeAssignment();
    let found: MobileAssignmentDelivery | null = null;

    if (assignment && assignment.routes) {
      for (const route of assignment.routes) {
        const d = route.deliveries.find((del: MobileAssignmentDelivery) => del.deliveryId === deliveryId);
        if (d) {
          found = d;
          break;
        }
      }
    }

    if (!found) {
      this.loadError.set('No encontramos esta entrega en tu ruta. Vuelve a Inicio y descarga la ruta.');
      return;
    }

    this.delivery.set(found);
  }

  stageLabel(): string {
    switch (this.stage()) {
      case 'WaitingWarehouse': return 'Esperando a Bodega';
      case 'ToPickup': return 'Listo para recoger';
      case 'ToDeliver': return 'En ruta';
      case 'Delivered': return 'Entregado';
      case 'Received': return 'Recibido';
    }
  }

  stageBadgeColor(): string {
    switch (this.stage()) {
      case 'WaitingWarehouse': return 'warning';
      case 'ToPickup': return 'warning';
      case 'ToDeliver': return 'primary';
      case 'Delivered': return 'success';
      case 'Received': return 'success';
    }
  }

  huStatusLabel(status: string): string {
    switch (status) {
      case 'Sealed':
      case 'Prepared': return 'Empacado por Bodega';
      case 'Dispatched': return 'Despachado al vehículo';
      case 'InTransit': return 'En custodia del conductor';
      case 'Delivered': return 'Entregado en el punto';
      case 'Received': return 'Recibido';
      case 'Partial': return 'Recibido parcialmente';
      default: return status;
    }
  }

  commandTitle(item: OutboxItem): string {
    return item.commandKind === 'Pickup' ? 'Recogida' : 'Entrega';
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }

  openSyncQueue(): void {
    this.router.navigate(['/sync']);
  }

  async pickup(): Promise<void> {
    await this.runCommand('Pickup', 'Recogida de bultos (Conductor)');
  }

  async deliver(): Promise<void> {
    await this.runCommand('Deliver', 'Entrega en punto (Conductor)');
  }

  private async runCommand(command: 'Pickup' | 'Deliver', label: string): Promise<void> {
    const del = this.delivery();
    if (!del || this.busy()) return;

    // Guarda también antes de guardar el comando, no solo en la plantilla.
    const expectedStage: CustodyStage = command === 'Pickup' ? 'ToPickup' : 'ToDeliver';
    if (this.stage() !== expectedStage) {
      this.validationError.set('La entrega no está en el estado que permite esa acción. Descarga la ruta de nuevo.');
      return;
    }

    this.validationError.set(null);
    this.busy.set(true);
    try {
      await this.outboxService.saveCustodyCommand({ deliveryId: del.deliveryId, command, label });
    } catch (error) {
      this.validationError.set(error instanceof Error ? error.message : 'No se pudo guardar el comando de custodia.');
    } finally {
      this.busy.set(false);
    }
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
}
