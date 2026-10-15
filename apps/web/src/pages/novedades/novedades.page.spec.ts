import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { FieldExceptionsApiService, FieldRequestOrderDto, ReceiptExceptionDto } from '@shared/api';
import { NovedadesPage } from './novedades.page';

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
