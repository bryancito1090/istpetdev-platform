import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonSpinner } from '@ionic/angular/standalone';
import * as QRCode from 'qrcode';
import {
  DeliveryListItem,
  DeliveryDetailResponse,
  HandlingUnitResponse
} from '@istpetdev/shared-core';
import { FIELD_API } from '../../field-api.interface';
import { FieldOutboxService } from '../../outbox/field-outbox.service';

@Component({
  selector: 'app-warehouse',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonSpinner
  ],
  template: `
    <div class="space-y-4">
      <!-- Encabezado Operativo de Bodega -->
      <div class="driver-stop-card">
        <div class="flex items-center justify-between">
          <div>
            <span class="stop-tag">Bodega y Despacho</span>
            <h2 class="stop-title mt-1">Preparación y Custodia</h2>
            <p class="stop-address">Preparación de bultos, emisión QR y custodia digital</p>
          </div>
          <button
            type="button"
            (click)="loadDraftDeliveries()"
            class="px-3 py-1.5 text-xs font-bold text-[#00A7C4] bg-sky-50 hover:bg-sky-100 rounded-lg transition"
            [disabled]="loadingDeliveries()">
            @if (loadingDeliveries()) {
              <ion-spinner name="crescent" class="w-3.5 h-3.5"></ion-spinner>
            } @else {
              Actualizar
            }
          </button>
        </div>
      </div>

      <!-- Banner de Modo Sin Conexión (#230) -->
      @if (isOffline()) {
        <div class="p-3 bg-amber-50 border border-amber-300 text-amber-950 rounded-xl text-xs space-y-1 shadow-sm" role="status">
          <div class="flex items-center gap-2 font-bold text-amber-900">
            <span>📡 Modo sin conexión — Mostrando datos en caché</span>
          </div>
          <p class="leading-relaxed text-[11px] text-amber-800">
            Puedes consultar entregas y despachar bultos ya preparados (se guardarán en el teléfono y sincronizarán automáticamente al volver la señal). La preparación de nuevos bultos con QR requiere conexión para emitir identificadores oficiales.
          </p>
        </div>
      }

      <!-- Mensajes de Estado / Alertas -->
      @if (errorMessage()) {
        <div class="p-3 bg-rose-50 border border-rose-200 text-rose-800 rounded-xl text-xs flex items-start justify-between gap-2 shadow-xs">
          <div class="flex items-center gap-2">
            <span class="font-bold">Error:</span>
            <span>{{ errorMessage() }}</span>
          </div>
          <button (click)="errorMessage.set(null)" class="text-rose-600 font-bold text-sm">✕</button>
        </div>
      }

      @if (successMessage()) {
        <div class="p-3 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-xl text-xs flex items-start justify-between gap-2 shadow-xs">
          <div class="flex items-center gap-2">
            <span class="font-bold">Éxito:</span>
            <span>{{ successMessage() }}</span>
          </div>
          <button (click)="successMessage.set(null)" class="text-emerald-600 font-bold text-sm">✕</button>
        </div>
      }

      <!-- VISTA 1: DETALLE DE ENTREGA SELECCIONADA -->
      @if (selectedDelivery(); as detail) {
        <div class="space-y-4">
          <div class="driver-stop-card">
            <div class="flex items-center justify-between mb-2">
              <span class="stop-tag">Detalle de Entrega</span>
              <button
                type="button"
                (click)="closeDetail()"
                class="text-xs font-bold text-slate-500 hover:text-slate-800 px-2.5 py-1 rounded bg-slate-100 transition">
                ← Volver a lista
              </button>
            </div>

            <h2 class="stop-title">
              {{ detail.pointName }}
            </h2>

            <div class="flex items-center gap-2 text-xs text-slate-600 mb-2">
              <span>Estado:</span>
              <span class="status-badge">
                {{ detail.status }}
              </span>
              <span class="text-slate-400">•</span>
              <span class="font-mono text-[11px]">Versión {{ detail.version }}</span>
            </div>

            <!-- a qué vehículo y conductor va esta entrega -->
            <div class="text-xs text-slate-700 pb-3 border-b border-slate-100" data-delivery-assignment>
              Vehículo <strong>{{ detail.vehicleCode || '—' }}</strong> ·
              Conductor <strong [class.text-rose-700]="!detail.driverName">{{ detail.driverName || 'sin asignar' }}</strong>
              @if (detail.originWarehouseName) { · Sale de <strong>{{ detail.originWarehouseName }}</strong> }
            </div>

            <!-- Líneas planificadas con doble verificación de empaque (Checkbox) -->
            <div class="stop-products-section">
              <div class="flex items-center justify-between mb-2.5">
                <span class="stop-products-title">
                  Líneas de la Entrega (Productos y Lotes)
                </span>
                <div class="flex items-center gap-2">
                  <span class="text-[11px] font-bold px-2 py-0.5 rounded-full"
                        [class.bg-emerald-100]="allLinesVerified()"
                        [class.text-emerald-800]="allLinesVerified()"
                        [class.bg-amber-100]="!allLinesVerified()"
                        [class.text-amber-800]="!allLinesVerified()">
                    {{ verifiedLineIds().size }} / {{ detail.lines.length }} verificados
                  </span>
                  <button
                    type="button"
                    class="text-xs font-bold text-[#00A7C4] hover:underline"
                    (click)="toggleVerifyAllLines()">
                    {{ allLinesVerified() ? 'Desmarcar todos' : 'Verificar todos' }}
                  </button>
                </div>
              </div>

              <div class="space-y-2">
                @for (line of detail.lines; track line.deliveryLineId) {
                  <div class="p-3 border rounded-xl flex items-center justify-between text-xs transition cursor-pointer select-none"
                       [class.bg-emerald-50]="isLineVerified(line.deliveryLineId)"
                       [class.border-emerald-200]="isLineVerified(line.deliveryLineId)"
                       [class.bg-slate-50]="!isLineVerified(line.deliveryLineId)"
                       [class.border-slate-200]="!isLineVerified(line.deliveryLineId)"
                       (click)="toggleLineVerify(line.deliveryLineId)">
                    <div class="flex items-center gap-3">
                      <input
                        type="checkbox"
                        class="w-4 h-4 text-emerald-600 rounded border-slate-300 focus:ring-emerald-500 cursor-pointer"
                        [checked]="isLineVerified(line.deliveryLineId)"
                        (click)="$event.stopPropagation()"
                        (change)="toggleLineVerify(line.deliveryLineId)" />
                      <div>
                        <span class="font-bold text-slate-900 block" [class.line-through]="isLineVerified(line.deliveryLineId)">
                          {{ line.productName }}
                        </span>
                        <span class="text-[11px] text-slate-500 font-mono">
                          SKU: {{ line.sku }} · <strong class="text-slate-700">Lote: {{ line.lotCode }}</strong>
                        </span>
                      </div>
                    </div>
                    <div class="text-right">
                      <span class="product-qty block mb-1">
                        {{ line.plannedQuantity }} {{ line.unit }}
                      </span>
                      <span class="text-[10px] font-bold"
                            [class.text-emerald-700]="isLineVerified(line.deliveryLineId)"
                            [class.text-slate-400]="!isLineVerified(line.deliveryLineId)">
                        {{ isLineVerified(line.deliveryLineId) ? 'Verificado' : 'Pendiente' }}
                      </span>
                    </div>
                  </div>
                }
              </div>
            </div>

            <!-- Bultos ya creados -->
            @if (detail.handlingUnits.length > 0) {
              <div class="mb-3">
                <span class="stop-products-title">
                  Bultos Creados en esta Entrega ({{ detail.handlingUnits.length }})
                </span>
                <div class="space-y-1.5">
                  @for (hu of detail.handlingUnits; track hu.handlingUnitId) {
                    <div class="p-2.5 bg-slate-50 border border-slate-200 rounded-lg flex items-center justify-between text-xs">
                      <div class="flex items-center gap-2">
                        <span class="font-mono font-bold text-slate-900">{{ hu.code }}</span>
                        <span class="status-badge">
                          {{ hu.status }}
                        </span>
                      </div>
                      <span class="text-[10px] text-slate-400 font-mono">{{ hu.handlingUnitId.slice(0, 8) }}...</span>
                    </div>
                  }
                </div>
              </div>
            }

            <!-- Acciones: Preparar bulto o Despachar -->
            <div class="pt-2 space-y-2">
              @if (detail.status === 'Draft' || detail.handlingUnits.length === 0) {
                @if (!allLinesVerified() && detail.lines.length > 0) {
                  <div class="p-2.5 bg-amber-50 border border-amber-300 rounded-lg text-xs text-amber-950">
                    Doble verificación requerida: chequea físicamente todos los productos y lotes antes de preparar el bulto.
                  </div>
                }
                <button
                  type="button"
                  class="stop-action-btn"
                  [disabled]="preparing() || (!allLinesVerified() && detail.lines.length > 0)"
                  (click)="createHandlingUnit(detail)">
                  @if (preparing()) {
                    <ion-spinner name="crescent" class="mr-2"></ion-spinner>
                    Preparando Bulto...
                  } @else {
                    Preparar Bulto con Todas las Líneas
                  }
                </button>
              }

              @if (detail.handlingUnits.length > 0 || createdHandlingUnit()) {
                <button
                  type="button"
                  class="stop-action-btn bg-emerald-700 hover:bg-emerald-800"
                  [disabled]="dispatching()"
                  (click)="dispatchToVehicle(detail)">
                  @if (dispatching()) {
                    <ion-spinner name="crescent" class="mr-2"></ion-spinner>
                    Despachando...
                  } @else {
                    Despachar al Vehículo
                  }
                </button>
              }
            </div>
          </div>

          <!-- Bulto recién creado con QR para impresión -->
          @if (createdHandlingUnit(); as created) {
            <div class="driver-stop-card printable-label overflow-hidden">
              <div class="flex items-center justify-between pb-3 border-b border-slate-100">
                <div>
                  <span class="stop-tag">Bulto Preparado</span>
                  <h3 class="stop-title font-mono mt-0.5">
                    {{ created.hu.code }}
                  </h3>
                </div>
                <button
                  type="button"
                  (click)="printLabel()"
                  class="px-3 py-1.5 bg-[#00A7C4] text-white rounded-lg text-xs font-bold hover:bg-[#008CA5] transition">
                  Imprimir etiqueta
                </button>
              </div>

              <div class="print-hide p-2.5 bg-amber-50 text-amber-900 text-xs font-medium border border-amber-200 rounded-lg my-3">
                Imprime ahora: este QR no se puede volver a mostrar
              </div>

              <div class="space-y-3 pt-1">
                <div class="flex flex-col sm:flex-row items-center gap-4">
                  <!-- QR Code Dibujado -->
                  <div class="shrink-0 p-3 bg-white border border-slate-200 rounded-xl text-center shadow-xs">
                    <img
                      [src]="created.qrDataUrl"
                      alt="Código QR de Custodia"
                      class="w-44 h-44 object-contain mx-auto" />
                    <span class="text-[10px] text-slate-500 font-mono mt-1 block">
                      Escaneo público de custodia
                    </span>
                  </div>

                  <!-- Datos de la Etiqueta -->
                  <div class="flex-1 space-y-2 text-xs w-full">
                    <div>
                      <span class="stop-products-title">Destino</span>
                      <span class="font-bold text-slate-900 text-sm block">{{ detail.pointName }}</span>
                    </div>

                    <div class="p-2.5 bg-slate-50 border border-slate-200 rounded-lg space-y-1">
                      <span class="stop-products-title">Contenido del Bulto</span>
                      @for (l of created.hu.lines; track l.deliveryLineId) {
                        <div class="flex justify-between items-center text-xs py-0.5">
                          <span class="font-semibold text-slate-800">{{ l.name || l.sku }}</span>
                          <span class="product-qty">{{ l.quantity }} {{ l.unit }}</span>
                        </div>
                      }
                    </div>

                    <div class="text-[11px] text-slate-500 pt-1">
                      <span>URL de Traza Pública:</span>
                      <a [href]="created.hu.labelUrl" target="_blank" class="text-[#00A7C4] font-mono block break-all underline mt-0.5">
                        {{ created.hu.labelUrl }}
                      </a>
                    </div>
                  </div>
                </div>

                <div class="p-2.5 bg-slate-50 border border-slate-200 rounded-lg text-xs text-slate-700 leading-relaxed">
                  <strong>Aviso de Seguridad:</strong> El token de custodia solo se emite en esta respuesta de preparación. Imprima o adhiera la etiqueta antes de cerrar.
                </div>
              </div>
            </div>
          }
        </div>
      }

      <!-- VISTA 2: LISTA DE ENTREGAS POR PREPARAR (DRAFT) -->
      @else {
        <div class="space-y-3">
          <div class="flex items-center justify-between px-1">
            <span class="text-xs font-bold uppercase tracking-wider text-slate-700">
              Entregas por Preparar
            </span>
            <span class="text-[11px] text-slate-500 font-bold bg-slate-100 px-2 py-0.5 rounded-full">
              {{ draftDeliveries().length }} pendientes
            </span>
          </div>

          @if (loadingDeliveries()) {
            <div class="p-8 text-center bg-white rounded-xl border border-slate-200">
              <ion-spinner name="crescent" color="primary"></ion-spinner>
              <p class="text-xs text-slate-500 mt-2 font-medium">Cargando entregas desde la API...</p>
            </div>
          } @else if (draftDeliveries().length === 0) {
            <div class="driver-stop-card text-center py-8">
              <p class="text-sm font-bold text-slate-700">No hay entregas por preparar</p>
              <p class="text-xs text-slate-500 mt-1 mb-3">
                Las rutas publicadas con entregas en estado borrador aparecerán aquí.
              </p>
              <button
                type="button"
                class="px-3 py-1.5 text-xs font-bold text-[#00A7C4] bg-sky-50 hover:bg-sky-100 rounded-lg transition"
                (click)="loadDraftDeliveries()">
                Verificar Nuevamente
              </button>
            </div>
          } @else {
            <div class="space-y-2">
              @for (del of draftDeliveries(); track del.deliveryId) {
                <div
                  class="driver-stop-card cursor-pointer hover:border-slate-300 transition"
                  (click)="selectDelivery(del)">
                  <div class="flex items-center justify-between mb-1.5">
                    <span class="stop-tag">Punto de distribución · {{ del.pointCode }}</span>
                    <span class="status-badge">
                      {{ del.status }}
                    </span>
                  </div>
                  <h2 class="stop-title">
                    {{ del.pointName }}
                  </h2>
                  <p class="stop-address">
                    Fecha: {{ formatDate(del.dueAtUtc) }} · Bultos HU: {{ del.packagesCount }}
                  </p>
                  <div class="flex items-center justify-between text-xs text-[#00A7C4] font-bold pt-2 border-t border-slate-100">
                    <span>Ver líneas y preparar bulto</span>
                    <span>→</span>
                  </div>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
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

    .stop-products-section {
      padding: 10px 0;
      border-top: 1px solid #F1F5F9;
      border-bottom: 1px solid #F1F5F9;
      margin-bottom: 12px;
    }

    .stop-products-title {
      display: block;
      font-size: 11px;
      font-weight: 750;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: #94A3B8;
      margin-bottom: 6px;
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
      padding: 10px 16px;
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

    @media print {
      :host {
        display: block;
      }
      .printable-label {
        position: fixed;
        left: 0;
        top: 0;
        width: 100%;
        margin: 0;
        padding: 16px;
        box-shadow: none !important;
        border: 2px solid #000 !important;
      }
      .print-hide {
        display: none !important;
      }
    }
  `]
})
export class WarehouseComponent implements OnInit, OnDestroy {
  private readonly fieldApi = inject(FIELD_API);
  private readonly outboxService = inject(FieldOutboxService);

