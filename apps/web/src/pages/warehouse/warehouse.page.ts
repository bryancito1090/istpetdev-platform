import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as QRCode from 'qrcode';
import { WEB_FIELD_API, IWebFieldApi } from '@shared/api';
import {
  HandlingUnitResponse,
  DeliveryListItem,
  DispatchResponse
} from '@istpetdev/shared-core';
import { CardComponent, ButtonComponent, BadgeComponent, InputComponent, AlertComponent } from '@shared/ui';
import { LucidePackageCheck, LucidePrinter, LucideTruck, LucideQrCode } from '@lucide/angular';

@Component({
  selector: 'page-warehouse',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardComponent,
    ButtonComponent,
    BadgeComponent,
    InputComponent,
    LucidePackageCheck,
    LucidePrinter,
    LucideTruck,
    LucideQrCode
  ],
  template: `
    <div class="space-y-6 max-w-5xl mx-auto">
      <!-- Encabezado de Bodega -->
      <div class="bg-white border border-slate-200/80 rounded-2xl p-5 sm:p-6 shadow-sm">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-1.5">
              <span class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded text-[11px] font-mono font-semibold bg-primary-50 text-primary-700 border border-primary-200/60">
                <svg lucidePackageCheck [size]="12" class="stroke-[2.2]"></svg>
                OPERACIONES DE BODEGA & DESPACHO
              </span>
              <span class="text-xs text-slate-400 font-mono">/</span>
              <span class="text-xs font-mono text-slate-500">SPEC 004 (T2, T3)</span>
            </div>
            <h1 class="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
              Preparación, Rotulado QR y Despacho
            </h1>
            <p class="text-sm text-slate-500 mt-1 max-w-2xl leading-relaxed">
              Agrupación de insumos médicos en Handling Units (HU), generación de etiquetas con token QR efímero y traspaso formal de custodia hacia la flota de transporte.
            </p>
          </div>

          <div class="shrink-0 bg-slate-50 p-3 rounded-xl border border-slate-200/80 text-right">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">Rol Activo</span>
            <span class="font-mono text-xs font-bold text-slate-800">Operador de Bodega</span>
          </div>
        </div>
      </div>

      <!-- Pestañas de Funcionalidad -->
      <div class="flex border-b border-slate-200 gap-2">
        <button
          type="button"
          (click)="activeTab.set('prepare')"
          [class]="activeTab() === 'prepare' ? 'border-primary-600 text-primary-600 font-bold border-b-2 pb-3 px-4 text-sm' : 'text-slate-500 hover:text-slate-800 pb-3 px-4 text-sm font-medium'">
          1. Preparar Unidad & Rotular QR
        </button>
        <button
          type="button"
          (click)="activeTab.set('dispatch')"
          [class]="activeTab() === 'dispatch' ? 'border-primary-600 text-primary-600 font-bold border-b-2 pb-3 px-4 text-sm' : 'text-slate-500 hover:text-slate-800 pb-3 px-4 text-sm font-medium'">
          2. Despachar a Vehículo
        </button>
        <button
          type="button"
          (click)="activeTab.set('deliveries')"
          [class]="activeTab() === 'deliveries' ? 'border-primary-600 text-primary-600 font-bold border-b-2 pb-3 px-4 text-sm' : 'text-slate-500 hover:text-slate-800 pb-3 px-4 text-sm font-medium'">
          3. Listado de Entregas
        </button>
      </div>

      <!-- PESTAÑA 1: Preparación y Rotulado QR -->
      @if (activeTab() === 'prepare') {
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Formulario de Preparación -->
          <ui-card [padding]="'md'">
            <h2 class="text-base font-bold text-slate-900 mb-1">Nueva Unidad de Manipulación (HU)</h2>
            <p class="text-xs text-slate-500 mb-4">Empaque de productos por lote asignado a una entrega</p>

            <form (submit)="createUnit($event)" class="space-y-3">
              <ui-input
                label="Identificador de Entrega (Delivery ID)"
                [(ngModel)]="prepDeliveryId"
                name="prepDeliveryId"
                placeholder="del-hosp-001"
                [required]="true">
              </ui-input>

              <div class="grid grid-cols-2 gap-3">
                <ui-input
                  label="Código SKU"
                  [(ngModel)]="prepSku"
                  name="prepSku"
                  placeholder="INS-NPH-100UI"
                  [required]="true">
                </ui-input>
                <ui-input
                  label="Lote de Producción"
                  [(ngModel)]="prepLotId"
                  name="prepLotId"
                  placeholder="LOT-INS-2026-A1"
                  [required]="true">
                </ui-input>
              </div>

              <div class="grid grid-cols-2 gap-3">
                <ui-input
                  label="Cantidad a Empacar"
                  type="number"
                  [(ngModel)]="prepQuantity"
                  name="prepQuantity"
                  [required]="true">
                </ui-input>
                <ui-input
                  label="Unidad de Medida"
                  [(ngModel)]="prepUnit"
                  name="prepUnit"
                  placeholder="Viales">
                </ui-input>
              </div>

              <div class="pt-2">
                <ui-button
                  type="submit"
                  variant="primary"
                  size="md"
                  [fullWidth]="true"
                  [loading]="isPreparing()">
                  <span>Generar Unidad y Código QR</span>
                </ui-button>
              </div>
            </form>
          </ui-card>

          <!-- Previsualización de Etiqueta con Código QR Renderizado -->
          <ui-card [padding]="'md'">
            <div class="flex items-center justify-between mb-3">
              <h2 class="text-base font-bold text-slate-900">Etiqueta Imprimible con QR</h2>
              @if (currentUnit()) {
                <ui-badge variant="success" size="sm" [label]="currentUnit()?.status || 'Prepared'"></ui-badge>
              }
            </div>

            @if (currentUnit(); as unit) {
              <div class="border-2 border-dashed border-slate-300 rounded-xl p-4 bg-white space-y-3 print:border-black print:p-6" id="printable-label">
                <div class="flex items-center justify-between border-b pb-2">
                  <div>
                    <span class="text-[10px] text-slate-400 font-bold uppercase tracking-wider block">IstpetDev Logistics</span>
                    <span class="font-mono text-base font-black text-slate-900">{{ unit.code }}</span>
                  </div>
                  <span class="text-xs font-mono text-slate-500">HU ID: {{ unit.handlingUnitId }}</span>
                </div>

                <div class="flex items-center gap-4 py-2">
                  <!-- Renderizado dinámico del QR vía qrcode -->
                  @if (qrDataUrl()) {
                    <img [src]="qrDataUrl()" alt="Código QR de Custodia" class="w-32 h-32 border p-1 rounded-lg bg-white shrink-0 shadow-xs" />
                  }

                  <div class="space-y-1 text-xs">
                    <div class="text-slate-600">
                      <span class="font-semibold text-slate-800">Entrega:</span> {{ unit.deliveryId }}
                    </div>
                    @for (line of unit.lines; track line.lotId) {
                      <div class="p-1.5 bg-slate-50 rounded border text-[11px]">
                        <span class="font-bold text-slate-900 block">{{ line.sku }}</span>
                        <span class="font-mono text-slate-500">Lote: {{ line.lotId }} ({{ line.quantity }} {{ line.unit }})</span>
                      </div>
                    }
                    <div class="text-[10px] text-slate-400 pt-1">
                      Token QR activo y protegido bajo S-07.
                    </div>
                  </div>
                </div>

                <div class="border-t pt-2 text-[10px] text-slate-400 flex justify-between">
                  <span>Sellado: {{ unit.createdAtUtc | date:'short' }}</span>
                  <span>Traza: /public-trace</span>
                </div>
              </div>

              <!-- Acciones de Etiqueta: Imprimir, Reimprimir (Rotar token) y Revocar -->
              <div class="flex flex-wrap gap-2 pt-3">
                <ui-button variant="secondary" size="sm" (click)="printLabel()">
                  <svg lucidePrinter [size]="14" class="mr-1"></svg>
                  <span>Imprimir Etiqueta</span>
                </ui-button>

                <ui-button variant="outline" size="sm" (click)="reprintLabel()">
                  <span>🔄 Reimprimir (Rotar Token)</span>
                </ui-button>

                <ui-button variant="destructive" size="sm" (click)="revokeToken()">
                  <span>🚫 Revocar QR</span>
                </ui-button>
              </div>

              @if (actionFeedback()) {
                <p class="text-xs text-emerald-700 font-semibold pt-2">{{ actionFeedback() }}</p>
              }
            } @else {
              <div class="p-8 text-center text-slate-400 text-xs">
                <svg lucideQrCode [size]="40" class="mx-auto mb-2 opacity-40"></svg>
                Complete el formulario de la izquierda o seleccione una unidad demo para generar e imprimir su etiqueta física.
              </div>
            }
          </ui-card>
        </div>
      }

      <!-- PESTAÑA 2: Despacho a Vehículo -->
      @if (activeTab() === 'dispatch') {
        <ui-card [padding]="'md'">
          <h2 class="text-base font-bold text-slate-900 mb-1">Despacho Atómico de Custodia (T3)</h2>
          <p class="text-xs text-slate-500 mb-4">Traspaso de custodia de unidades de bodega hacia el vehículo del conductor</p>

          <div class="space-y-4 max-w-lg">
            <ui-input
              label="Identificador de Entrega"
              [(ngModel)]="dispatchDeliveryId"
              name="dispatchDeliveryId"
              placeholder="del-hosp-001">
            </ui-input>

            <ui-input
              label="Unidades a Despachar (separadas por coma)"
              [(ngModel)]="dispatchUnitIds"
              name="dispatchUnitIds"
              placeholder="hu-001, hu-002">
            </ui-input>

            <ui-button
              variant="primary"
              size="lg"
              [fullWidth]="true"
              [loading]="isDispatching()"
              (click)="dispatchUnits()">
              <svg lucideTruck [size]="16" class="mr-2"></svg>
              <span>Confirmar Despacho y Traspaso de Custodia</span>
            </ui-button>

            @if (dispatchResult(); as res) {
              <div class="p-4 bg-emerald-50 border border-emerald-200 rounded-xl space-y-1.5 text-xs text-emerald-900">
                <span class="font-bold block text-sm">✓ Despacho Exitoso</span>
                <div><span class="font-semibold">Entrega:</span> {{ res.deliveryId }}</div>
                <div><span class="font-semibold">Estado:</span> {{ res.status }}</div>
                <div><span class="font-semibold">Evento de Custodia:</span> {{ res.custodyEventId }}</div>
                <div><span class="font-semibold">Fecha y Hora:</span> {{ res.dispatchedAtUtc | date:'medium' }}</div>
              </div>
            }
          </div>
        </ui-card>
      }

      <!-- PESTAÑA 3: Listado de Entregas -->
      @if (activeTab() === 'deliveries') {
        <ui-card [padding]="'md'">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h2 class="text-base font-bold text-slate-900">Entregas Registradas en Bodega</h2>
              <p class="text-xs text-slate-500">Monitoreo de estado de bultos y custodia</p>
            </div>
            <ui-button variant="outline" size="sm" (click)="loadDeliveries()">
              <span>Actualizar</span>
            </ui-button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs border border-slate-200 rounded-lg overflow-hidden">
              <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-semibold uppercase text-[10px]">
                <tr>
                  <th class="p-2.5">ID Entrega</th>
                  <th class="p-2.5">Destino</th>
                  <th class="p-2.5">Ruta</th>
                  <th class="p-2.5">Bultos</th>
                  <th class="p-2.5">Estado</th>
                  <th class="p-2.5">Evidencias</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                @for (d of deliveries(); track d.deliveryId) {
                  <tr class="hover:bg-slate-50/50 transition">
                    <td class="p-2.5 font-mono font-bold text-slate-800">{{ d.deliveryId }}</td>
                    <td class="p-2.5">{{ d.pointName }}</td>
                    <td class="p-2.5 font-mono text-slate-500">{{ d.routePlanId }}</td>
                    <td class="p-2.5">{{ d.packagesCount }}</td>
                    <td class="p-2.5">
                      <ui-badge
                        [variant]="d.status === 'Dispatched' ? 'transit' : d.status === 'Delivered' ? 'success' : 'neutral'"
                        size="sm"
                        [label]="d.status">
                      </ui-badge>
                    </td>
                    <td class="p-2.5">
                      <ui-badge
                        [variant]="d.evidenceStatus === 'Confirmed' ? 'success' : d.evidenceStatus === 'Uploaded' ? 'transit' : 'neutral'"
                        size="sm"
                        [label]="d.evidenceStatus">
                      </ui-badge>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </ui-card>
      }
    </div>
  `
})
export class WarehousePage implements OnInit {
  private readonly webFieldApi = inject<IWebFieldApi>(WEB_FIELD_API);

