import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardSubtitle,
  IonCardContent,
  IonButton,
  IonBadge,
  IonSpinner
} from '@ionic/angular/standalone';
import * as QRCode from 'qrcode';
import {
  DeliveryListItem,
  DeliveryDetailResponse,
  HandlingUnitResponse
} from '@istpetdev/shared-core';
import { FIELD_API } from '../../field-api.interface';

@Component({
  selector: 'app-warehouse',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardSubtitle,
    IonCardContent,
    IonButton,
    IonBadge,
    IonSpinner
  ],
  template: `
    <div class="space-y-4">
      <!-- Encabezado Operativo de Bodega -->
      <ion-card class="m-0 border border-sky-200 bg-white shadow-sm">
        <ion-card-header class="pb-2 bg-sky-50 border-b border-sky-100">
          <div class="flex items-center justify-between">
            <div>
              <ion-card-title class="text-base text-sky-950 font-bold">
                Gestión de Bodega y Despacho Real
              </ion-card-title>
              <ion-card-subtitle class="text-xs text-sky-700">
                Preparación de Bultos, Emisión QR y Custodia Digital (Spec 004)
              </ion-card-subtitle>
            </div>
            <button
              type="button"
              (click)="loadDraftDeliveries()"
              class="px-2.5 py-1 text-xs font-bold text-sky-700 bg-sky-100 hover:bg-sky-200 rounded-lg transition"
              [disabled]="loadingDeliveries()">
              @if (loadingDeliveries()) {
                <ion-spinner name="crescent" class="w-3.5 h-3.5"></ion-spinner>
              } @else {
                ↻ Actualizar
              }
            </button>
          </div>
        </ion-card-header>
      </ion-card>

      <!-- Mensajes de Estado / Alertas -->
      @if (errorMessage()) {
        <div class="p-3 bg-rose-50 border border-rose-200 text-rose-800 rounded-xl text-xs flex items-start justify-between gap-2 shadow-sm">
          <div class="flex items-center gap-2">
            <span class="font-bold">⚠️ Error:</span>
            <span>{{ errorMessage() }}</span>
          </div>
          <button (click)="errorMessage.set(null)" class="text-rose-600 font-black text-sm">✕</button>
        </div>
      }

      @if (successMessage()) {
        <div class="p-3 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-xl text-xs flex items-start justify-between gap-2 shadow-sm">
          <div class="flex items-center gap-2">
            <span class="font-bold">✅ Éxito:</span>
            <span>{{ successMessage() }}</span>
          </div>
          <button (click)="successMessage.set(null)" class="text-emerald-600 font-black text-sm">✕</button>
        </div>
      }

      <!-- VISTA 1: DETALLE DE ENTREGA SELECCIONADA -->
      @if (selectedDelivery(); as detail) {
        <div class="space-y-4">
          <ion-card class="m-0 border border-slate-300 bg-white shadow-md">
            <ion-card-header class="pb-2 bg-slate-50 border-b border-slate-200">
              <div class="flex items-center justify-between">
                <span class="text-xs font-mono font-bold text-slate-500 uppercase">
                  Detalle de Entrega
                </span>
                <button
                  type="button"
                  (click)="closeDetail()"
                  class="text-xs font-bold text-slate-500 hover:text-slate-800 px-2 py-0.5 rounded bg-slate-200">
                  ← Volver a lista
                </button>
              </div>
              <ion-card-title class="text-base font-bold text-slate-900">
                {{ detail.pointName }}
              </ion-card-title>
              <div class="flex items-center gap-2 text-xs text-slate-600">
                <span>Estado:</span>
                <ion-badge [color]="detail.status === 'Draft' ? 'warning' : 'primary'">
                  {{ detail.status }}
                </ion-badge>
                <span class="text-slate-400">•</span>
                <span class="font-mono text-[11px]">Versión {{ detail.version }}</span>
              </div>
              <!-- #160: a qué vehículo y conductor va esta entrega -->
              <div class="text-xs text-slate-700 mt-1" data-delivery-assignment>
                Vehículo <strong>{{ detail.vehicleCode || '—' }}</strong> ·
                Conductor <strong [class.text-rose-700]="!detail.driverName">{{ detail.driverName || 'sin asignar' }}</strong>
                @if (detail.originWarehouseName) { · Sale de <strong>{{ detail.originWarehouseName }}</strong> }
              </div>
            </ion-card-header>

            <ion-card-content class="p-4 space-y-4">
              <!-- Líneas planificadas -->
              <div>
                <h4 class="text-xs font-bold text-slate-700 uppercase tracking-wide mb-2">
                  Líneas de la Entrega (Productos y Lotes)
                </h4>
                <div class="space-y-2">
                  @for (line of detail.lines; track line.deliveryLineId) {
                    <div class="p-2.5 bg-slate-50 border border-slate-200 rounded-lg flex items-center justify-between text-xs">
                      <div>
                        <span class="font-bold text-slate-900 block">{{ line.productName }}</span>
                        <span class="text-[11px] text-slate-500 font-mono">
                          SKU: {{ line.sku }} • Lote: {{ line.lotCode }}
                        </span>
                      </div>
                      <div class="text-right">
                        <span class="text-sm font-black text-slate-800 block">
                          {{ line.plannedQuantity }} {{ line.unit }}
                        </span>
                        <span class="text-[10px] text-slate-400">Planificado</span>
                      </div>
                    </div>
                  }
                </div>
              </div>

              <!-- Bultos ya creados -->
              @if (detail.handlingUnits.length > 0) {
                <div>
                  <h4 class="text-xs font-bold text-slate-700 uppercase tracking-wide mb-2">
                    Bultos Creados en esta Entrega ({{ detail.handlingUnits.length }})
                  </h4>
                  <div class="space-y-1.5">
                    @for (hu of detail.handlingUnits; track hu.handlingUnitId) {
                      <div class="p-2 bg-sky-50 border border-sky-200 rounded-lg flex items-center justify-between text-xs">
                        <div class="flex items-center gap-2">
                          <span class="font-mono font-bold text-sky-900">{{ hu.code }}</span>
                          <span class="text-[10px] bg-sky-200 text-sky-800 px-1.5 py-0.5 rounded font-bold">
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
              <div class="pt-2 border-t border-slate-200 space-y-2">
                @if (detail.status === 'Draft' || detail.handlingUnits.length === 0) {
                  <ion-button
                    expand="block"
                    size="default"
                    color="primary"
                    class="font-bold"
                    [disabled]="preparing()"
                    (click)="createHandlingUnit(detail)">
                    @if (preparing()) {
                      <ion-spinner name="crescent" class="mr-2"></ion-spinner>
                      Preparando Bulto...
                    } @else {
                      📦 Preparar Bulto con Todas las Líneas
                    }
                  </ion-button>
                }

                @if (detail.handlingUnits.length > 0 || createdHandlingUnit()) {
                  <ion-button
                    expand="block"
                    size="default"
                    color="success"
                    class="font-bold"
                    [disabled]="dispatching()"
                    (click)="dispatchToVehicle(detail)">
                    @if (dispatching()) {
                      <ion-spinner name="crescent" class="mr-2"></ion-spinner>
                      Despachando...
                    } @else {
                      🚚 Despachar al Vehículo
                    }
                  </ion-button>
                }
              </div>
            </ion-card-content>
          </ion-card>

          <!-- Bulto recién creado con QR para impresión -->
          @if (createdHandlingUnit(); as created) {
            <ion-card class="m-0 border-2 border-teal-500 bg-white shadow-lg overflow-hidden printable-label">
              <div class="p-3 bg-teal-600 text-white flex items-center justify-between">
                <div>
                  <span class="text-[10px] uppercase tracking-wider font-bold block text-teal-100">
                    Bulto Preparado con Éxito (Spec 004)
                  </span>
                  <h3 class="text-base font-black font-mono">
                    {{ created.hu.code }}
                  </h3>
                </div>
                <button
                  type="button"
                  (click)="printLabel()"
                  class="px-3 py-1.5 bg-white text-teal-900 rounded-lg text-xs font-bold shadow hover:bg-teal-50 transition flex items-center gap-1">
                  🖨️ Imprimir
                </button>
              </div>

              <ion-card-content class="p-4 space-y-3">
                <div class="flex flex-col sm:flex-row items-center gap-4">
                  <!-- QR Code Dibujado -->
                  <div class="shrink-0 p-2 bg-white border-2 border-slate-300 rounded-xl shadow-inner text-center">
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
                      <span class="text-[11px] text-slate-500 font-bold uppercase block">Destino</span>
                      <span class="font-black text-slate-900 text-sm block">{{ detail.pointName }}</span>
                    </div>

                    <div class="p-2.5 bg-slate-50 border border-slate-200 rounded-lg space-y-1">
                      <span class="text-[10px] text-slate-500 font-bold uppercase block">Contenido del Bulto</span>
                      @for (l of created.hu.lines; track l.deliveryLineId) {
                        <div class="flex justify-between items-center text-xs">
                          <span class="font-semibold text-slate-800">{{ l.sku }}</span>
                          <span class="font-bold text-slate-900">{{ l.quantity }} {{ l.unit }}</span>
                        </div>
                      }
                    </div>

                    <div class="text-[11px] text-slate-500">
                      <span>URL de Traza Pública:</span>
                      <a [href]="created.hu.labelUrl" target="_blank" class="text-teal-700 font-mono block break-all underline">
                        {{ created.hu.labelUrl }}
                      </a>
                    </div>
                  </div>
                </div>

                <div class="p-2.5 bg-amber-50 border border-amber-200 rounded-lg text-[11px] text-amber-900 leading-relaxed">
                  ⚠️ <strong>Aviso de Seguridad:</strong> El token de custodia solo se emite en esta respuesta de preparación. Imprima o adhiera la etiqueta antes de cerrar.
                </div>
              </ion-card-content>
            </ion-card>
          }
        </div>
      }

      <!-- VISTA 2: LISTA DE ENTREGAS POR PREPARAR (DRAFT) -->
      @else {
        <div class="space-y-3">
          <div class="flex items-center justify-between px-1">
            <span class="text-xs font-bold uppercase tracking-wider text-slate-600">
              Entregas por Preparar
            </span>
            <span class="text-[11px] text-slate-500 font-bold">
              {{ draftDeliveries().length }} pendientes
            </span>
          </div>

          @if (loadingDeliveries()) {
            <div class="p-8 text-center bg-white rounded-xl border border-slate-200">
              <ion-spinner name="crescent" color="primary"></ion-spinner>
              <p class="text-xs text-slate-500 mt-2 font-medium">Cargando entregas desde la API...</p>
            </div>
          } @else if (draftDeliveries().length === 0) {
            <ion-card class="m-0 border border-dashed border-slate-300 bg-white">
              <ion-card-content class="text-center py-10 space-y-2">
                <span class="text-3xl block">📦</span>
                <p class="text-sm font-bold text-slate-700">No hay entregas por preparar</p>
                <p class="text-xs text-slate-500">
                  Las rutas publicadas con entregas en estado borrador aparecerán aquí.
                </p>
                <ion-button size="small" fill="outline" class="mt-2 font-bold" (click)="loadDraftDeliveries()">
                  Verificar Nuevamente
                </ion-button>
              </ion-card-content>
            </ion-card>
          } @else {
            <div class="space-y-2">
              @for (del of draftDeliveries(); track del.deliveryId) {
                <ion-card
                  class="m-0 border border-slate-200 hover:border-sky-300 shadow-sm bg-white cursor-pointer transition active:scale-[0.99]"
                  (click)="selectDelivery(del)">
                  <ion-card-header class="pb-2">
                    <div class="flex items-center justify-between">
                      <ion-badge color="warning">
                        {{ del.status }}
                      </ion-badge>
                      <span class="text-[11px] text-slate-400 font-mono">
                        {{ del.pointCode }}
                      </span>
                    </div>
                    <ion-card-title class="text-sm font-bold text-slate-900 mt-1">
                      {{ del.pointName }}
                    </ion-card-title>
                    <ion-card-subtitle class="text-xs text-slate-500">
                      Fecha: {{ formatDate(del.dueAtUtc) }} • Bultos HU: {{ del.packagesCount }}
                    </ion-card-subtitle>
                  </ion-card-header>
                  <ion-card-content class="pt-0">
                    <div class="flex items-center justify-between text-xs text-sky-700 font-bold pt-1 border-t border-slate-100">
                      <span>Ver líneas y preparar bulto</span>
                      <span>→</span>
                    </div>
                  </ion-card-content>
                </ion-card>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
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
    }
  `]
})
export class WarehouseComponent implements OnInit {
  private readonly fieldApi = inject(FIELD_API);

