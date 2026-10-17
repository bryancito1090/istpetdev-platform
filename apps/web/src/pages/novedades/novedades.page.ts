import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthSession } from '@entities/session';
import { EstimatedBacktrackDto, FieldExceptionsApiService, FieldRequestOrderDto, ReceiptExceptionDto } from '@shared/api';
import {
  LucideRefreshCw,
  LucideTriangleAlert,
  LucideTruck,
  LucidePackage,
  LucideBoxes,
  LucideCheckCircle2,
  LucideShieldCheck,
  LucideFilter,
  LucideClock,
  LucideAlertCircle,
  LucideArrowRight,
} from '@lucide/angular';

/** Mensaje legible de un error HTTP: el detalle de Problem Details si existe. */
function errorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) return 'Sin conexión con el servidor. Revisa la red y vuelve a intentarlo.';
    const detail = (err.error as { detail?: string } | null)?.detail;
    if (detail) return detail;
    if (err.status === 403) return 'Tu cuenta no tiene permiso para esta acción.';
  }
  return fallback;
}

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pendiente',
  Planned: 'Planificado',
  Dispatched: 'Despachado',
  Delivered: 'Entregado',
  Cancelled: 'Cancelado',
};

// Etiquetas del catálogo de motivos (V2, #236). Un texto fuera del catálogo se muestra tal cual.
const REASON_LABELS: Record<string, string> = {
  Faltante: 'Faltante',
  Sobrante: 'Sobrante',
  Daño: 'Daño',
  PrecintoViolado: 'Precinto violado',
  LoteNoCoincide: 'Lote no coincide',
  CadenaDeFrio: 'Cadena de frío',
  ErrorRotulado: 'Error de rotulado',
};

const NO_REASON = 'Sin motivo registrado';

export interface RejectionReasonSummary {
  reason: string;
  label: string;
  lines: number;
  units: number;
}

/** Formatea códigos de pedido largos para que sean legibles en la interfaz de usuario. */
export function formatOrderNumber(orderNumber: string): string {
  if (!orderNumber) return '';
  if (orderNumber.length <= 24) return orderNumber;
  return `${orderNumber.slice(0, 15)}…${orderNumber.slice(-6)}`;
}

/** #236: líneas y unidades rechazadas por motivo, de mayor a menor número de líneas. */
export function summarizeRejections(receipts: ReceiptExceptionDto[]): RejectionReasonSummary[] {
  const byReason = new Map<string, RejectionReasonSummary>();
  for (const r of receipts) {
    for (const l of r.lines) {
      if (l.quantityRejected <= 0) continue;
      const reason = l.rejectionReason?.trim() || NO_REASON;
      const row = byReason.get(reason) ?? { reason, label: REASON_LABELS[reason] ?? reason, lines: 0, units: 0 };
      row.lines += 1;
      row.units += l.quantityRejected;
      byReason.set(reason, row);
    }
  }
  return [...byReason.values()].sort((a, b) => b.lines - a.lines || b.units - a.units || a.label.localeCompare(b.label));
}

/** Número con coma decimal, como se lee en Ecuador: 18,4 · 8,30. */
function decimalEs(value: number, digits: number): string {
  return value.toFixed(digits).replace('.', ',');
}

/** #237: «Reentrega estimada: 18,4 km · USD 8,30 · 0,6 h». Nunca se presenta como costo medido. */
export function formatBacktrack(b: EstimatedBacktrackDto): string {
  return `Reentrega estimada: ${decimalEs(b.km, 1)} km · USD ${decimalEs(b.costUsd, 2)} · ${decimalEs(b.hours, 1)} h`;
}

/**
 * #116 / #342: Novedades de campo para el Planificador y Gerencia.
 * - Recepciones con rechazo en entrega (ESC-06).
 * - Ajustes por discrepancia de stock en percha (ESC-09).
 * - Pedidos de reposición urgente registrados en campo (FieldRequest).
 * Interfaz intuitiva orientada al negocio con KPIs ejecutivos, filtros operativos y tarjetas estructuradas.
 */