  readonly draftDeliveries = signal<DeliveryListItem[]>([]);
  readonly loadingDeliveries = signal<boolean>(false);
  readonly selectedDelivery = signal<DeliveryDetailResponse | null>(null);
  readonly loadingDetail = signal<boolean>(false);
  readonly createdHandlingUnit = signal<{ hu: HandlingUnitResponse; qrDataUrl: string } | null>(null);
  readonly isOffline = signal<boolean>(typeof navigator !== 'undefined' ? !navigator.onLine : false);

  // Restauración pendiente del título tras imprimir (#161); conserva el título original
  private afterPrintListener: (() => void) | null = null;
  private readonly networkListener = () => {
    const offline = typeof navigator !== 'undefined' ? !navigator.onLine : false;
    this.isOffline.set(offline);
    if (!offline) {
      this.loadDraftDeliveries();
    }
  };

  readonly preparing = signal<boolean>(false);
  readonly dispatching = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Doble verificación física de productos y lotes en mesa de empaque
  readonly verifiedLineIds = signal<Set<string>>(new Set<string>());
  readonly allLinesVerified = computed(() => {
    const detail = this.selectedDelivery();
    if (!detail || !detail.lines || detail.lines.length === 0) return false;
    const verified = this.verifiedLineIds();
    return detail.lines.every((l) => verified.has(l.deliveryLineId));
  });

