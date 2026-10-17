import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, DeliveryOrderDto } from '@shared/api';
import { RutasPage } from './rutas.page';

// #199: el asistente permite editar la cantidad por línea antes de confirmar y la envía en `lines`.
describe('RutasPage - cantidad por línea en el asistente (#199)', () => {
  const orders: DeliveryOrderDto[] = [
    {
      id: 'ord-1',
      orderNumber: 'PED-001',
      destinationCode: 'HOS-01',
      destinationName: 'Hospital',
      isWarehouse: false,
      requiredDateUtc: '2026-10-17T12:00:00Z',
      priority: 'Critical',
      status: 'Pending',
      sourceType: 'Manual',
      createdAtUtc: '2026-10-16T10:00:00Z',
      lines: [
        // 150 pedidas, 30 ya planificadas en otra ruta: pendiente 120.
        { id: 'l1', productId: 'p1', sku: 'ALC-01', productName: 'Alcohol', requestedQuantity: 150, plannedQuantity: 30, deliveredQuantity: 0 },
        // Ya planificada por completo: no se ofrece.
        { id: 'l2', productId: 'p2', sku: 'GUA-01', productName: 'Guantes', requestedQuantity: 10, plannedQuantity: 10, deliveredQuantity: 0 },
        { id: 'l3', productId: 'p3', sku: 'SUE-01', productName: 'Suero', requestedQuantity: 20, plannedQuantity: 0, deliveredQuantity: 5 }
      ]
    }
  ];

  const setup = () => {
    const apiMock = {
      getRoutePlans: vi.fn(() => of([])),
      getRoutePlanById: vi.fn(() => of(null)),
      getRoutePlanGeometry: vi.fn(() => of(null)),
      getDrivers: vi.fn(() => of([])),
      getServicePoints: vi.fn(() => of([])),
      getWarehouses: vi.fn(() => of([])),
      getDeliveryOrders: vi.fn(() => of(orders)),
      getEntityData: vi.fn(() => of([])),
      getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
      confirmRoutePlan: vi.fn((_req: any) => of({ id: 'plan-1', lines: [] }))
    };
    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => true } },
        { provide: ApiService, useValue: apiMock }
      ]
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.selectedOrderIds.set(new Set(['ord-1']));
    component.selectedVehicleId.set('veh-1');
    return { component, apiMock };
  };

  it('ofrece por defecto lo pendiente de cada línea y omite las ya planificadas', () => {
    const { component } = setup();
    const lines = component.selectedOrderLines();
    expect(lines.map(l => [l.sku, l.pending, l.quantity])).toEqual([
      ['ALC-01', 120, 120],
      ['SUE-01', 15, 15]
    ]);
    expect(component.lineQuantityError()).toBeNull();
  });

  it('sin ediciones no envía lines (la API planifica lo pendiente)', () => {
    const { component, apiMock } = setup();
    component.confirmAndSavePlan();
    expect(apiMock.confirmRoutePlan).toHaveBeenCalledTimes(1);
    expect('lines' in apiMock.confirmRoutePlan.mock.calls[0][0]).toBe(false);
  });

  it('envía en lines solo la cantidad editada', () => {
    const { component, apiMock } = setup();
    component.setLineQuantity('ord-1|p1', 100);
    component.confirmAndSavePlan();
    expect(apiMock.confirmRoutePlan.mock.calls[0][0].lines).toEqual([
      { deliveryOrderId: 'ord-1', productId: 'p1', plannedQuantity: 100 }
    ]);
  });

  it('no confirma si la cantidad supera lo pendiente o no es positiva', () => {
    const { component, apiMock } = setup();
    component.setLineQuantity('ord-1|p1', 121);
    expect(component.lineQuantityError()).toContain('PED-001 · ALC-01');
    component.confirmAndSavePlan();
    expect(apiMock.confirmRoutePlan).not.toHaveBeenCalled();
    expect(component.feedbackMessage()?.type).toBe('error');

    component.setLineQuantity('ord-1|p1', 0);
    expect(component.lineQuantityError()).not.toBeNull();
    component.setLineQuantity('ord-1|p1', Number.NaN);
    expect(component.lineQuantityError()).not.toBeNull();
    component.setLineQuantity('ord-1|p1', 120);
    expect(component.lineQuantityError()).toBeNull();
  });
});
