import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthSession } from '@entities/session';
import { FieldExceptionsApiService, FieldRequestOrderDto, ReceiptExceptionDto } from '@shared/api';

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
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 pb-5">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Novedades de campo</h1>
            @if (!session.canWrite()) {
              <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200">
                Solo lectura
              </span>
            }
          </div>
          <p class="text-sm text-slate-500 mt-1">
            Recepciones con rechazos o con diferencias de stock en percha, y pedidos de reposición hechos en el punto de servicio.
          </p>
        </div>
        <button type="button" (click)="loadAll()" [disabled]="receiptsLoading() || ordersLoading()"
          class="inline-flex items-center text-xs font-semibold px-3 py-2 rounded-lg border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 disabled:opacity-50">
          Actualizar
        </button>
      </div>

      <!-- Recepciones con novedades -->
      <section class="space-y-4" aria-labelledby="titulo-recepciones">
        <div class="flex flex-col sm:flex-row sm:items-end justify-between gap-3">
          <div>
            <h2 id="titulo-recepciones" class="text-lg font-semibold text-slate-900">Recepciones con novedades</h2>
            <p class="text-xs text-slate-500">Rechazos del receptor y ajustes entre el stock declarado en percha y el saldo del sistema.</p>
          </div>
          @if (pointOptions().length > 0) {
            <label class="text-xs text-slate-600 flex items-center gap-2">
              Punto
              <select class="border border-slate-300 rounded-lg px-2 py-1 text-xs" [ngModel]="pointFilter()" (ngModelChange)="onPointFilter($event)">
                <option value="">Todos</option>
                @for (p of pointOptions(); track p.id) {
                  <option [value]="p.id">{{ p.code }} — {{ p.name }}</option>
                }
              </select>
            </label>
          }
        </div>

        @if (receiptsLoading()) {
          <p class="text-sm text-slate-500">Cargando recepciones…</p>
        } @else if (receiptsError()) {
          <div class="p-4 rounded-lg border border-rose-200 bg-rose-50 text-sm text-rose-800 flex items-center justify-between gap-3">
            <span>{{ receiptsError() }}</span>
            <button type="button" class="text-xs font-semibold underline" (click)="loadReceipts()">Reintentar</button>
          </div>
        } @else if (receipts().length === 0) {
          <p class="text-sm text-slate-500 p-4 rounded-lg border border-dashed border-slate-300">No hay recepciones con rechazos ni ajustes.</p>
        } @else {
          <p class="text-xs text-slate-500">Mostrando {{ receipts().length }} de {{ receiptsTotal() }}.</p>
          @for (r of receipts(); track r.receiptId) {
            <article class="rounded-xl border border-slate-200 bg-white p-4 space-y-3" [attr.data-receipt]="r.receiptId">
              <div class="flex flex-wrap items-center gap-2 justify-between">
                <div>
                  <p class="text-sm font-semibold text-slate-900">{{ r.servicePointCode }} — {{ r.servicePointName }}</p>
                  <p class="text-xs text-slate-500">
                    Entrega {{ r.deliveryCode }} · registrada por {{ r.actorName || 'cuenta desconocida' }} ·
                    servidor {{ r.receivedAtServer | date: 'dd/MM/yyyy HH:mm' }}
                    @if (r.occurredAtDevice) { · dispositivo {{ r.occurredAtDevice | date: 'dd/MM/yyyy HH:mm' }} }
                  </p>
                </div>
                <div class="flex gap-2">
                  @if (r.hasRejections) {
                    <span class="text-[11px] font-semibold px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-200">Rechazo</span>
                  }
                  @if (r.hasAdjustments) {
                    <span class="text-[11px] font-semibold px-2 py-0.5 rounded bg-amber-50 text-amber-800 border border-amber-200">Ajuste de percha</span>
                  }
                </div>
              </div>

              <div class="overflow-x-auto">
                <table class="w-full text-xs">
                  <thead class="text-slate-500 text-left">
                    <tr>
                      <th class="py-1 pr-3">Producto / lote</th>
                      <th class="py-1 pr-3 text-right">Aceptado</th>
                      <th class="py-1 pr-3 text-right">Rechazado</th>
                      <th class="py-1 pr-3">Motivo</th>
                      <th class="py-1 pr-3 text-right">Percha declarada</th>
                      <th class="py-1">Ajuste</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (l of r.lines; track l.receiptLineId) {
                      <tr class="border-t border-slate-100">
                        <td class="py-1 pr-3">{{ l.sku }} {{ l.productName }} <span class="text-slate-400">({{ l.lotCode }})</span></td>
                        <td class="py-1 pr-3 text-right">{{ l.quantityAccepted }}</td>
                        <td class="py-1 pr-3 text-right" [class.text-rose-700]="l.quantityRejected > 0">{{ l.quantityRejected }}</td>
                        <td class="py-1 pr-3">{{ l.quantityRejected > 0 ? (l.rejectionReason || 'Sin motivo registrado') : '—' }}</td>
                        <td class="py-1 pr-3 text-right">{{ l.remainingDeclared ?? '—' }}</td>
                        <td class="py-1" [title]="l.adjustmentReason ?? ''">
                          @if (l.adjustmentQuantity != null) {
                            <span [class.text-amber-800]="l.adjustmentQuantity < 0" [class.text-sky-800]="l.adjustmentQuantity > 0">
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

              <div class="text-xs text-slate-600">
                <p class="font-semibold">Evidencia</p>
                @if (r.evidence.length === 0) {
                  <p class="text-slate-500">Sin evidencia registrada.</p>
                } @else {
                  <ul class="list-disc pl-5">
                    @for (e of r.evidence; track e.evidenceId) {
                      <li class="flex items-center gap-1.5 py-0.5">
                        <span class="font-semibold capitalize">{{ e.kind === 'photo' ? 'Foto' : e.kind === 'signature' ? 'Firma' : e.kind }}:</span>
                        @if (e.status === 'Confirmed') {
                          <span class="text-emerald-700 font-bold">✓ Confirmada en almacenamiento S3</span>
                        } @else {
                          <span class="text-amber-700 font-medium">⏳ Pendiente de subida</span>
                        }
                        @if (e.byteSize) { · <span class="text-slate-500">{{ e.byteSize }} bytes</span> }
                        @if (e.sha256) { · <span class="font-mono text-slate-500" [title]="e.sha256">SHA-256: {{ e.sha256.slice(0, 12) }}…</span> }
                      </li>
                    }
                  </ul>
                  <p class="text-slate-500 mt-1 text-[11px]">Archivos privados en S3 (cifrados con integridad SHA-256 verificada por el servidor).</p>
                }
              </div>
            </article>
          }
        }
      </section>

      <!-- Pedidos de reposición en campo -->
      <section class="space-y-4" aria-labelledby="titulo-pedidos">
        <div>
          <h2 id="titulo-pedidos" class="text-lg font-semibold text-slate-900">Pedidos de reposición en campo</h2>
          <p class="text-xs text-slate-500">Lo que el receptor pidió para la próxima visita. Se planifican desde Rutas.</p>
        </div>

        @if (actionMessage()) {
          <div class="p-3 rounded-lg border text-sm"
            [class.border-rose-200]="actionIsError()" [class.bg-rose-50]="actionIsError()" [class.text-rose-800]="actionIsError()"
            [class.border-emerald-200]="!actionIsError()" [class.bg-emerald-50]="!actionIsError()" [class.text-emerald-800]="!actionIsError()">
            {{ actionMessage() }}
          </div>
        }

        @if (ordersLoading()) {
          <p class="text-sm text-slate-500">Cargando pedidos…</p>
        } @else if (ordersError()) {
          <div class="p-4 rounded-lg border border-rose-200 bg-rose-50 text-sm text-rose-800 flex items-center justify-between gap-3">
            <span>{{ ordersError() }}</span>
            <button type="button" class="text-xs font-semibold underline" (click)="loadOrders()">Reintentar</button>
          </div>
        } @else if (orders().length === 0) {
          <p class="text-sm text-slate-500 p-4 rounded-lg border border-dashed border-slate-300">No hay pedidos de reposición hechos en campo.</p>
        } @else {
          <div class="overflow-x-auto rounded-xl border border-slate-200 bg-white">
            <table class="w-full text-xs">
              <thead class="text-slate-500 text-left bg-slate-50">
                <tr>
                  <th class="py-2 px-3">Pedido</th>
                  <th class="py-2 px-3">Destino</th>
                  <th class="py-2 px-3">Productos</th>
                  <th class="py-2 px-3">Requerido</th>
                  <th class="py-2 px-3">Estado</th>
                  @if (session.canWrite()) { <th class="py-2 px-3">Acción</th> }
                </tr>
              </thead>
              <tbody>
                @for (o of orders(); track o.id) {
                  <tr class="border-t border-slate-100" [attr.data-order]="o.id">
                    <td class="py-2 px-3 font-mono">{{ o.orderNumber }}</td>
                    <td class="py-2 px-3">{{ o.destinationCode }} — {{ o.destinationName }}</td>
                    <td class="py-2 px-3">
                      @for (l of o.lines; track l.id) {
                        <div>{{ l.sku }} {{ l.productName }}: {{ l.requestedQuantity }}</div>
                      }
                    </td>
                    <td class="py-2 px-3">{{ o.requiredDateUtc | date: 'dd/MM/yyyy' }}</td>
                    <td class="py-2 px-3">{{ statusLabel(o.status) }}</td>
                    @if (session.canWrite()) {
                      <td class="py-2 px-3">
                        @if (o.status === 'Pending') {
                          @if (confirmingId() === o.id) {
                            <button type="button" class="text-xs font-semibold text-rose-700 mr-2" [disabled]="cancellingId() === o.id"
                              (click)="cancel(o)">{{ cancellingId() === o.id ? 'Cancelando…' : 'Confirmar cancelación' }}</button>
                            <button type="button" class="text-xs text-slate-600" [disabled]="cancellingId() === o.id"
                              (click)="confirmingId.set(null)">No</button>
                          } @else {
                            <button type="button" class="text-xs font-semibold text-slate-700 underline" [disabled]="cancellingId() !== null"
                              (click)="confirmingId.set(o.id)">Cancelar pedido</button>
                          }
                        } @else { — }
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