  toggleLineVerify(lineId: string): void {
    const next = new Set(this.verifiedLineIds());
    if (next.has(lineId)) {
      next.delete(lineId);
    } else {
      next.add(lineId);
    }
    this.verifiedLineIds.set(next);
  }

  toggleVerifyAllLines(): void {
    const detail = this.selectedDelivery();
    if (!detail) return;
    if (this.allLinesVerified()) {
      this.verifiedLineIds.set(new Set());
    } else {
      this.verifiedLineIds.set(new Set(detail.lines.map((l) => l.deliveryLineId)));
    }
  }

  isLineVerified(lineId: string): boolean {
    return this.verifiedLineIds().has(lineId);
  }

  ngOnInit(): void {
    if (typeof window !== 'undefined') {
      window.addEventListener('online', this.networkListener);
      window.addEventListener('offline', this.networkListener);
    }
    this.loadDraftDeliveries();
  }

  loadDraftDeliveries(): void {
    this.loadingDeliveries.set(true);
    this.errorMessage.set(null);
    const offline = typeof navigator !== 'undefined' ? !navigator.onLine : false;
    this.isOffline.set(offline);

    if (offline) {
      this.loadCachedDraftDeliveries();
      this.loadingDeliveries.set(false);
      return;
    }

    this.fieldApi.getDeliveries(undefined, 'Draft').subscribe({
      next: (res) => {
        const items = res.items || [];
        this.draftDeliveries.set(items);
        this.cacheDraftDeliveries(items);
        this.loadingDeliveries.set(false);
      },
      error: (err) => {
        this.loadingDeliveries.set(false);
        const status = err?.status || err?.statusCode;
        if (status === 0 || (typeof navigator !== 'undefined' && !navigator.onLine)) {
          this.isOffline.set(true);
          this.loadCachedDraftDeliveries();
        } else {
          this.errorMessage.set(this.formatError(err));
        }
      }
    });
  }

