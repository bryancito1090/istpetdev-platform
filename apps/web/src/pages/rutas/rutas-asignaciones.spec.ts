import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, DeliveryOrderDto } from '@shared/api';
import { RutasPage } from './rutas.page';

const order = (plannedQuantity: number): DeliveryOrderDto => ({
  id: 'field-order', orderNumber: 'REP-01', destinationName: 'Clínica Sur', destinationCode: 'SUR',
  isWarehouse: false, requiredDateUtc: '', priority: 'Normal', status: 'Pending', sourceType: 'FieldRequest', createdAtUtc: '',
  lines: [{ id: 'line', productId: 'product', sku: 'P', productName: 'Guantes', requestedQuantity: 10, plannedQuantity, deliveredQuantity: 0 }]
});

function setup(plannedQuantity: number) {
  const api = { getRoutePlans: vi.fn(() => of([])), getDrivers: vi.fn(() => of([])),
    getServicePoints: vi.fn(() => of([])), getWarehouses: vi.fn(() => of([])), getEntityData: vi.fn(() => of([])),
    getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
    getDeliveryOrders: vi.fn(() => of([order(plannedQuantity)])), suggestRoutes: vi.fn(() => of({ options: [] })) };
  TestBed.configureTestingModule({ imports: [RutasPage], providers: [provideRouter([]),
    { provide: AuthSession, useValue: { canWrite: () => true } }, { provide: ApiService, useValue: api }] });
  const fixture = TestBed.createComponent(RutasPage), page = fixture.componentInstance;
  fixture.detectChanges(); page.openOrderRoutingModal(); fixture.detectChanges();
  return { fixture, page, api };
}

describe('Rutas: aviso previo de entregas asignadas #345', () => {
  it('avisa al seleccionar un pedido parcialmente asignado y detiene el cálculo automático', () => {
    const { fixture, page, api } = setup(4);
    expect(fixture.nativeElement.querySelector('[data-testid="assigned-orders-warning"]').textContent).toContain('REP-01');
    page.advanceWizardStep();
    expect(api.suggestRoutes).not.toHaveBeenCalled();
    expect(page.wizardStep()).toBe(1);
    fixture.destroy();
  });

  it('solo calcula los pendientes tras la acción explícita y reinicia el permiso al cambiar la selección', () => {
    const { fixture, page, api } = setup(4);
    fixture.nativeElement.querySelector('[data-testid="calculate-remaining"]').click();
    expect(api.suggestRoutes).toHaveBeenCalledWith(['field-order'], undefined, undefined, true);
    page.wizardStep.set(1); page.toggleOrderSelection('field-order'); page.toggleOrderSelection('field-order');
    api.suggestRoutes.mockClear(); page.advanceWizardStep();
    expect(api.suggestRoutes).not.toHaveBeenCalled();
    fixture.destroy();
  });

  it('no permite continuar si todas las cantidades ya están cubiertas', () => {
    const { fixture, page, api } = setup(10);
    expect(fixture.nativeElement.querySelector('[data-testid="assigned-orders-warning"]').textContent).toContain('No se generará otra ruta');
    expect(fixture.nativeElement.querySelector('[data-testid="calculate-remaining"]')).toBeNull();
    page.advanceWizardStep(); expect(api.suggestRoutes).not.toHaveBeenCalled();
    fixture.destroy();
  });

  it('muestra el aviso del servidor y permanece en selección si otro usuario asignó el pedido', () => {
    const { fixture, page, api } = setup(0);
    api.suggestRoutes.mockReturnValue(throwError(() => ({ status: 409, error: { code: 'orders_already_assigned',
      message: 'El pedido ya tiene una ruta asignada.', assignedOrders: [{ orderId: 'field-order', orderNumber: 'REP-01', hasPendingQuantity: false }] } })));
    page.advanceWizardStep(); fixture.detectChanges();
    expect(page.wizardStep()).toBe(1);
    expect(fixture.nativeElement.querySelector('[data-testid="assigned-orders-warning"]').textContent).toContain('REP-01');
    fixture.destroy();
  });

  it('permite calcular un pedido nuevo de reposición, aunque su destino tenga entregas previas', () => {
    const { fixture, page, api } = setup(0);
    page.advanceWizardStep(); expect(api.suggestRoutes).toHaveBeenCalledOnce();
    fixture.destroy();
  });
});
