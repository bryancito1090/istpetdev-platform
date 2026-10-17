import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { FieldExceptionsApiService, FieldRequestOrderDto, ReceiptExceptionDto } from '@shared/api';
import { NovedadesPage, formatBacktrack, summarizeRejections } from './novedades.page';

const receipt: ReceiptExceptionDto = {
  receiptId: 'r-1',
  deliveryId: 'd-1',
  deliveryCode: 'D-001',
  servicePointId: 'sp-1',
  servicePointCode: 'CS-NORTE',
  servicePointName: 'Centro de Salud Norte',
  actorName: 'Receptor de prueba',
  receivedAtServer: '2026-10-15T15:00:00Z',
  occurredAtDevice: '2026-10-15T14:58:00Z',
  hasRejections: true,
  hasAdjustments: true,
  lines: [{
    receiptLineId: 'rl-1', sku: 'SKU-1', productName: 'Desinfectante', lotCode: 'L-1',
    quantityAccepted: 8, quantityRejected: 4, rejectionReason: 'Daño en el empaque',
    remainingDeclared: 2, adjustmentQuantity: -6, adjustmentReason: 'consumo derivado',
  }],
  evidence: [],
};

const order = (id: string, status: string): FieldRequestOrderDto => ({
  id, orderNumber: `ORD-${id}`, destinationCode: 'CS-NORTE', destinationName: 'Centro de Salud Norte',
  requiredDateUtc: '2026-10-17T00:00:00Z', priority: 'Normal', status, sourceType: 'FieldRequest',
  notes: '', createdAtUtc: '2026-10-15T15:00:00Z', version: 3,
  lines: [{ id: `l-${id}`, productId: 'p-1', sku: 'SKU-1', productName: 'Desinfectante', requestedQuantity: 20, plannedQuantity: 0, deliveredQuantity: 0 }],
});

function render(canWrite: boolean, api: Partial<FieldExceptionsApiService>) {
  TestBed.configureTestingModule({
    imports: [NovedadesPage],
    providers: [
      { provide: AuthSession, useValue: { canWrite: () => canWrite } },
      { provide: FieldExceptionsApiService, useValue: api },
    ],
  });
  const fixture = TestBed.createComponent(NovedadesPage);
  fixture.detectChanges();
  return fixture;
}

function buttons(el: HTMLElement): HTMLButtonElement[] {
  return Array.from(el.querySelectorAll('button')) as HTMLButtonElement[];
}

describe('NovedadesPage (#116)', () => {
  it('el Auditor ve rechazos y ajustes sin ningún botón de acción (FR-21)', () => {
    const fixture = render(false, {
      listReceiptExceptions: vi.fn(() => of({ items: [receipt], total: 1 })),
      listFieldRequests: vi.fn(() => of([order('o-1', 'Pending')])),
      cancelOrder: vi.fn(),
    });
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Solo lectura');
    expect(text).toContain('Daño en el empaque');
    expect(text).toContain('(consumo derivado)');
    expect(buttons(fixture.nativeElement).some(b => /Cancelar/.test(b.textContent ?? ''))).toBe(false);
  });

  it('el Planificador cancela solo pedidos pendientes, con confirmación y la versión vista', () => {
    const cancelOrder = vi.fn(() => of({ id: 'o-1', status: 'Cancelled', version: 4 }));
    const listFieldRequests = vi.fn(() => of([order('o-1', 'Pending'), order('o-2', 'Planned')]));
    const fixture = render(true, {
      listReceiptExceptions: vi.fn(() => of({ items: [], total: 0 })),
      listFieldRequests,
      cancelOrder,
    });
    const el = fixture.nativeElement as HTMLElement;

    expect(buttons(el).filter(b => b.textContent?.includes('Cancelar pedido'))).toHaveLength(1);
    buttons(el).find(b => b.textContent?.includes('Cancelar pedido'))!.click();
    fixture.detectChanges();
    expect(cancelOrder).not.toHaveBeenCalled();

    buttons(el).find(b => b.textContent?.includes('Confirmar cancelación'))!.click();
    fixture.detectChanges();
    expect(cancelOrder).toHaveBeenCalledWith('o-1', 3);
    expect(listFieldRequests).toHaveBeenCalledTimes(2);
  });

  it('un 409 muestra el motivo del servidor y recarga el estado real del pedido', () => {
    const conflict = new HttpErrorResponse({ status: 409, error: { detail: 'El pedido ORD-o-1 está en estado Planned y ya no se puede cancelar.' } });
    const listFieldRequests = vi.fn(() => of([order('o-1', 'Pending')]));
    const fixture = render(true, {
      listReceiptExceptions: vi.fn(() => of({ items: [], total: 0 })),
      listFieldRequests,
      cancelOrder: vi.fn(() => throwError(() => conflict)),
    });
    const el = fixture.nativeElement as HTMLElement;

    buttons(el).find(b => b.textContent?.includes('Cancelar pedido'))!.click();
    fixture.detectChanges();
    buttons(el).find(b => b.textContent?.includes('Confirmar cancelación'))!.click();
    fixture.detectChanges();

    expect(el.textContent).toContain('ya no se puede cancelar');
    expect(listFieldRequests).toHaveBeenCalledTimes(2);
  });

  it('si falla una sección, la otra sigue visible y se puede reintentar', () => {
    const listReceiptExceptions = vi.fn(() => throwError(() => new HttpErrorResponse({ status: 0 })));
    const fixture = render(true, {
      listReceiptExceptions,
      listFieldRequests: vi.fn(() => of([order('o-1', 'Pending')])),
      cancelOrder: vi.fn(),
    });
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Sin conexión con el servidor');
    expect(el.textContent).toContain('ORD-o-1');
    buttons(el).find(b => b.textContent?.trim() === 'Reintentar')!.click();
    expect(listReceiptExceptions).toHaveBeenCalledTimes(2);
  });
});