  selectDelivery(del: DeliveryListItem): void {
    this.loadingDetail.set(true);
    this.errorMessage.set(null);
    this.createdHandlingUnit.set(null);
    this.verifiedLineIds.set(new Set());
    const offline = typeof navigator !== 'undefined' ? !navigator.onLine : false;
    this.isOffline.set(offline);

    if (offline) {
      this.loadCachedDeliveryDetail(del.deliveryId);
      this.loadingDetail.set(false);
      return;
    }

    this.fieldApi.getDelivery(del.deliveryId).subscribe({
      next: (detail) => {
        this.selectedDelivery.set(detail);
        this.cacheDeliveryDetail(detail);
        this.loadingDetail.set(false);
      },
      error: (err) => {
        this.loadingDetail.set(false);
        const status = err?.status || err?.statusCode;
        if (status === 0 || (typeof navigator !== 'undefined' && !navigator.onLine)) {
          this.isOffline.set(true);
          this.loadCachedDeliveryDetail(del.deliveryId);
        } else {
          this.errorMessage.set(this.formatError(err));
        }
      }
    });
  }

  closeDetail(): void {
    this.selectedDelivery.set(null);
    this.createdHandlingUnit.set(null);
    this.verifiedLineIds.set(new Set());
    this.loadDraftDeliveries();
  }

