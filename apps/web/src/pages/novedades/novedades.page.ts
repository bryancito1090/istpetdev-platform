import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthSession } from '@entities/session';
import { EstimatedBacktrackDto, FieldExceptionsApiService, FieldRequestOrderDto, ReceiptExceptionDto } from '@shared/api';

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
 * #116: novedades de campo para el Planificador.
 * - Recepciones con rechazo (ESC-06) o con ajuste por stock en percha (ESC-09).
 * - Pedidos de reposición hechos en campo (FieldRequest).
 * Gerencia y Planificador pueden cancelar un pedido pendiente; el Auditor solo lee (FR-21).
 */
@Component({
  selector: 'page-novedades',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8 space-y-8">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E3E8EE] pb-5">
        <div>
          <div class="flex items-center gap-2.5">
            <h1 class="text-2xl font-bold text-[#1A2C44] tracking-tight">Novedades de campo</h1>
            @if (!session.canWrite()) {
              <span class="text-xs font-medium px-2.5 py-0.5 rounded-full bg-[#F8FAFC] text-[#4F5B66] border border-[#E3E8EE]">
                Solo lectura
              </span>
            }
          </div>
          <p class="text-sm text-[#4F5B66] mt-1">
            Recepciones con rechazos o con diferencias de stock en percha, y pedidos de reposición hechos en el punto de servicio.
          </p>
        </div>
        <button type="button" (click)="loadAll()" [disabled]="receiptsLoading() || ordersLoading()"
          class="h-9 inline-flex items-center text-xs font-medium px-3.5 rounded-lg border border-[#E3E8EE] bg-white text-[#2D3748] hover:bg-[#F8FAFC] transition-colors shadow-2xs disabled:opacity-50">
          Actualizar
        </button>
      </div>

      <!-- Recepciones con novedades -->
      <section class="space-y-4" aria-labelledby="titulo-recepciones">
        <div class="flex flex-col sm:flex-row sm:items-end justify-between gap-3">
          <div>
            <h2 id="titulo-recepciones" class="text-base font-bold text-[#1A2C44]">Recepciones con novedades</h2>
            <p class="text-xs text-[#697386]">Rechazos del receptor y ajustes entre el stock declarado en percha y el saldo del sistema.</p>
          </div>
          @if (pointOptions().length > 0) {
            <label class="text-xs text-[#4F5B66] flex items-center gap-2">
              <span>Punto</span>
              <select class="h-9 border border-[#E3E8EE] rounded-lg px-2.5 text-xs text-[#2D3748] bg-white focus:outline-none focus:ring-1 focus:ring-[#00A7C4]" [ngModel]="pointFilter()" (ngModelChange)="onPointFilter($event)">
                <option value="">Todos</option>
                @for (p of pointOptions(); track p.id) {
                  <option [value]="p.id">{{ p.code }} — {{ p.name }}</option>
                }
              </select>
            </label>
          }
        </div>

        @if (receiptsLoading()) {
          <div class="p-8 text-center text-[#697386] text-sm bg-white border border-[#E3E8EE] rounded-xl shadow-2xs">
            Cargando recepciones…
          </div>
        } @else if (receiptsError()) {
          <div class="p-4 rounded-xl border border-rose-200 bg-rose-50 text-xs text-rose-800 flex items-center justify-between gap-3 shadow-2xs">
            <span>{{ receiptsError() }}</span>
            <button type="button" class="text-xs font-semibold underline hover:text-rose-900" (click)="loadReceipts()">Reintentar</button>
          </div>
        } @else if (receipts().length === 0) {
          <div class="p-8 text-center text-sm text-[#697386] rounded-xl border border-dashed border-[#E3E8EE] bg-[#F8FAFC]">
            No hay recepciones con rechazos ni ajustes.
          </div>
        } @else {
          <p class="text-xs text-[#697386]">Mostrando {{ receipts().length }} de {{ receiptsTotal() }}.</p>
          @if (rejectionSummary().length > 0) {
            <div role="region" class="rounded-xl border border-[#E3E8EE] bg-white p-4 shadow-2xs space-y-2" aria-labelledby="titulo-resumen-motivos" data-rejection-summary>
              <h3 id="titulo-resumen-motivos" class="text-xs font-bold text-[#1A2C44] uppercase tracking-wide">Líneas rechazadas por motivo</h3>
              <p class="text-[11px] text-[#697386]">Calculado sobre las {{ receipts().length }} recepciones mostradas.</p>
              <ul class="mt-2 flex flex-wrap gap-2">
                @for (s of rejectionSummary(); track s.reason) {
                  <li class="text-xs px-2.5 py-1 rounded-lg border border-rose-200 bg-rose-50 text-rose-800 font-medium" [attr.data-reason]="s.reason">
                    <span class="font-semibold">{{ s.label }}</span>:
                    {{ s.lines }} {{ s.lines === 1 ? 'línea' : 'líneas' }} · {{ s.units }} {{ s.units === 1 ? 'unidad' : 'unidades' }}
                  </li>
                }
              </ul>
            </div>
          }
          @for (r of receipts(); track r.receiptId) {
            <article class="rounded-xl border border-[#E3E8EE] bg-white p-4 shadow-2xs space-y-3" [attr.data-receipt]="r.receiptId">
              <div class="flex flex-wrap items-center gap-2 justify-between border-b border-[#F1F3F5] pb-3">
                <div>
                  <p class="text-sm font-bold text-[#1A2C44]">{{ r.servicePointCode }} — {{ r.servicePointName }}</p>
                  <p class="text-xs text-[#697386] mt-0.5">
                    Entrega {{ r.deliveryCode }} · registrada por {{ r.actorName || 'cuenta desconocida' }} ·
                    servidor {{ r.receivedAtServer | date: 'dd/MM/yyyy HH:mm' }}
                    @if (r.occurredAtDevice) { · dispositivo {{ r.occurredAtDevice | date: 'dd/MM/yyyy HH:mm' }} }
                  </p>
                </div>
                <div class="flex gap-2">
                  @if (r.hasRejections) {
                    <span class="text-[10px] font-semibold px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-200">Rechazo</span>
                  }
                  @if (r.hasAdjustments) {
                    <span class="text-[10px] font-semibold px-2 py-0.5 rounded bg-[#FFF4E5] text-[#B76E00] border border-[#FFE2B5]">Ajuste de percha</span>
                  }
                </div>
              </div>

              @if (r.estimatedBacktrack; as b) {
                <p class="text-xs text-[#2D3748] font-medium" data-backtrack
                  title="Viaje de ida y vuelta bodega de origen → punto para reponer lo rechazado. No se suma al costo de la ruta original.">
                  {{ backtrackLabel(b) }}
                  @if (b.isApproximate) {
                    <span class="text-[#B76E00] font-normal">(aproximado: distancia en línea recta)</span>
                  }
                </p>
              }

              <div class="overflow-x-auto border border-[#E3E8EE] rounded-lg">
                <table class="w-full text-xs">
                  <thead class="bg-[#F8FAFC] text-[10px] text-[#697386] uppercase border-b border-[#E3E8EE] font-mono font-semibold text-left">
                    <tr>
                      <th class="py-2 px-3">Producto / lote</th>
                      <th class="py-2 px-3 text-right">Aceptado</th>
                      <th class="py-2 px-3 text-right">Rechazado</th>
                      <th class="py-2 px-3">Motivo</th>
                      <th class="py-2 px-3 text-right">Percha declarada</th>
                      <th class="py-2 px-3">Ajuste</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-[#F1F3F5] text-[#2D3748]">
                    @for (l of r.lines; track l.receiptLineId) {
                      <tr class="hover:bg-[#F8FAFC]/50">
                        <td class="py-2 px-3 font-medium text-[#1A2C44]">{{ l.sku }} {{ l.productName }} <span class="text-[#697386] font-mono font-normal">({{ l.lotCode }})</span></td>
                        <td class="py-2 px-3 text-right font-mono font-semibold text-[#00875A]">{{ l.quantityAccepted }}</td>
                        <td class="py-2 px-3 text-right font-mono font-bold" [class.text-rose-600]="l.quantityRejected > 0">{{ l.quantityRejected }}</td>
                        <td class="py-2 px-3">{{ l.quantityRejected > 0 ? reasonLabel(l.rejectionReason) : '—' }}</td>
                        <td class="py-2 px-3 text-right font-mono">{{ l.remainingDeclared ?? '—' }}</td>
                        <td class="py-2 px-3" [title]="l.adjustmentReason ?? ''">
                          @if (l.adjustmentQuantity != null) {
                            <span [class.text-[#B76E00]]="l.adjustmentQuantity < 0" [class.text-sky-800]="l.adjustmentQuantity > 0" class="font-medium">
                              {{ l.adjustmentQuantity > 0 ? '+' : '' }}{{ l.adjustmentQuantity }}
                              {{ l.adjustmentQuantity < 0 ? '(consumo derivado)' : '(conteo mayor que el saldo)' }}
                            </span>
                          } @else { — }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>

              <div class="text-xs text-[#4F5B66] pt-1">
                <p class="font-semibold text-[#1A2C44]">Evidencia</p>
                @if (r.evidence.length === 0) {
                  <p class="text-[#697386]">Sin evidencia registrada.</p>
                } @else {
                  <ul class="list-disc pl-5 mt-1 space-y-0.5">
                    @for (e of r.evidence; track e.evidenceId) {
                      <li class="flex items-center gap-1.5 py-0.5">
                        <span class="font-semibold capitalize text-[#1A2C44]">{{ e.kind === 'photo' ? 'Foto' : e.kind === 'signature' ? 'Firma' : e.kind }}:</span>
                        @if (e.status === 'Confirmed') {
                          <span class="text-[#00875A] font-semibold">✓ Confirmada en almacenamiento S3</span>
                        } @else {
                          <span class="text-[#B76E00] font-medium">⏳ Pendiente de subida</span>
                        }
                        @if (e.byteSize) { · <span class="text-[#697386]">{{ e.byteSize }} bytes</span> }
                        @if (e.sha256) { · <span class="font-mono text-[#697386]" [title]="e.sha256">SHA-256: {{ e.sha256.slice(0, 12) }}…</span> }
                      </li>
                    }
                  </ul>
                  <p class="text-[#697386] mt-1 text-[11px]">Archivos privados en S3 (cifrados con integridad SHA-256 verificada por el servidor).</p>
                }
              </div>
            </article>
          }
        }
      </section>

      <!-- Pedidos de reposición en campo -->
      <section class="space-y-4" aria-labelledby="titulo-pedidos">
        <div class="flex items-center justify-between">
          <div>
            <h2 id="titulo-pedidos" class="text-base font-semibold text-[#1A2C44]">Pedidos de reposición en campo</h2>
            <p class="text-xs text-[#697386]">Requerimientos registrados por el receptor para la próxima visita. Se planifican desde Rutas.</p>
          </div>
        </div>

        @if (actionMessage()) {
          <div class="p-3.5 rounded-md border text-xs font-medium"
            [class.border-[#DF1B41]/30]="actionIsError()" [class.bg-[#DF1B41]/10]="actionIsError()" [class.text-[#DF1B41]]="actionIsError()"
            [class.border-[#00D924]/30]="!actionIsError()" [class.bg-[#00D924]/10]="!actionIsError()" [class.text-[#0E6245]]="!actionIsError()">
            {{ actionMessage() }}
          </div>
        }

        @if (ordersLoading()) {
          <div class="p-8 text-center bg-white rounded-md border border-[#E3E8EE] text-[#697386] text-xs">
            Cargando pedidos…
          </div>
        } @else if (ordersError()) {
          <div class="p-4 rounded-md border border-[#DF1B41]/30 bg-[#DF1B41]/10 text-xs text-[#DF1B41] flex items-center justify-between gap-3">
            <span>{{ ordersError() }}</span>
            <button type="button" class="text-xs font-medium underline hover:text-[#DF1B41]/80 transition-colors" (click)="loadOrders()">Reintentar</button>
          </div>
        } @else if (orders().length === 0) {
          <div class="p-8 text-center bg-white rounded-md border border-dashed border-[#E3E8EE] text-xs text-[#697386]">
            No hay pedidos de reposición hechos en campo.
          </div>
        } @else {
          <div class="overflow-x-auto rounded-md border border-[#E3E8EE] bg-white shadow-sm">
            <table class="w-full text-xs text-left border-collapse">
              <thead class="text-[11px] font-semibold uppercase tracking-wider text-[#697386] bg-[#F8FAFC] border-b border-[#E3E8EE]">
                <tr>
                  <th class="py-2.5 px-3.5">Pedido</th>
                  <th class="py-2.5 px-3.5">Destino</th>
                  <th class="py-2.5 px-3.5">Productos</th>
                  <th class="py-2.5 px-3.5">Requerido</th>
                  <th class="py-2.5 px-3.5">Estado</th>
                  @if (session.canWrite()) { <th class="py-2.5 px-3.5 text-right">Acción</th> }
                </tr>
              </thead>
              <tbody class="divide-y divide-[#E3E8EE]">
                @for (o of orders(); track o.id) {
                  <tr class="hover:bg-[#F8FAFC]/60 transition-colors" [attr.data-order]="o.id">
                    <td class="py-2.5 px-3.5 font-mono text-[#1A2C44] font-medium">{{ o.orderNumber }}</td>
                    <td class="py-2.5 px-3.5 text-[#2D3748]">
                      <span class="font-medium text-[#1A2C44]">{{ o.destinationCode }}</span>
                      <span class="text-[#697386]"> — {{ o.destinationName }}</span>
                    </td>
                    <td class="py-2.5 px-3.5">
                      @for (l of o.lines; track l.id) {
                        <div class="text-[#2D3748]">
                          <span class="font-mono text-[#697386]">{{ l.sku }}</span>
                          <span class="font-medium text-[#1A2C44]"> {{ l.productName }}</span>: 
                          <span class="font-mono">{{ l.requestedQuantity }}</span>
                        </div>
                      }
                    </td>
                    <td class="py-2.5 px-3.5 font-mono text-[#697386]">{{ o.requiredDateUtc | date: 'dd/MM/yyyy' }}</td>
                    <td class="py-2.5 px-3.5">
                      <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium border"
                        [class.bg-[#F8FAFC]]="o.status === 'Pending'"
                        [class.text-[#697386]]="o.status === 'Pending'"
                        [class.border-[#E3E8EE]]="o.status === 'Pending'"
                        [class.bg-[#00D924]/10]="o.status === 'Delivered' || o.status === 'Planned'"
                        [class.text-[#0E6245]]="o.status === 'Delivered' || o.status === 'Planned'"
                        [class.border-[#00D924]/30]="o.status === 'Delivered' || o.status === 'Planned'"
                        [class.bg-[#DF1B41]/10]="o.status === 'Cancelled'"
                        [class.text-[#DF1B41]]="o.status === 'Cancelled'"
                        [class.border-[#DF1B41]/30]="o.status === 'Cancelled'">
                        {{ statusLabel(o.status) }}
                      </span>
                    </td>
                    @if (session.canWrite()) {
                      <td class="py-2.5 px-3.5 text-right">
                        @if (o.status === 'Pending') {
                          @if (confirmingId() === o.id) {
                            <div class="inline-flex items-center gap-2 justify-end">
                              <button type="button" class="text-xs font-medium text-[#DF1B41] hover:underline transition-colors" [disabled]="cancellingId() === o.id"
                                (click)="cancel(o)">{{ cancellingId() === o.id ? 'Cancelando…' : 'Confirmar cancelación' }}</button>
                              <button type="button" class="text-xs text-[#697386] hover:text-[#1A2C44] transition-colors" [disabled]="cancellingId() === o.id"
                                (click)="confirmingId.set(null)">No</button>
                            </div>
                          } @else {
                            <button type="button" class="text-xs font-medium text-[#697386] hover:text-[#DF1B41] underline transition-colors" [disabled]="cancellingId() !== null"
                              (click)="confirmingId.set(o.id)">Cancelar pedido</button>
                          }
                        } @else {
                          <span class="text-[#697386]">—</span>
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
}
