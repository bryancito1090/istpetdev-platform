import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService, ReturnGuideDto, ReceiveReturnGuideResponse } from '@shared/api';
import { AuthSession } from '@entities/session';

@Component({
  selector: 'page-guia-retorno',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-5xl mx-auto px-4 py-8 space-y-6 font-sans text-[#30313D]">
      <!-- Barra de navegación y acciones superior (oculta al imprimir) -->
      <div class="flex flex-wrap items-center justify-between gap-4 print:hidden border-b border-[#E3E8EE] pb-4">
        <div class="flex items-center gap-3">
          <a routerLink="/rutas" class="inline-flex items-center gap-1.5 text-xs font-semibold text-[#635BFF] hover:underline">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            Volver a Rutas
          </a>
          <span class="text-[#8792A2]">|</span>
          <span class="text-xs text-[#697386] font-medium">Logística Inversa &bull; Custodia y Devoluciones</span>
        </div>

        <div class="flex items-center gap-2">
          <!-- Búsqueda rápida por número de guía -->
          <form (submit)="searchGuide()" class="flex items-center gap-1">
            <input
              type="text"
              [(ngModel)]="searchQuery"
              name="searchQuery"
              placeholder="Ej. GRT-B1C2D3E4"
              class="h-8 px-2.5 text-xs border border-[#CBD5E1] rounded-md focus:outline-none focus:border-[#635BFF] font-mono"
            />
            <button
              type="submit"
              class="h-8 px-3 text-xs font-medium bg-[#F1F5F9] text-[#334155] rounded-md hover:bg-[#E2E8F0] transition"
            >
              Buscar
            </button>
          </form>

          <button
            type="button"
            (click)="printGuide()"
            class="inline-flex items-center gap-1.5 h-8 px-3 text-xs font-medium bg-white border border-[#CBD5E1] text-[#1E293B] rounded-md shadow-xs hover:bg-[#F8FAFC] transition"
          >
            <svg class="w-3.5 h-3.5 text-[#64748B]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
            </svg>
            Imprimir Guía
          </button>

          <!-- Botón de recepción en bodega -->
          @if (canReceiveInWarehouse() && guide() && guide()!.status !== 'ReceivedAtWarehouse' && !reconcileSuccess()) {
            <button
              type="button"
              (click)="receiveInWarehouse()"
              [disabled]="processingReceive()"
              class="inline-flex items-center gap-1.5 h-8 px-3 text-xs font-medium bg-[#059669] text-white rounded-md shadow-xs hover:bg-[#047857] transition disabled:opacity-50"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              {{ processingReceive() ? 'Conciliando...' : 'Recepción en Bodega' }}
            </button>
          }
        </div>
      </div>

      <!-- Mensajes de Estado / Alertas -->
      @if (errorMessage()) {
        <div class="p-4 rounded-lg bg-red-50 border border-red-200 text-red-800 text-xs flex items-center justify-between print:hidden">
          <span>{{ errorMessage() }}</span>
          <button (click)="errorMessage.set(null)" class="text-red-500 hover:text-red-700 font-bold">&times;</button>
        </div>
      }

      @if (reconcileSuccess()) {
        <div class="p-4 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-900 text-xs space-y-1 print:hidden shadow-xs">
          <div class="flex items-center gap-2 font-semibold text-emerald-800">
            <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Recepción confirmada y stock conciliado en bodega de destino
          </div>
          <p class="text-[11px] text-emerald-700">
            Se generó el pedido de reinyección prioritaria con ID:
            <span class="font-mono font-bold">{{ reconcileSuccess()!.rebalanceOrderId }}</span>.
            Esta demanda queda programada con prioridad ALTA/CRÍTICA para la planificación del día siguiente.
          </p>
        </div>
      }

      <!-- Indicador de Carga -->
      @if (loading()) {
        <div class="py-16 text-center text-xs text-[#64748B] flex flex-col items-center justify-center gap-2">
          <div class="w-6 h-6 border-2 border-[#635BFF] border-t-transparent rounded-full animate-spin"></div>
          <span>Cargando datos de la guía de retorno...</span>
        </div>
      }

      <!-- DOCUMENTO IMPRIMIBLE: GUÍA DE RETORNO -->
      @if (!loading() && guide()) {
        <div class="bg-white border border-[#CBD5E1] rounded-lg p-6 sm:p-8 space-y-6 shadow-sm print:border-none print:shadow-none print:p-0">
          
          <!-- Encabezado Oficial -->
          <div class="border-b-2 border-[#1E293B] pb-4 flex flex-col sm:flex-row sm:items-start justify-between gap-4">
            <div>
              <div class="flex items-center gap-2">
                <span class="text-[11px] font-bold uppercase tracking-wider px-2 py-0.5 rounded bg-[#1E293B] text-white">
                  ISTPETDEV LOGISTICS
                </span>
                <span class="text-[11px] text-[#64748B] font-mono">CADENA DE CUSTODIA V1</span>
              </div>
              <h1 class="text-xl sm:text-2xl font-bold text-[#0F172A] mt-1 tracking-tight">
                GUÍA DE RETORNO DE MATERIAL NO ENTREGADO
              </h1>
              <p class="text-xs text-[#64748B]">
                Comprobante de custodia y reintegro físico de unidades no entregadas o rechazadas en ruta
              </p>
            </div>

            <div class="text-left sm:text-right shrink-0 bg-[#F8FAFC] border border-[#E2E8F0] p-3 rounded-lg print:border-slate-300">
              <span class="text-[10px] uppercase font-bold text-[#64748B] block">Número de Guía</span>
              <span class="text-lg font-mono font-extrabold text-[#0F172A] block">{{ guide()!.guideNumber }}</span>
              <div class="mt-1">
                @if (guide()!.status === 'ReceivedAtWarehouse' || reconcileSuccess()) {
                  <span class="inline-flex items-center gap-1 text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200">
                    &bull; RECIBIDO EN BODEGA
                  </span>
                } @else {
                  <span class="inline-flex items-center gap-1 text-[10px] font-semibold px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 border border-amber-200">
                    &bull; PENDIENTE DE RECEPCIÓN
                  </span>
                }
              </div>
            </div>
          </div>

          <!-- Metadatos de la Operación -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 p-4 rounded-lg bg-[#F8FAFC] border border-[#E2E8F0] text-xs">
            <div>
              <span class="text-[10px] uppercase font-semibold text-[#64748B] block">Fecha de Emisión</span>
              <span class="font-medium text-[#1E293B]">{{ guide()!.generatedAtUtc | date:'dd/MM/yyyy HH:mm' }}</span>
            </div>
            <div>
              <span class="text-[10px] uppercase font-semibold text-[#64748B] block">Vehículo Asignado</span>
              <span class="font-semibold text-[#1E293B] font-mono">{{ guide()!.vehiclePlate || 'N/A' }}</span>
            </div>
            <div>
              <span class="text-[10px] uppercase font-semibold text-[#64748B] block">Conductor / Custodio</span>
              <span class="font-medium text-[#1E293B]">{{ guide()!.driverName || 'N/A' }}</span>
            </div>
            <div>
              <span class="text-[10px] uppercase font-semibold text-[#64748B] block">Bodega de Destino</span>
              <span class="font-semibold text-[#0F172A]">{{ guide()!.destinationWarehouseName || 'Bodega Central' }}</span>
            </div>
          </div>

          <!-- Detalle de Bultos e Insumos Devueltos -->
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <h2 class="text-xs font-bold uppercase tracking-wider text-[#334155]">
                Bultos y Productos en Custodia de Retorno ({{ guide()!.lines.length }})
              </h2>
              <span class="text-[11px] text-[#64748B]">
                Reinyección automática al plan diario: <strong>Prioridad ALTA / CRÍTICA</strong>
              </span>
            </div>

            @if (guide()!.lines.length === 0) {
              <div class="p-6 text-center text-xs text-[#64748B] bg-[#F8FAFC] rounded-lg border border-dashed border-[#CBD5E1]">
                No hay bultos registrados como no entregados en este plan de ruta.
              </div>
            } @else {
              <div class="overflow-x-auto border border-[#E2E8F0] rounded-lg">
                <table class="w-full text-left text-xs border-collapse">
                  <thead>
                    <tr class="bg-[#F1F5F9] border-b border-[#E2E8F0] text-[11px] font-semibold text-[#475569]">
                      <th class="py-2.5 px-3">Bulto (HU)</th>
                      <th class="py-2.5 px-3">SKU</th>
                      <th class="py-2.5 px-3">Descripción Producto</th>
                      <th class="py-2.5 px-3">Lote</th>
                      <th class="py-2.5 px-3 text-right">Cantidad</th>
                      <th class="py-2.5 px-3">Motivo de Retorno</th>
                      <th class="py-2.5 px-3 text-center print:hidden">Trazabilidad</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-[#E2E8F0] text-[#1E293B]">
                    @for (line of guide()!.lines; track line.handlingUnitId + line.lotId) {
                      <tr class="hover:bg-[#F8FAFC]">
                        <td class="py-2.5 px-3 font-mono font-medium text-[#0F172A] whitespace-nowrap">
                          {{ line.handlingUnitCode }}
                        </td>
                        <td class="py-2.5 px-3 font-mono text-[#475569] whitespace-nowrap">
                          {{ line.sku }}
                        </td>
                        <td class="py-2.5 px-3 font-medium">
                          {{ line.productName }}
                        </td>
                        <td class="py-2.5 px-3 font-mono text-[#64748B] whitespace-nowrap">
                          {{ line.lotCode }}
                        </td>
                        <td class="py-2.5 px-3 text-right font-mono font-semibold whitespace-nowrap">
                          {{ line.quantity }} {{ line.unit }}
                        </td>
                        <td class="py-2.5 px-3 text-[#B91C1C] font-medium">
                          {{ line.reason || 'No entregado en ruta' }}
                        </td>
                        <td class="py-2.5 px-3 text-center print:hidden">
                          <a
                            [routerLink]="['/public-trace', line.handlingUnitId]"
                            target="_blank"
                            class="inline-flex items-center gap-1 text-[11px] text-[#635BFF] hover:underline"
                            title="Ver trazabilidad QR de este bulto"
                          >
                            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z" />
                            </svg>
                            Ver QR
                          </a>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </div>

          <!-- Nota Operativa de Reinyección de Demanda -->
          <div class="p-4 rounded-lg bg-[#EFF6FF] border border-[#BFDBFE] text-xs text-[#1E40AF] space-y-1">
            <span class="font-semibold block text-[11px] uppercase tracking-wide">Regla Operativa de Reinyección (Issue #247):</span>
            <p class="leading-relaxed text-[11px]">
              Al recibirse físicamente en la bodega de destino, estos insumos se reintegran automáticamente al stock disponible
              y se genera un pedido de reposición inmediata para el planificador con prioridad <strong>ALTA (o CRÍTICA si el destino es un punto vital)</strong>
              para ser despachado en el primer turno de la jornada siguiente.
            </p>
          </div>

          <!-- Bloque de Firmas y Cadena de Custodia Física -->
          <div class="pt-8 border-t border-[#E2E8F0] grid grid-cols-2 gap-8 text-xs">
            <div class="border border-[#CBD5E1] rounded-lg p-4 space-y-4">
              <div class="text-[11px] font-bold uppercase text-[#475569] border-b border-[#E2E8F0] pb-1">
                Entregado por (Conductor / Custodia en Ruta)
              </div>
              <div class="h-16 border-b border-dashed border-[#94A3B8]"></div>
              <div class="space-y-1 text-[11px] text-[#64748B]">
                <div>Nombre: <strong class="text-[#1E293B]">{{ guide()!.driverName || '................................................' }}</strong></div>
                <div>Vehículo: <strong class="text-[#1E293B]">{{ guide()!.vehiclePlate || '................................................' }}</strong></div>
                <div>Fecha y Hora: <span class="font-mono text-[#1E293B]">{{ guide()!.generatedAtUtc | date:'dd/MM/yyyy HH:mm' }}</span></div>
              </div>
            </div>

            <div class="border border-[#CBD5E1] rounded-lg p-4 space-y-4">
              <div class="text-[11px] font-bold uppercase text-[#475569] border-b border-[#E2E8F0] pb-1">
                Recibido en Bodega (Conciliación de Stock)
              </div>
              <div class="h-16 border-b border-dashed border-[#94A3B8]"></div>
              <div class="space-y-1 text-[11px] text-[#64748B]">
                <div>Bodega: <strong class="text-[#1E293B]">{{ guide()!.destinationWarehouseName || 'Bodega Central' }}</strong></div>
                <div>Responsable: <span class="text-[#1E293B]">................................................</span></div>
                <div>Fecha y Sello: <span class="text-[#1E293B]">................................................</span></div>
              </div>
            </div>
          </div>

        </div>
      }

      @if (!loading() && !guide()) {
        <div class="p-12 text-center rounded-lg border border-dashed border-[#CBD5E1] space-y-3">
          <p class="text-sm font-medium text-[#475569]">No se ha seleccionado ninguna guía de retorno</p>
          <p class="text-xs text-[#64748B]">
            Ingresa un número de guía arriba (ej. <span class="font-mono font-semibold">GRT-XXXXXXXX</span>) o accede desde el asistente de Rutas al cerrar una ruta.
          </p>
        </div>
      }
    </div>
  `,
  styles: [`
    @media print {
      body {
        background: white !important;
        color: black !important;
      }
      .print\\:hidden {
        display: none !important;
      }
      .print\\:border-none {
        border: none !important;
      }
      .print\\:shadow-none {
        box-shadow: none !important;
      }
      .print\\:p-0 {
        padding: 0 !important;
      }
    }
  `]
})
export class GuiaRetornoPage implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private auth = inject(AuthSession);

  guide = signal<ReturnGuideDto | null>(null);
  loading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  processingReceive = signal<boolean>(false);
  reconcileSuccess = signal<ReceiveReturnGuideResponse | null>(null);
  searchQuery = '';

  canReceiveInWarehouse = signal<boolean>(false);

  ngOnInit(): void {
    const role = this.auth.role();
    this.canReceiveInWarehouse.set(role === 'Bodega' || role === 'Gerencia');

    const guideNumber = this.route.snapshot.params['guideNumber'] || this.route.snapshot.queryParams['guideNumber'];
    const planId = this.route.snapshot.queryParams['planId'];

    if (guideNumber) {
      this.searchQuery = guideNumber;
      this.loadGuide(guideNumber);
    } else if (planId) {
      this.generateFromPlan(planId);
    }
  }

  loadGuide(guideNumber: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.reconcileSuccess.set(null);

    this.api.getReturnGuide(guideNumber).subscribe({
      next: (data) => {
        this.guide.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(`No se pudo cargar la guía ${guideNumber}: ${err.message || 'Error desconocido'}`);
      }
    });
  }

  generateFromPlan(planId: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.generateReturnGuide(planId).subscribe({
      next: (data) => {
        this.guide.set(data);
        this.searchQuery = data.guideNumber;
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(`Error al generar guía para el plan: ${err.message || 'Error desconocido'}`);
      }
    });
  }

  searchGuide(): void {
    const q = this.searchQuery.trim();
    if (!q) return;
    this.router.navigate(['/guias-retorno', q]);
    this.loadGuide(q);
  }

  receiveInWarehouse(): void {
    const g = this.guide();
    if (!g) return;

    this.processingReceive.set(true);
    this.errorMessage.set(null);

    this.api.receiveReturnGuide(g.guideNumber).subscribe({
      next: (resp) => {
        this.processingReceive.set(false);
        this.reconcileSuccess.set(resp);
        this.guide.update(prev => prev ? { ...prev, status: 'ReceivedAtWarehouse' } : prev);
      },
      error: (err) => {
        this.processingReceive.set(false);
        this.errorMessage.set(`Error al recibir la guía en bodega: ${err.message || 'Error desconocido'}`);
      }
    });
  }

  printGuide(): void {
    window.print();
  }
}