  createHandlingUnit(detail: DeliveryDetailResponse): void {
    if (typeof navigator !== 'undefined' && !navigator.onLine) {
      this.errorMessage.set('La preparación de bultos y emisión de QR requiere conexión con el servidor para emitir el código y token de custodia.');
      return;
    }

    if (!detail.lines || detail.lines.length === 0) {
      this.errorMessage.set('La entrega no tiene líneas para preparar.');
      return;
    }

    this.preparing.set(true);
    this.errorMessage.set(null);

    const operationId = crypto.randomUUID();
    const lines = detail.lines.map((l) => ({
      deliveryLineId: l.deliveryLineId,
      lotId: l.lotId,
      quantity: l.plannedQuantity
    }));

    this.fieldApi.createHandlingUnit(detail.deliveryId, { operationId, lines }).subscribe({
      next: async (hu) => {
        try {
          const qrDataUrl = await QRCode.toDataURL(hu.labelUrl, {
            width: 240,
            margin: 1,
            errorCorrectionLevel: 'M'
          });

          this.createdHandlingUnit.set({ hu, qrDataUrl });

          // Actualizar lista de handlingUnits en el detalle
          const updatedUnits = [...detail.handlingUnits];
          if (!updatedUnits.some((u) => u.handlingUnitId === hu.handlingUnitId)) {
            updatedUnits.push({
              handlingUnitId: hu.handlingUnitId,
              code: hu.code,
              status: hu.status
            });
          }
          const updatedDetail: DeliveryDetailResponse = {
            ...detail,
            handlingUnits: updatedUnits
          };
          this.selectedDelivery.set(updatedDetail);
          this.cacheDeliveryDetail(updatedDetail);

          this.successMessage.set(`Bulto ${hu.code} creado exitosamente.`);
        } catch (qrErr) {
          this.errorMessage.set('Error al generar imagen de código QR.');
        } finally {
          this.preparing.set(false);
        }
      },
      error: (err) => {
        this.preparing.set(false);
        this.errorMessage.set(this.formatError(err));
      }
    });
  }