@Component({
  selector: 'page-novedades',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideRefreshCw,
    LucideTriangleAlert,
    LucideTruck,
    LucidePackage,
    LucideBoxes,
    LucideCheckCircle2,
    LucideShieldCheck,
    LucideFilter,
    LucideClock,
    LucideAlertCircle,
    LucideArrowRight,
  ],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8 space-y-7 font-sans">
      <!-- Encabezado principal de la página -->
      <header class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E3E8EE] pb-5">
        <div>
          <div class="flex items-center gap-2.5">
            <h1 class="text-2xl font-bold text-[#1A2C44] tracking-tight">Novedades de campo</h1>
            @if (!session.canWrite()) {
              <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-[#F8FAFC] text-[#4F5B66] border border-[#E3E8EE]">
                Solo lectura
              </span>
            } @else {
              <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-emerald-50 text-emerald-800 border border-emerald-200">
                Operación activa
              </span>
            }
          </div>
          <p class="text-sm text-[#4F5B66] mt-1 max-w-3xl">
            Recepciones con rechazos o con diferencias de stock en percha, y pedidos de reposición hechos en el punto de servicio.
          </p>
        </div>
        <div class="flex items-center gap-2.5 shrink-0">
          @if (session.canWrite()) {
            <button type="button" (click)="seedSample()" [disabled]="seeding() || receiptsLoading() || ordersLoading()"
              class="h-9 inline-flex items-center gap-2 text-xs font-semibold px-3.5 rounded-lg border border-[#008CA5] bg-[#008CA5] text-white hover:bg-[#007388] transition-all shadow-2xs disabled:opacity-50 cursor-pointer">
              <svg lucideBoxes [size]="14" [class.animate-spin]="seeding()"></svg>
              <span>{{ seeding() ? 'Cargando datos…' : 'Cargar datos de prueba' }}</span>
            </button>
          }
          <button type="button" (click)="loadAll()" [disabled]="receiptsLoading() || ordersLoading() || seeding()"
            class="h-9 inline-flex items-center gap-2 text-xs font-semibold px-4 rounded-lg border border-[#E3E8EE] bg-white text-[#1A2C44] hover:bg-[#F8FAFC] hover:border-[#CBD5E1] transition-all shadow-2xs disabled:opacity-50 cursor-pointer">
            <svg lucideRefreshCw [size]="14" [class.animate-spin]="receiptsLoading() || ordersLoading()"></svg>
            Actualizar
          </button>
        </div>
      </header>

      <!-- Panel de Métricas Ejecutivas (KPIs de impacto operativo) -->
      <section class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4" aria-label="Resumen de novedades de campo">
        <!-- KPI 1: Rechazos en destino -->
        <div class="p-4 rounded-xl border border-rose-200 bg-rose-50/50 shadow-2xs flex flex-col justify-between">
          <div class="flex items-center justify-between text-xs text-rose-800 font-semibold mb-2">
            <span class="flex items-center gap-1.5">
              <svg lucideTriangleAlert [size]="15" class="text-rose-600"></svg>
              Rechazos en Destino
            </span>
            <span class="px-2 py-0.5 rounded bg-rose-100 text-rose-900 text-[10px] uppercase font-bold">
              {{ rejectionSummary().length }} motivos
            </span>
          </div>
          <div>
            <div class="text-2xl font-bold text-rose-950 font-mono">
              {{ totalRejectionsCount() }} <span class="text-xs font-sans font-medium text-rose-800">unidades</span>
            </div>
            <p class="text-[11px] text-rose-700 mt-1">
              En {{ totalRejectionsLines() }} {{ totalRejectionsLines() === 1 ? 'línea afectada' : 'líneas afectadas' }} de entrega
            </p>
          </div>
        </div>

        <!-- KPI 2: Costo de reentrega estimada -->
        <div class="p-4 rounded-xl border border-amber-200 bg-amber-50/50 shadow-2xs flex flex-col justify-between">
          <div class="flex items-center justify-between text-xs text-amber-800 font-semibold mb-2">
            <span class="flex items-center gap-1.5">
              <svg lucideTruck [size]="15" class="text-amber-600"></svg>
              Impacto de Reentrega
            </span>
            <span class="px-2 py-0.5 rounded bg-amber-100 text-amber-900 text-[10px] uppercase font-bold">Estimado</span>
          </div>
          <div>
            <div class="text-2xl font-bold text-amber-950 font-mono">
              USD {{ totalBacktrackCostUsd() | number: '1.2-2' }}
            </div>
            <p class="text-[11px] text-amber-800 mt-1">
              {{ totalBacktrackKm() | number: '1.1-1' }} km de retorno calculados
            </p>
          </div>
        </div>

        <!-- KPI 3: Ajustes de inventario en percha -->
        <div class="p-4 rounded-xl border border-sky-200 bg-sky-50/50 shadow-2xs flex flex-col justify-between">
          <div class="flex items-center justify-between text-xs text-sky-800 font-semibold mb-2">
            <span class="flex items-center gap-1.5">
              <svg lucideBoxes [size]="15" class="text-sky-600"></svg>
              Ajustes en Percha
            </span>
            <span class="px-2 py-0.5 rounded bg-sky-100 text-sky-900 text-[10px] uppercase font-bold">Auditoría</span>
          </div>
          <div>
            <div class="text-2xl font-bold text-sky-950 font-mono">
              {{ totalAdjustmentsCount() }} <span class="text-xs font-sans font-medium text-sky-800">recepciones</span>
            </div>
            <p class="text-[11px] text-sky-700 mt-1">
              Discrepancias entre stock físico y sistema
            </p>
          </div>
        </div>

        <!-- KPI 4: Pedidos de reposición en campo -->
        <div class="p-4 rounded-xl border border-emerald-200 bg-emerald-50/50 shadow-2xs flex flex-col justify-between">
          <div class="flex items-center justify-between text-xs text-emerald-800 font-semibold mb-2">
            <span class="flex items-center gap-1.5">
              <svg lucidePackage [size]="15" class="text-emerald-600"></svg>
              Pedidos en Campo
            </span>
            <span class="px-2 py-0.5 rounded bg-emerald-100 text-emerald-900 text-[10px] uppercase font-bold">Próx. Visita</span>
          </div>
          <div>
            <div class="text-2xl font-bold text-emerald-950 font-mono">
              {{ pendingOrdersCount() }} <span class="text-xs font-sans font-medium text-emerald-800">pendientes</span>
            </div>
            <p class="text-[11px] text-emerald-700 mt-1">
              {{ pendingOrdersUnits() }} unidades solicitadas por receptor
            </p>
          </div>
        </div>
      </section>

      <!-- Barra de navegación por pestañas y filtro de punto de servicio -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-3 p-1.5 bg-[#F8FAFC] border border-[#E3E8EE] rounded-xl">
        <nav class="flex flex-wrap items-center gap-1" aria-label="Filtro por tipo de novedad">
          <button type="button" (click)="activeTab.set('all')"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-all"
            [class.bg-white]="activeTab() === 'all'"
            [class.text-[#1A2C44]]="activeTab() === 'all'"
            [class.shadow-2xs]="activeTab() === 'all'"
            [class.border]="activeTab() === 'all'"
            [class.border-[#E3E8EE]]="activeTab() === 'all'"
            [class.text-[#697386]]="activeTab() !== 'all'"
            [class.hover:text-[#1A2C44]]="activeTab() !== 'all'">
            Todas las novedades
          </button>
          <button type="button" (click)="activeTab.set('rejections')"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-all inline-flex items-center gap-1.5"
            [class.bg-white]="activeTab() === 'rejections'"
            [class.text-rose-900]="activeTab() === 'rejections'"
            [class.shadow-2xs]="activeTab() === 'rejections'"
            [class.border]="activeTab() === 'rejections'"
            [class.border-rose-200]="activeTab() === 'rejections'"
            [class.text-[#697386]]="activeTab() !== 'rejections'"
            [class.hover:text-rose-900]="activeTab() !== 'rejections'">
            <span class="w-2 h-2 rounded-full bg-rose-500"></span>
            Rechazos de entrega ({{ rejectionsReceiptsCount() }})
          </button>
          <button type="button" (click)="activeTab.set('adjustments')"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-all inline-flex items-center gap-1.5"
            [class.bg-white]="activeTab() === 'adjustments'"
            [class.text-sky-900]="activeTab() === 'adjustments'"
            [class.shadow-2xs]="activeTab() === 'adjustments'"
            [class.border]="activeTab() === 'adjustments'"
            [class.border-sky-200]="activeTab() === 'adjustments'"
            [class.text-[#697386]]="activeTab() !== 'adjustments'"
            [class.hover:text-sky-900]="activeTab() !== 'adjustments'">
            <span class="w-2 h-2 rounded-full bg-sky-500"></span>
            Ajustes de percha ({{ adjustmentsReceiptsCount() }})
          </button>
          <button type="button" (click)="activeTab.set('orders')"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-all inline-flex items-center gap-1.5"
            [class.bg-white]="activeTab() === 'orders'"
            [class.text-emerald-900]="activeTab() === 'orders'"
            [class.shadow-2xs]="activeTab() === 'orders'"
            [class.border]="activeTab() === 'orders'"
            [class.border-emerald-200]="activeTab() === 'orders'"
            [class.text-[#697386]]="activeTab() !== 'orders'"
            [class.hover:text-emerald-900]="activeTab() !== 'orders'">
            <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
            Pedidos de reposición ({{ orders().length }})
          </button>
        </nav>

        @if (pointOptions().length > 0) {
          <div class="flex items-center gap-2 px-2 shrink-0">
            <svg lucideFilter [size]="14" class="text-[#697386]"></svg>
            <label class="text-xs font-medium text-[#4F5B66] flex items-center gap-1.5">
              <span>Punto:</span>
              <select class="h-8 border border-[#E3E8EE] rounded-lg px-2.5 text-xs text-[#1A2C44] bg-white focus:outline-none focus:ring-1 focus:ring-[#00A7C4] shadow-2xs"
                [ngModel]="pointFilter()" (ngModelChange)="onPointFilter($event)">
                <option value="">Todos los destinos</option>
                @for (p of pointOptions(); track p.id) {
                  <option [value]="p.id">{{ p.code }} — {{ p.name }}</option>
                }
              </select>
            </label>
          </div>
        }
      </div>

      <!-- SECCIÓN 1: Recepciones con novedades -->
      @if (activeTab() !== 'orders') {
        <section class="space-y-4" aria-labelledby="titulo-recepciones">
          <div class="flex flex-col sm:flex-row sm:items-end justify-between gap-3 border-b border-[#F1F3F5] pb-2">
            <div>
              <div class="flex items-center gap-2">
                <h2 id="titulo-recepciones" class="text-base font-bold text-[#1A2C44]">Recepciones con novedades</h2>
                <span class="text-xs px-2 py-0.5 rounded-full bg-[#F1F3F5] text-[#4F5B66] font-semibold">
                  {{ visibleReceipts().length }}
                </span>
              </div>
              <p class="text-xs text-[#697386] mt-0.5">Rechazos del receptor y ajustes entre el stock declarado en percha y el saldo del sistema.</p>
            </div>
            <p class="text-xs text-[#697386]">Mostrando {{ receipts().length }} de {{ receiptsTotal() }}.</p>
          </div>

          @if (receiptsLoading()) {
            <div class="p-8 text-center text-[#697386] text-sm bg-white border border-[#E3E8EE] rounded-xl shadow-2xs">
              <svg lucideRefreshCw [size]="20" class="animate-spin inline-block text-[#00A7C4] mb-2"></svg>
              <p>Cargando recepciones…</p>
            </div>
          } @else if (receiptsError()) {
            <div class="p-4 rounded-xl border border-rose-200 bg-rose-50 text-xs text-rose-800 flex items-center justify-between gap-3 shadow-2xs">
              <div class="flex items-center gap-2">
                <svg lucideAlertCircle [size]="16" class="text-rose-600 shrink-0"></svg>
                <span>{{ receiptsError() }}</span>
              </div>
              <button type="button" class="text-xs font-semibold underline hover:text-rose-900" (click)="loadReceipts()">Reintentar</button>
            </div>
          } @else if (receipts().length === 0) {
            <div class="p-8 text-center text-sm text-[#697386] rounded-xl border border-dashed border-[#E3E8EE] bg-[#F8FAFC] space-y-3">
              <p>No hay recepciones con rechazos ni ajustes registradas aún.</p>
              @if (session.canWrite()) {
                <div>
                  <button type="button" (click)="seedSample()" [disabled]="seeding() || receiptsLoading() || ordersLoading()"
                    class="h-8 inline-flex items-center gap-1.5 text-xs font-semibold px-3.5 rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-all shadow-2xs disabled:opacity-50 cursor-pointer">
                    <svg lucideBoxes [size]="13" [class.animate-spin]="seeding()"></svg>
                    <span>{{ seeding() ? 'Generando datos…' : 'Cargar datos de prueba' }}</span>
                  </button>
                </div>
              }
            </div>
          } @else {
            <!-- Resumen de líneas rechazadas por motivo (Requerimiento #236) -->
            @if (rejectionSummary().length > 0) {
              <div role="region" class="rounded-xl border border-rose-200 bg-white p-4 shadow-2xs space-y-2.5" aria-labelledby="titulo-resumen-motivos" data-rejection-summary>
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="p-1.5 rounded-lg bg-rose-100 text-rose-700">
                      <svg lucideTriangleAlert [size]="16"></svg>
                    </span>
                    <div>
                      <h3 id="titulo-resumen-motivos" class="text-xs font-bold text-[#1A2C44] uppercase tracking-wide">Líneas rechazadas por motivo</h3>
                      <p class="text-[11px] text-[#697386]">Calculado sobre las {{ receipts().length }} recepciones mostradas.</p>
                    </div>
                  </div>
                  <span class="text-xs font-semibold px-2 py-0.5 rounded bg-rose-50 text-rose-800 border border-rose-200">
                    {{ totalRejectionsCount() }} unidades rechazadas
                  </span>
                </div>
                <ul class="flex flex-wrap gap-2 pt-1">
                  @for (s of rejectionSummary(); track s.reason) {
                    <li class="text-xs px-3 py-1.5 rounded-lg border border-rose-200 bg-rose-50 text-rose-900 font-medium flex items-center gap-1.5" [attr.data-reason]="s.reason">
                      <span class="w-1.5 h-1.5 rounded-full bg-rose-600"></span>
                      <span class="font-semibold">{{ s.label }}</span>:
                      {{ s.lines }} {{ s.lines === 1 ? 'línea' : 'líneas' }} · {{ s.units }} {{ s.units === 1 ? 'unidad' : 'unidades' }}
                    </li>
                  }
                </ul>
              </div>
            }

            <!-- Tarjetas de cada recepción con novedad -->
            <div class="space-y-4">
              @for (r of visibleReceipts(); track r.receiptId) {
                <article class="rounded-xl border border-[#E3E8EE] bg-white p-5 shadow-2xs space-y-4 transition-all hover:border-[#CBD5E1]" [attr.data-receipt]="r.receiptId">
                  <!-- Cabecera de la tarjeta -->
                  <div class="flex flex-wrap items-center gap-3 justify-between border-b border-[#F1F3F5] pb-3">
                    <div class="space-y-0.5">
                      <div class="flex items-center gap-2">
                        <span class="font-mono text-xs font-bold px-2 py-0.5 rounded bg-[#E6F7FA] text-[#008CA5] border border-[#C1EDF3]">
                          {{ r.servicePointCode }}
                        </span>
                        <h3 class="text-sm font-bold text-[#1A2C44]">{{ r.servicePointCode }} — {{ r.servicePointName }}</h3>
                      </div>
                      <p class="text-xs text-[#697386]">
                        Entrega <span class="font-mono font-semibold text-[#1A2C44]">{{ r.deliveryCode }}</span> · registrada por <span class="font-medium text-[#2D3748]">{{ r.actorName || 'cuenta desconocida' }}</span> ·
                        servidor {{ r.receivedAtServer | date: 'dd/MM/yyyy HH:mm' }}
                        @if (r.occurredAtDevice) { · dispositivo {{ r.occurredAtDevice | date: 'dd/MM/yyyy HH:mm' }} }
                      </p>
                    </div>
                    <div class="flex gap-2">
                      @if (r.hasRejections) {
                        <span class="text-xs font-semibold px-2.5 py-1 rounded-md bg-rose-50 text-rose-700 border border-rose-200 flex items-center gap-1">
                          <svg lucideTriangleAlert [size]="13"></svg>
                          Rechazo
                        </span>
                      }
                      @if (r.hasAdjustments) {
                        <span class="text-xs font-semibold px-2.5 py-1 rounded-md bg-[#FFF4E5] text-[#B76E00] border border-[#FFE2B5] flex items-center gap-1">
                          <svg lucideBoxes [size]="13"></svg>
                          Ajuste de percha
                        </span>
                      }
                    </div>
                  </div>

                  <!-- Estimación de impacto logístico de reentrega (#237) -->
                  @if (r.estimatedBacktrack; as b) {
                    <div class="rounded-lg border border-amber-200 bg-amber-50/60 p-3 flex items-start gap-3">
                      <div class="p-1.5 rounded-md bg-amber-100 text-amber-700 shrink-0 mt-0.5">
                        <svg lucideTruck [size]="16"></svg>
                      </div>
                      <div class="min-w-0 flex-1">
                        <div class="flex items-center gap-2">
                          <span class="text-xs font-bold text-[#1A2C44]">Sobrecosto estimado de reentrega</span>
                          <span class="text-[10px] px-1.5 py-0.2 rounded bg-amber-200/60 text-amber-900 font-semibold">Logística inversa</span>
                        </div>
                        <p class="text-xs text-[#2D3748] font-medium mt-0.5" data-backtrack
                          title="Viaje de ida y vuelta bodega de origen → punto para reponer lo rechazado. No se suma al costo de la ruta original.">
                          {{ backtrackLabel(b) }}
                          @if (b.isApproximate) {
                            <span class="text-[#B76E00] font-normal">(aproximado: distancia en línea recta)</span>
                          }
                        </p>
                        <p class="text-[11px] text-[#697386] mt-0.5">
                          Costeo de viaje especial de reposición origen ↔ destino; no altera la métrica del despacho primario.
                        </p>
                      </div>
                    </div>
                  }

                  <!-- Tabla de detalle de líneas, rechazos y ajustes en percha -->
                  <div class="overflow-x-auto border border-[#E3E8EE] rounded-lg">
                    <table class="w-full text-xs">
                      <thead class="bg-[#F8FAFC] text-[10px] text-[#697386] uppercase border-b border-[#E3E8EE] font-mono font-semibold text-left">
                        <tr>
                          <th class="py-2.5 px-3">Producto / lote</th>
                          <th class="py-2.5 px-3 text-right">Aceptado</th>
                          <th class="py-2.5 px-3 text-right">Rechazado</th>
                          <th class="py-2.5 px-3">Motivo</th>
                          <th class="py-2.5 px-3 text-right">Percha declarada</th>
                          <th class="py-2.5 px-3">Ajuste de stock</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-[#F1F3F5] text-[#2D3748]">
                        @for (l of r.lines; track l.receiptLineId) {
                          <tr class="hover:bg-[#F8FAFC]/50 transition-colors">
                            <td class="py-2.5 px-3 font-medium text-[#1A2C44]">
                              <span class="font-bold">{{ l.sku }}</span> {{ l.productName }}
                              <span class="text-[#697386] font-mono font-normal">({{ l.lotCode }})</span>
                            </td>
                            <td class="py-2.5 px-3 text-right font-mono font-semibold text-[#00875A]">
                              {{ l.quantityAccepted }}
                            </td>
                            <td class="py-2.5 px-3 text-right font-mono font-bold" [class.text-rose-600]="l.quantityRejected > 0">
                              {{ l.quantityRejected }}
                            </td>
                            <td class="py-2.5 px-3">
                              @if (l.quantityRejected > 0) {
                                <span class="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded bg-rose-50 text-rose-800 border border-rose-200">
                                  {{ reasonLabel(l.rejectionReason) }}
                                </span>
                              } @else {
                                <span class="text-[#8792A2]">—</span>
                              }
                            </td>
                            <td class="py-2.5 px-3 text-right font-mono font-medium">
                              {{ l.remainingDeclared ?? '—' }}
                            </td>
                            <td class="py-2.5 px-3" [title]="l.adjustmentReason ?? ''">
                              @if (l.adjustmentQuantity != null) {
                                <div class="inline-flex flex-col">
                                  <span [class.text-[#B76E00]]="l.adjustmentQuantity < 0" [class.text-sky-800]="l.adjustmentQuantity > 0" class="font-medium">
                                    {{ l.adjustmentQuantity > 0 ? '+' : '' }}{{ l.adjustmentQuantity }}
                                    {{ l.adjustmentQuantity < 0 ? '(consumo derivado)' : '(conteo mayor que el saldo)' }}
                                  </span>
                                  <span class="text-[10px] text-[#697386]">
                                    {{ l.adjustmentQuantity < 0 ? 'Faltante: consumo real en punto' : 'Sobrante: mayor stock físico en estante' }}
                                  </span>
                                </div>
                              } @else {
                                <span class="text-[#8792A2]">—</span>
                              }
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>

                  <!-- Bloque de Evidencia de Campo -->
                  <div class="text-xs text-[#4F5B66] pt-1 border-t border-[#F1F3F5]">
                    <div class="flex items-center justify-between">
                      <p class="font-semibold text-[#1A2C44] flex items-center gap-1.5">
                        <svg lucideShieldCheck [size]="15" class="text-emerald-600"></svg>
                        Evidencia
                      </p>
                      @if (r.evidence.length > 0) {
                        <span class="text-[10px] text-[#697386] font-medium">Almacenamiento inmutable en Amazon S3</span>
                      }
                    </div>
                    @if (r.evidence.length === 0) {
                      <p class="text-[#697386] mt-1 text-xs">Sin evidencia registrada.</p>
                    } @else {
                      <ul class="mt-2 space-y-1.5">
                        @for (e of r.evidence; track e.evidenceId) {
                          <li class="flex flex-wrap items-center gap-2 p-2 rounded-lg bg-[#F8FAFC] border border-[#E3E8EE]">
                            <span class="font-semibold capitalize text-[#1A2C44] px-1.5 py-0.5 rounded bg-white border border-[#E3E8EE] text-[11px]">
                              {{ e.kind === 'photo' ? 'Foto' : e.kind === 'signature' ? 'Firma' : e.kind }}:
                            </span>
                            @if (e.status === 'Confirmed') {
                              <span class="text-[#00875A] font-semibold flex items-center gap-1 text-xs">
                                <svg lucideCheckCircle2 [size]="13"></svg>
                                ✓ Confirmada en almacenamiento S3
                              </span>
                            } @else {
                              <span class="text-[#B76E00] font-medium flex items-center gap-1 text-xs">
                                <svg lucideClock [size]="13"></svg>
                                ⏳ Pendiente de subida
                              </span>
                            }
                            @if (e.byteSize) {
                              <span class="text-[#697386] text-[11px]">· {{ e.byteSize }} bytes</span>
                            }
                            @if (e.sha256) {
                              <span class="font-mono text-[11px] text-[#697386] bg-white px-1 py-0.5 rounded border border-[#E3E8EE]" [title]="e.sha256">
                                SHA-256: {{ e.sha256.slice(0, 12) }}…
                              </span>
                            }
                          </li>
                        }
                      </ul>
                      <p class="text-[#697386] mt-1.5 text-[11px]">Archivos privados en S3 (cifrados con integridad SHA-256 verificada por el servidor).</p>
                    }
                  </div>
                </article>
              }
            </div>
          }
        </section>
      }

      <!-- SECCIÓN 2: Pedidos de reposición en campo -->
      @if (activeTab() === 'all' || activeTab() === 'orders') {
        <section class="space-y-4 pt-4 border-t border-[#E3E8EE]" aria-labelledby="titulo-pedidos">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <div class="flex items-center gap-2">
                <h2 id="titulo-pedidos" class="text-base font-bold text-[#1A2C44]">Pedidos de reposición en campo</h2>
                <span class="text-xs px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-semibold">
                  {{ orders().length }} pedidos
                </span>
              </div>
              <p class="text-xs text-[#697386] mt-0.5">Requerimientos registrados por el receptor para la próxima visita. Se planifican desde Rutas.</p>
            </div>
            <a href="/rutas" class="text-xs font-semibold text-[#00A7C4] hover:text-[#008CA5] inline-flex items-center gap-1 transition-colors">
              Ir a Planificación de Rutas
              <svg lucideArrowRight [size]="14"></svg>
            </a>
          </div>

          @if (actionMessage()) {
            <div class="p-3.5 rounded-xl border text-xs font-medium flex items-center justify-between gap-3"
              [class.border-rose-200]="actionIsError()" [class.bg-rose-50]="actionIsError()" [class.text-rose-900]="actionIsError()"
              [class.border-emerald-200]="!actionIsError()" [class.bg-emerald-50]="!actionIsError()" [class.text-emerald-900]="!actionIsError()">
              <div class="flex items-center gap-2">
                @if (actionIsError()) {
                  <svg lucideAlertCircle [size]="16" class="text-rose-600 shrink-0"></svg>
                } @else {
                  <svg lucideCheckCircle2 [size]="16" class="text-emerald-600 shrink-0"></svg>
                }
                <span>{{ actionMessage() }}</span>
              </div>
              <button type="button" class="text-xs font-bold hover:underline" (click)="actionMessage.set(null)">Cerrar</button>
            </div>
          }

          @if (ordersLoading()) {
            <div class="p-8 text-center bg-white rounded-xl border border-[#E3E8EE] text-[#697386] text-xs shadow-2xs">
              <svg lucideRefreshCw [size]="20" class="animate-spin inline-block text-[#00A7C4] mb-2"></svg>
              <p>Cargando pedidos…</p>
            </div>
          } @else if (ordersError()) {
            <div class="p-4 rounded-xl border border-rose-200 bg-rose-50 text-xs text-rose-800 flex items-center justify-between gap-3 shadow-2xs">
              <div class="flex items-center gap-2">
                <svg lucideAlertCircle [size]="16" class="text-rose-600 shrink-0"></svg>
                <span>{{ ordersError() }}</span>
              </div>
              <button type="button" class="text-xs font-semibold underline hover:text-rose-900" (click)="loadOrders()">Reintentar</button>
            </div>
          } @else if (orders().length === 0) {
            <div class="p-8 text-center bg-white rounded-xl border border-dashed border-[#E3E8EE] text-xs text-[#697386]">
              No hay pedidos de reposición hechos en campo.
            </div>
          } @else {
            <div class="overflow-x-auto rounded-xl border border-[#E3E8EE] bg-white shadow-2xs">
              <table class="w-full text-xs text-left border-collapse">
                <thead class="text-[11px] font-semibold uppercase tracking-wider text-[#697386] bg-[#F8FAFC] border-b border-[#E3E8EE]">
                  <tr>
                    <th class="py-3 px-3.5">Pedido</th>
                    <th class="py-3 px-3.5">Destino</th>
                    <th class="py-3 px-3.5">Insumos Solicitados</th>
                    <th class="py-3 px-3.5">Fecha Requerida</th>
                    <th class="py-3 px-3.5">Estado</th>
                    @if (session.canWrite()) { <th class="py-3 px-3.5 text-right">Acción</th> }
                  </tr>
                </thead>
                <tbody class="divide-y divide-[#E3E8EE]">
                  @for (o of visibleOrders(); track o.id) {
                    <tr class="hover:bg-[#F8FAFC]/70 transition-colors" [attr.data-order]="o.id">
                      <!-- Número de pedido estilizado con tooltip y accesibilidad -->
                      <td class="py-3 px-3.5">
                        <div class="flex flex-col">
                          <span class="font-mono text-xs font-bold text-[#1A2C44]" [title]="o.orderNumber">
                            {{ formatOrderNumber(o.orderNumber) }}
                          </span>
                          <span class="sr-only">{{ o.orderNumber }}</span>
                          <span class="text-[10px] text-[#697386]">Solicitud en entrega</span>
                        </div>
                      </td>
                      <!-- Destino con código destacado -->
                      <td class="py-3 px-3.5 text-[#2D3748]">
                        <span class="font-mono font-bold px-1.5 py-0.5 rounded bg-[#F1F3F5] text-[#1A2C44] mr-1 text-[11px]">
                          {{ o.destinationCode }}
                        </span>
                        <span class="font-medium text-[#1A2C44]">{{ o.destinationName }}</span>
                      </td>
                      <!-- Insumos en chips limpios -->
                      <td class="py-3 px-3.5">
                        <div class="flex flex-wrap gap-1.5">
                          @for (l of o.lines; track l.id) {
                            <div class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md bg-[#F1F3F5] text-xs">
                              <span class="font-mono text-[11px] font-semibold text-[#008CA5]">{{ l.sku }}</span>
                              <span class="font-medium text-[#1A2C44]">{{ l.productName }}:</span>
                              <span class="font-mono font-bold text-[#1A2C44]">{{ l.requestedQuantity }}</span>
                            </div>
                          }
                        </div>
                      </td>
                      <!-- Fecha requerida -->
                      <td class="py-3 px-3.5 font-mono text-[#697386] whitespace-nowrap">
                        <div class="flex items-center gap-1.5">
                          <svg lucideClock [size]="13" class="text-[#8792A2]"></svg>
                          <span>{{ o.requiredDateUtc | date: 'dd/MM/yyyy' }}</span>
                        </div>
                      </td>
                      <!-- Estado operativo -->
                      <td class="py-3 px-3.5">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold border"
                          [class.bg-[#F8FAFC]]="o.status === 'Pending'"
                          [class.text-[#4F5B66]]="o.status === 'Pending'"
                          [class.border-[#CBD5E1]]="o.status === 'Pending'"
                          [class.bg-emerald-50]="o.status === 'Delivered' || o.status === 'Planned'"
                          [class.text-emerald-800]="o.status === 'Delivered' || o.status === 'Planned'"
                          [class.border-emerald-200]="o.status === 'Delivered' || o.status === 'Planned'"
                          [class.bg-rose-50]="o.status === 'Cancelled'"
                          [class.text-rose-800]="o.status === 'Cancelled'"
                          [class.border-rose-200]="o.status === 'Cancelled'">
                          {{ statusLabel(o.status) }}
                        </span>
                      </td>
                      <!-- Acciones con confirmación en dos pasos -->
                      @if (session.canWrite()) {
                        <td class="py-3 px-3.5 text-right whitespace-nowrap">
                          @if (o.status === 'Pending') {
                            @if (confirmingId() === o.id) {
                              <div class="inline-flex items-center gap-2 justify-end">
                                <button type="button" class="text-xs font-bold text-rose-600 hover:text-rose-800 transition-colors" [disabled]="cancellingId() === o.id"
                                  (click)="cancel(o)">{{ cancellingId() === o.id ? 'Cancelando…' : 'Confirmar cancelación' }}</button>
                                <button type="button" class="text-xs text-[#697386] hover:text-[#1A2C44] transition-colors" [disabled]="cancellingId() === o.id"
                                  (click)="confirmingId.set(null)">No</button>
                              </div>
                            } @else {
                              <button type="button" class="text-xs font-semibold text-[#697386] hover:text-rose-600 hover:underline transition-colors" [disabled]="cancellingId() !== null"
                                (click)="confirmingId.set(o.id)">Cancelar pedido</button>
                            }
                          } @else {
                            <span class="text-[#8792A2]">—</span>
                          }
                        </td>
                      }
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </section>
      }
    </div>
  `,
})
export class NovedadesPage implements OnInit {
  readonly session = inject(AuthSession);
  private readonly api = inject(FieldExceptionsApiService);

  readonly receipts = signal<ReceiptExceptionDto[]>([]);
  readonly receiptsTotal = signal(0);
  readonly receiptsLoading = signal(false);
  readonly receiptsError = signal<string | null>(null);
  readonly pointFilter = signal('');
  // Puntos vistos en la última carga sin filtro, para no perder opciones al filtrar.
  private readonly knownPoints = signal<Array<{ id: string; code: string; name: string }>>([]);
  readonly pointOptions = computed(() => this.knownPoints());
  readonly rejectionSummary = computed(() => summarizeRejections(this.receipts()));

  readonly orders = signal<FieldRequestOrderDto[]>([]);
  readonly ordersLoading = signal(false);
  readonly ordersError = signal<string | null>(null);
  readonly confirmingId = signal<string | null>(null);
  readonly cancellingId = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);
  readonly actionIsError = signal(false);
  readonly seeding = signal(false);

  // Control de pestaña activa para navegación fluida e intuitiva
  readonly activeTab = signal<'all' | 'rejections' | 'adjustments' | 'orders'>('all');

  // Métricas agregadas ejecutivas para el panel superior
  readonly totalRejectionsCount = computed(() => {
    return this.receipts().reduce((acc, r) => acc + r.lines.reduce((lAcc, l) => lAcc + (l.quantityRejected > 0 ? l.quantityRejected : 0), 0), 0);
  });

  readonly totalRejectionsLines = computed(() => {
    return this.receipts().reduce((acc, r) => acc + r.lines.filter(l => l.quantityRejected > 0).length, 0);
  });

  readonly totalBacktrackCostUsd = computed(() => {
    return this.receipts().reduce((acc, r) => acc + (r.estimatedBacktrack?.costUsd ?? 0), 0);
  });

  readonly totalBacktrackKm = computed(() => {
    return this.receipts().reduce((acc, r) => acc + (r.estimatedBacktrack?.km ?? 0), 0);
  });

  readonly totalAdjustmentsCount = computed(() => {
    return this.receipts().filter(r => r.hasAdjustments).length;
  });

  readonly pendingOrdersCount = computed(() => {
    return this.orders().filter(o => o.status === 'Pending').length;
  });

  readonly pendingOrdersUnits = computed(() => {
    return this.orders()
      .filter(o => o.status === 'Pending')
      .reduce((acc, o) => acc + o.lines.reduce((lAcc, l) => lAcc + (l.requestedQuantity ?? 0), 0), 0);
  });

  // Conteo de recepciones con filtros aplicados para las pestañas
  readonly rejectionsReceiptsCount = computed(() => this.receipts().filter(r => r.hasRejections).length);
  readonly adjustmentsReceiptsCount = computed(() => this.receipts().filter(r => r.hasAdjustments).length);

  // Vistas filtradas según la pestaña activa
  readonly visibleReceipts = computed(() => {
    const tab = this.activeTab();
    if (tab === 'rejections') return this.receipts().filter(r => r.hasRejections);
    if (tab === 'adjustments') return this.receipts().filter(r => r.hasAdjustments);
    return this.receipts();
  });

  readonly visibleOrders = computed(() => {
    return this.orders();
  });

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loadReceipts();
    this.loadOrders();
  }

  loadReceipts(): void {
    this.receiptsLoading.set(true);
    this.receiptsError.set(null);
    const filter = this.pointFilter() || undefined;
    this.api.listReceiptExceptions(filter).subscribe({
      next: res => {
        this.receipts.set(res.items);
        this.receiptsTotal.set(res.total);
        if (!filter) {
          const seen = new Map<string, { id: string; code: string; name: string }>();
          for (const r of res.items) seen.set(r.servicePointId, { id: r.servicePointId, code: r.servicePointCode, name: r.servicePointName });
          this.knownPoints.set([...seen.values()]);
        }
        this.receiptsLoading.set(false);
      },
      error: err => {
        // Se conserva la última lista conocida fuera de pantalla; no se muestra como vacía.
        this.receiptsError.set(errorMessage(err, 'No se pudieron cargar las recepciones con novedades.'));
        this.receiptsLoading.set(false);
      },
    });
  }

  onPointFilter(value: string): void {
    this.pointFilter.set(value);
    this.loadReceipts();
  }

  loadOrders(): void {
    this.ordersLoading.set(true);
    this.ordersError.set(null);
    this.api.listFieldRequests().subscribe({
      next: orders => {
        this.orders.set(orders);
        this.ordersLoading.set(false);
      },
      error: err => {
        this.ordersError.set(errorMessage(err, 'No se pudieron cargar los pedidos de reposición.'));
        this.ordersLoading.set(false);
      },
    });
  }

  backtrackLabel(b: EstimatedBacktrackDto): string {
    return formatBacktrack(b);
  }

  reasonLabel(reason: string | null | undefined): string {
    const trimmed = reason?.trim();
    return trimmed ? (REASON_LABELS[trimmed] ?? trimmed) : NO_REASON;
  }

  statusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  formatOrderNumber(num: string): string {
    return formatOrderNumber(num);
  }

  cancel(order: FieldRequestOrderDto): void {
    // Un solo envío a la vez: evita doble clic y cancelaciones cruzadas.
    if (this.cancellingId() !== null) return;
    this.cancellingId.set(order.id);
    this.actionMessage.set(null);
    this.api.cancelOrder(order.id, order.version).subscribe({
      next: () => {
        this.cancellingId.set(null);
        this.confirmingId.set(null);
        this.actionIsError.set(false);
        this.actionMessage.set(`Pedido ${order.orderNumber} cancelado.`);
        this.loadOrders();
      },
      error: err => {
        this.cancellingId.set(null);
        this.confirmingId.set(null);
        this.actionIsError.set(true);
        this.actionMessage.set(errorMessage(err, `No se pudo cancelar el pedido ${order.orderNumber}.`));
        // 409: el pedido cambió (planificado por otro usuario o versión vieja); se muestra el estado real.
        if (err instanceof HttpErrorResponse && err.status === 409) this.loadOrders();
      },
    });
  }

  seedSample(): void {
    if (this.seeding()) return;
    this.seeding.set(true);
    this.actionMessage.set(null);
    this.api.seedFieldActivity().subscribe({
      next: () => {
        this.seeding.set(false);
        this.actionIsError.set(false);
        this.actionMessage.set('Muestra operativa de campo cargada exitosamente.');
        this.loadAll();
      },
      error: err => {
        this.seeding.set(false);
        this.actionIsError.set(true);
        this.actionMessage.set(errorMessage(err, 'No se pudo cargar la muestra operativa.'));
      },
    });
  }
}