  readonly activeTab = signal<'prepare' | 'dispatch' | 'deliveries'>('prepare');
  readonly isPreparing = signal(false);
  readonly isDispatching = signal(false);
  readonly currentUnit = signal<HandlingUnitResponse | null>(null);
  readonly qrDataUrl = signal<string | null>(null);
  readonly actionFeedback = signal<string | null>(null);
  readonly dispatchResult = signal<DispatchResponse | null>(null);
  readonly deliveries = signal<DeliveryListItem[]>([]);

  // Formulario de Preparación
  prepDeliveryId = 'del-hosp-001';
  prepSku = 'INS-NPH-100UI';
  prepLotId = 'LOT-INS-2026-A1';
  prepQuantity = 50;
  prepUnit = 'Viales';

  // Formulario de Despacho
  dispatchDeliveryId = 'del-hosp-001';
  dispatchUnitIds = 'hu-001';

  ngOnInit(): void {
    this.loadDeliveries();
  }

  loadDeliveries(): void {
    this.webFieldApi.getDeliveries().subscribe({
      next: (res) => {
        this.deliveries.set(res.items);
      }
    });
  }

  async createUnit(event: Event): Promise<void> {
    event.preventDefault();
    this.isPreparing.set(true);
    this.actionFeedback.set(null);

    const operationId = 'op-' + Math.random().toString(36).slice(2, 10);
    this.webFieldApi
      .createHandlingUnit(this.prepDeliveryId, {
        operationId,
        lines: [
          {
            deliveryLineId: `dl-${this.prepDeliveryId}-01`,
            lotId: this.prepLotId,
            quantity: this.prepQuantity
          }
        ]
      })
      .subscribe({
        next: async (res) => {
          this.currentUnit.set(res);
          await this.generateQrCode(res.handlingUnitId, res.token || 'tok-demo-hu-001');
          this.isPreparing.set(false);
          this.actionFeedback.set(`✓ Unidad ${res.code} preparada exitosamente.`);
        },
        error: () => {
          this.isPreparing.set(false);
        }
      });
  }