  dispatchToVehicle(detail: DeliveryDetailResponse): void {
    const huIds = detail.handlingUnits.map((u) => u.handlingUnitId);
    if (this.createdHandlingUnit()) {
      const createdId = this.createdHandlingUnit()!.hu.handlingUnitId;
      if (!huIds.includes(createdId)) {
        huIds.push(createdId);
      }
    }

    if (huIds.length === 0) {
      this.errorMessage.set('Debe crear al menos un bulto antes de despachar.');
      return;
    }

    this.dispatching.set(true);
    this.errorMessage.set(null);

    if (typeof navigator !== 'undefined' && !navigator.onLine) {
      const label = `Despacho ${detail.pointName} (${detail.deliveryId})`;
      this.outboxService.saveDispatchCommand({
        deliveryId: detail.deliveryId,
        handlingUnitIds: huIds,
        label
      }).then(() => {
        this.dispatching.set(false);
        this.successMessage.set('Despacho guardado en este dispositivo. Se enviará automáticamente cuando vuelva la conexión.');
        this.markDeliveryAsDispatchedInCache(detail.deliveryId);
        this.selectedDelivery.set(null);
        this.createdHandlingUnit.set(null);
      }).catch((err) => {
        this.dispatching.set(false);
        this.errorMessage.set(err instanceof Error ? err.message : 'Error al guardar despacho offline.');
      });
      return;
    }

    const operationId = crypto.randomUUID();
    this.fieldApi.dispatch(detail.deliveryId, { operationId, handlingUnitIds: huIds }, operationId).subscribe({
      next: (res) => {
        this.dispatching.set(false);
        this.successMessage.set('Entrega despachada exitosamente al vehículo.');
        this.markDeliveryAsDispatchedInCache(detail.deliveryId);
        this.selectedDelivery.set(null);
        this.createdHandlingUnit.set(null);
        this.loadDraftDeliveries();
      },
      error: (err) => {
        this.dispatching.set(false);
        this.errorMessage.set(this.formatError(err));
      }
    });
  }

  private cacheDraftDeliveries(items: DeliveryListItem[]): void {
    try {
      localStorage.setItem('istpetdev_warehouse_draft_deliveries_cache', JSON.stringify(items));
    } catch {
      // Silencioso
    }
  }