describe('Resumen de rechazos por motivo (#236)', () => {
  const line = (id: string, rejected: number, reason: string | null) => ({
    receiptLineId: id, sku: 'SKU-1', productName: 'Desinfectante', lotCode: 'L-1',
    quantityAccepted: 10 - rejected, quantityRejected: rejected, rejectionReason: reason,
    remainingDeclared: null, adjustmentQuantity: null, adjustmentReason: null,
  });
  const rotulado: ReceiptExceptionDto = {
    ...receipt, receiptId: 'r-2', deliveryCode: 'D-002', hasAdjustments: false,
    lines: [line('rl-2', 3, 'ErrorRotulado'), line('rl-3', 2, 'Daño'), line('rl-4', 0, null)],
  };
  const otra: ReceiptExceptionDto = {
    ...receipt, receiptId: 'r-3', deliveryCode: 'D-003', hasAdjustments: false,
    lines: [line('rl-5', 1, 'ErrorRotulado'), line('rl-6', 4, null)],
  };

  it('cuenta líneas y unidades rechazadas por motivo y omite las líneas sin rechazo', () => {
    expect(summarizeRejections([rotulado, otra])).toEqual([
      { reason: 'ErrorRotulado', label: 'Error de rotulado', lines: 2, units: 4 },
      { reason: 'Sin motivo registrado', label: 'Sin motivo registrado', lines: 1, units: 4 },
      { reason: 'Daño', label: 'Daño', lines: 1, units: 2 },
    ]);
  });

  it('una recepción con ErrorRotulado aparece en la lista y en el resumen arriba de ella', () => {
    const fixture = render(false, {
      listReceiptExceptions: vi.fn(() => of({ items: [rotulado], total: 1 })),
      listFieldRequests: vi.fn(() => of([])),
      cancelOrder: vi.fn(),
    });
    const el = fixture.nativeElement as HTMLElement;
    const summary = el.querySelector('[data-rejection-summary]')!;

    expect(summary.querySelector('[data-reason="ErrorRotulado"]')?.textContent).toMatch(/Error de rotulado:\s*1 línea · 3 unidades/);
    expect(summary.querySelector('[data-reason="Daño"]')?.textContent).toMatch(/Daño:\s*1 línea · 2 unidades/);
    // El resumen va antes de la primera tarjeta.
    const card = el.querySelector('[data-receipt="r-2"]')!;
    expect(summary.compareDocumentPosition(card) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(card.textContent).toContain('Error de rotulado');
  });

  it('sin rechazos (solo ajustes de percha) no muestra el resumen', () => {
    const fixture = render(false, {
      listReceiptExceptions: vi.fn(() => of({ items: [{ ...rotulado, hasRejections: false, lines: [line('rl-7', 0, null)] }], total: 1 })),
      listFieldRequests: vi.fn(() => of([])),
      cancelOrder: vi.fn(),
    });
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-rejection-summary]')).toBeNull();
  });
});

describe('Reentrega estimada en cada tarjeta (#237)', () => {
  function renderWith(items: ReceiptExceptionDto[]) {
    return render(false, {
      listReceiptExceptions: vi.fn(() => of({ items, total: items.length })),
      listFieldRequests: vi.fn(() => of([])),
      cancelOrder: vi.fn(),
    });
  }

  it('formatea km, USD y horas con coma decimal y dice «estimada»', () => {
    expect(formatBacktrack({ km: 18.4, costUsd: 8.3, hours: 0.62, isApproximate: false }))
      .toBe('Reentrega estimada: 18,4 km · USD 8,30 · 0,6 h');
  });

  it('muestra la línea en la tarjeta y marca «aproximado» si la distancia es en línea recta', () => {
    const fixture = renderWith([
      { ...receipt, receiptId: 'r-road', estimatedBacktrack: { km: 18.6, costUsd: 12.09, hours: 0.62, isApproximate: false } },
      { ...receipt, receiptId: 'r-approx', estimatedBacktrack: { km: 9.75, costUsd: 5.85, hours: 0.24, isApproximate: true } },
    ]);
    const el = fixture.nativeElement as HTMLElement;
    const road = el.querySelector('[data-receipt="r-road"] [data-backtrack]')!;
    const approx = el.querySelector('[data-receipt="r-approx"] [data-backtrack]')!;

    expect(road.textContent).toContain('Reentrega estimada: 18,6 km · USD 12,09 · 0,6 h');
    expect(road.textContent).not.toContain('aproximado');
    expect(approx.textContent).toContain('Reentrega estimada: 9,8 km · USD 5,85 · 0,2 h');
    expect(approx.textContent).toContain('aproximado');
    expect(road.getAttribute('title')).toContain('No se suma al costo de la ruta original');
  });

  it('sin estimación (null o ausente) no muestra la línea ni inventa valores', () => {
    const fixture = renderWith([
      { ...receipt, receiptId: 'r-null', estimatedBacktrack: null },
      { ...receipt, receiptId: 'r-none' },
    ]);
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-backtrack]')).toBeNull();
  });
});