  readonly draftDeliveries = signal<DeliveryListItem[]>([]);
  readonly loadingDeliveries = signal<boolean>(false);
  readonly selectedDelivery = signal<DeliveryDetailResponse | null>(null);
  readonly loadingDetail = signal<boolean>(false);
  readonly createdHandlingUnit = signal<{ hu: HandlingUnitResponse; qrDataUrl: string } | null>(null);
  readonly preparing = signal<boolean>(false);
  readonly dispatching = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadDraftDeliveries();
  }

  loadDraftDeliveries(): void {
    this.loadingDeliveries.set(true);
    this.errorMessage.set(null);

    this.fieldApi.getDeliveries(undefined, 'Draft').subscribe({
      next: (res) => {
        this.draftDeliveries.set(res.items || []);
        this.loadingDeliveries.set(false);
      },
      error: (err) => {
        this.loadingDeliveries.set(false);
        this.errorMessage.set(this.formatError(err));
      }
    });
  }

  selectDelivery(del: DeliveryListItem): void {
    this.loadingDetail.set(true);
    this.errorMessage.set(null);
    this.createdHandlingUnit.set(null);

    this.fieldApi.getDelivery(del.deliveryId).subscribe({
      next: (detail) => {
        this.selectedDelivery.set(detail);
        this.loadingDetail.set(false);
      },
      error: (err) => {
        this.loadingDetail.set(false);
        this.errorMessage.set(this.formatError(err));
      }
    });
  }

  closeDetail(): void {
    this.selectedDelivery.set(null);
    this.createdHandlingUnit.set(null);
    this.loadDraftDeliveries();
  }

  createHandlingUnit(detail: DeliveryDetailResponse): void {
    if (!navigator.onLine) {
      this.errorMessage.set('Necesitas señal para preparar o despachar.');
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
          this.selectedDelivery.set({
            ...detail,
            handlingUnits: updatedUnits
          });

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
    if (!navigator.onLine) {
      this.errorMessage.set('Necesitas señal para preparar o despachar.');
      return;
    }

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

    const operationId = crypto.randomUUID();
    this.fieldApi.dispatch(detail.deliveryId, { operationId, handlingUnitIds: huIds }, operationId).subscribe({
      next: (res) => {
        this.dispatching.set(false);
        this.successMessage.set('Entrega despachada exitosamente al vehículo.');
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

  printLabel(): void {
    window.print();
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
    if (!navigator.onLine) {
      return 'Necesitas señal para preparar o despachar.';
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