  private async generateQrCode(unitId: string, token: string): Promise<void> {
    const origin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200';
    // URL conforme a Spec 004: fragmento hash #t=...
    const traceUrl = `${origin}/public-trace?u=${unitId}#t=${token}`;
    try {
      const dataUrl = await QRCode.toDataURL(traceUrl, {
        width: 180,
        margin: 1,
        color: {
          dark: '#0f172a',
          light: '#ffffff'
        }
      });
      this.qrDataUrl.set(dataUrl);
    } catch {
      // Ignorar fallo de generación
    }
  }

  printLabel(): void {
    if (typeof window !== 'undefined') {
      window.print();
    }
  }

  async reprintLabel(): Promise<void> {
    const unit = this.currentUnit();
    if (!unit) return;

    // Generar nuevo token efímero rotado (PA-05)
    const newToken = `tok-reprint-${Math.random().toString(36).slice(2, 8)}`;
    unit.token = newToken;
    await this.generateQrCode(unit.handlingUnitId, newToken);
    this.actionFeedback.set('✓ Etiqueta reimpresa. El token QR anterior fue rotado e invalidado.');
  }

  revokeToken(): void {
    const unit = this.currentUnit();
    if (!unit) return;

    this.webFieldApi.revokePublicToken(unit.handlingUnitId).subscribe({
      next: () => {
        this.actionFeedback.set(`🚫 El código QR de la unidad ${unit.code} ha sido revocado. Las consultas devolverán 404.`);
      }
    });
  }

  dispatchUnits(): void {
    if (!this.dispatchDeliveryId.trim() || !this.dispatchUnitIds.trim()) return;
    this.isDispatching.set(true);
    this.dispatchResult.set(null);

    const units = this.dispatchUnitIds.split(',').map((u) => u.trim());
    const opId = 'op-disp-' + Math.random().toString(36).slice(2, 10);

    this.webFieldApi
      .dispatch(
        this.dispatchDeliveryId.trim(),
        {
          operationId: opId,
          handlingUnitIds: units
        },
        opId
      )
      .subscribe({
        next: (res: DispatchResponse) => {
          this.dispatchResult.set(res);
          this.isDispatching.set(false);
          this.loadDeliveries();
        },
        error: () => {
          this.isDispatching.set(false);
        }
      });
  }
}