  private loadCachedDraftDeliveries(): void {
    try {
      const raw = localStorage.getItem('istpetdev_warehouse_draft_deliveries_cache');
      if (raw) {
        this.draftDeliveries.set(JSON.parse(raw));
      } else {
        this.draftDeliveries.set([]);
      }
    } catch {
      this.draftDeliveries.set([]);
    }
  }

  private cacheDeliveryDetail(detail: DeliveryDetailResponse): void {
    try {
      localStorage.setItem(`istpetdev_warehouse_detail_${detail.deliveryId}`, JSON.stringify(detail));
    } catch {
      // Silencioso
    }
  }

  private loadCachedDeliveryDetail(deliveryId: string): void {
    try {
      const raw = localStorage.getItem(`istpetdev_warehouse_detail_${deliveryId}`);
      if (raw) {
        this.selectedDelivery.set(JSON.parse(raw));
      } else {
        const draft = this.draftDeliveries().find((d) => d.deliveryId === deliveryId);
        if (draft) {
          this.selectedDelivery.set({
            deliveryId: draft.deliveryId,
            status: draft.status,
            version: 1,
            pointName: draft.pointName,
            routePlanId: draft.routePlanId,
            lines: [],
            handlingUnits: [],
            confirmedEvidenceCount: 0
          });
        } else {
          this.errorMessage.set('No hay datos en caché para esta entrega.');
        }
      }
    } catch {
      this.errorMessage.set('Error al recuperar datos de entrega en caché.');
    }
  }

  private markDeliveryAsDispatchedInCache(deliveryId: string): void {
    try {
      const cached = this.draftDeliveries().filter((d) => d.deliveryId !== deliveryId);
      this.draftDeliveries.set(cached);
      localStorage.setItem('istpetdev_warehouse_draft_deliveries_cache', JSON.stringify(cached));
      const detail = this.selectedDelivery();
      if (detail && detail.deliveryId === deliveryId) {
        const updatedDetail: DeliveryDetailResponse = {
          ...detail,
          status: 'Dispatched',
          handlingUnits: detail.handlingUnits.map((hu) => ({ ...hu, status: 'Dispatched' }))
        };
        this.cacheDeliveryDetail(updatedDetail);
      }
    } catch {
      // Silencioso
    }
  }

  printLabel(): void {
    // El navegador usa document.title como nombre del PDF: uno único por bulto (#161)
    const code = this.createdHandlingUnit()?.hu.code || 'bulto';

    // Sin temporizador: en Chrome Android print() no bloquea y el título volvería
    // antes de «Guardar como PDF». Solo se restaura en afterprint o al destruir.
    if (!this.afterPrintListener) {
      const original = document.title;
      this.afterPrintListener = () => this.restorePrintTitle(original);
      window.addEventListener('afterprint', this.afterPrintListener, { once: true });
    }
    document.title = `etiqueta-${code}-${this.printStamp(new Date())}`;

    window.print();
  }

  ngOnDestroy(): void {
    this.afterPrintListener?.();
    if (typeof window !== 'undefined') {
      window.removeEventListener('online', this.networkListener);
      window.removeEventListener('offline', this.networkListener);
    }
  }

  private restorePrintTitle(original: string): void {
    if (!this.afterPrintListener) return;
    window.removeEventListener('afterprint', this.afterPrintListener);
    this.afterPrintListener = null;
    document.title = original;
  }

  /** yyyyMMddHHmm en hora local. */
  private printStamp(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}${pad(d.getHours())}${pad(d.getMinutes())}`;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('es-EC', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
    } catch {
      return dateStr;
    }
  }

  private formatError(err: any): string {
    if (typeof navigator !== 'undefined' && !navigator.onLine) {
      return 'Sin conexión con el servidor. Mostrando datos en caché.';
    }
    const status = err?.status || err?.statusCode;
    if (status === 409) {
      return 'Entrega ya despachada o con conflicto de estado.';
    }
    if (status === 422) {
      return err?.error?.detail || err?.detail || 'Cantidad o lote no coincide.';
    }
    const title = err?.error?.title || err?.title || err?.error?.code || err?.code;
    const detail = err?.error?.detail || err?.detail || err?.message;
    if (title && detail) {
      return `${title}: ${detail}`;
    }
    return detail || title || 'Error en operación de bodega.';
  }
}
