import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, DeliveryOrderDto } from '@shared/api';
import { RutasPage } from './rutas.page';

describe('RutasPage - Selección y Priorización de Pedidos en Asistente (#210)', () => {
  const mockOrders: DeliveryOrderDto[] = [
    {
      id: 'ord-low',
      orderNumber: 'PED-LOW-01',
      destinationCode: 'CS-01',
      destinationName: 'Centro de Salud Sur',
      isWarehouse: false,
      requiredDateUtc: '2026-10-18T12:00:00Z',
      priority: 'Low',
      status: 'Pending',
      sourceType: 'Manual',
      createdAtUtc: '2026-10-16T10:00:00Z',
      lines: [{ id: 'l1', productId: 'p1', sku: 'ALC-01', productName: 'Alcohol', requestedQuantity: 10, plannedQuantity: 0, deliveredQuantity: 0 }]
    },
    {
      id: 'ord-crit',
      orderNumber: 'PED-CRIT-01',
      destinationCode: 'HP-01',
      destinationName: 'Hospital Quirúrgico',
      isWarehouse: false,
      requiredDateUtc: '2026-10-16T09:00:00Z',
      priority: 'Critical',
      status: 'Pending',
      sourceType: 'Manual',
      createdAtUtc: '2026-10-16T08:00:00Z',
      lines: [{ id: 'l2', productId: 'p2', sku: 'ANEST-01', productName: 'Anestésico', requestedQuantity: 5, plannedQuantity: 0, deliveredQuantity: 0 }]
    },
    {
      id: 'ord-high',
      orderNumber: 'PED-HIGH-01',
      destinationCode: 'HP-02',
      destinationName: 'Hospital Pediátrico',
      isWarehouse: false,
      requiredDateUtc: '2026-10-16T11:00:00Z',
      priority: 'High',
      status: 'Pending',
      sourceType: 'Manual',
      createdAtUtc: '2026-10-16T08:30:00Z',
      lines: [{ id: 'l3', productId: 'p3', sku: 'SUERO-01', productName: 'Suero Fisiológico', requestedQuantity: 20, plannedQuantity: 0, deliveredQuantity: 0 }]
    },
    {
      id: 'ord-norm',
      orderNumber: 'PED-NORM-01',
      destinationCode: 'CS-02',
      destinationName: 'Subcentro Calderón',
      isWarehouse: false,
      requiredDateUtc: '2026-10-17T15:00:00Z',
      priority: 'Normal',
      status: 'Pending',
      sourceType: 'Manual',
      createdAtUtc: '2026-10-16T09:00:00Z',
      lines: [{ id: 'l4', productId: 'p1', sku: 'ALC-01', productName: 'Alcohol', requestedQuantity: 15, plannedQuantity: 0, deliveredQuantity: 0 }]
    }
  ];

  const mockProducts = [
    { id: 'p1', sku: 'ALC-01', name: 'Alcohol', weight_per_unit: 1.0, volume_per_unit: 0.001 },
    { id: 'p2', sku: 'ANEST-01', name: 'Anestésico', weight_per_unit: 0.4, volume_per_unit: 0.0005 },
    { id: 'p3', sku: 'SUERO-01', name: 'Suero Fisiológico', weight_per_unit: 0.5, volume_per_unit: 0.0008 }
  ];

  const setupComponent = (customOrders: DeliveryOrderDto[] = mockOrders) => {
    const apiMock = {
      getRoutePlans: vi.fn(() => of([])),
      getRoutePlanById: vi.fn(() => of(null)),
      getRoutePlanGeometry: vi.fn(() => of(null)),
      getDrivers: vi.fn(() => of([])),
      getServicePoints: vi.fn(() => of([])),
      getWarehouses: vi.fn(() => of([])),
      getDeliveryOrders: vi.fn(() => of(customOrders)),
      getEntityData: vi.fn((entity: string) => entity === 'products' ? of(mockProducts) : of([])),
      getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
      createDeliveryOrder: vi.fn((req: any) => of({ id: 'new-ord-99', ...req })),
      confirmRoutePlan: vi.fn((req: any) => of({ id: 'new-plan-1', code: 'PLAN-001' }))
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
    return { fixture, component: fixture.componentInstance, apiMock };
  };

  it('ordena pedidos pendientes jerárquicamente: Critical > High > Normal > Low', () => {
    const { component } = setupComponent();
    const sorted = component.filteredPendingOrders();

    expect(sorted.length).toBe(4);
    expect(sorted[0].priority).toBe('Critical');
    expect(sorted[0].id).toBe('ord-crit');
    expect(sorted[1].priority).toBe('High');
    expect(sorted[1].id).toBe('ord-high');
    expect(sorted[2].priority).toBe('Normal');
    expect(sorted[2].id).toBe('ord-norm');
    expect(sorted[3].priority).toBe('Low');
    expect(sorted[3].id).toBe('ord-low');
  });

  it('preselecciona automáticamente todos los pedidos Critical y High', () => {
    const { component } = setupComponent();
    const selected = component.selectedOrderIds();

    expect(selected.has('ord-crit')).toBe(true);
    expect(selected.has('ord-high')).toBe(true);
    expect(selected.has('ord-norm')).toBe(false);
    expect(selected.has('ord-low')).toBe(false);
  });

  it('permite filtrar pedidos por chips rápidos y usar acciones por lote', () => {
    const { component } = setupComponent();

    // Filtro Críticos y Altos
    component.orderFilter.set('critical_high');
    const filteredPrio = component.filteredPendingOrders();
    expect(filteredPrio.length).toBe(2);
    expect(filteredPrio.every(o => o.priority === 'Critical' || o.priority === 'High')).toBe(true);

    // Deseleccionar todos
    component.deselectAllOrders();
    expect(component.selectedOrderIds().size).toBe(0);

    // Seleccionar todos los críticos
    component.selectAllCritical();
    expect(component.selectedOrderIds().has('ord-crit')).toBe(true);
    expect(component.selectedOrderIds().has('ord-high')).toBe(true);
    expect(component.selectedOrderIds().size).toBe(2);

    // Filtro Todos
    component.orderFilter.set('all');
    component.selectAllFiltered();
    expect(component.selectedOrderIds().size).toBe(4);
  });

  it('calcula la carga acumulada en tiempo real (peso kg y volumen m³)', () => {
    const { component } = setupComponent();

    // ord-crit: 5 u * 0.4 kg = 2.0 kg; ord-high: 20 u * 0.5 kg = 10.0 kg => Total 12.0 kg
    const summary = component.selectedOrdersSummary();
    expect(summary.count).toBe(2);
    expect(summary.weightKg).toBe(12.0);
    expect(summary.criticalCount).toBe(1);
    expect(summary.highCount).toBe(1);
    expect(summary.volumeM3).toBeGreaterThan(0);
  });

  it('permite crear pedidos manuales con selector de prioridad explícito', () => {
    const { component, apiMock } = setupComponent();

    component.newOrderDestId = 'sp-dest-01';
    component.newOrderProdId = 'p1';
    component.newOrderQty = 25;
    component.newOrderPriority = 'Critical';

    component.submitNewOrder();

    expect(apiMock.createDeliveryOrder).toHaveBeenCalledWith(
      expect.objectContaining({
        destinationServicePointId: 'sp-dest-01',
        priority: 'Critical',
        lines: [{ productId: 'p1', quantity: 25 }]
      })
    );
  });

  it('alerta de sobrecarga cuando el peso excede la capacidad del vehículo y maneja remanentes', () => {
    const { component, apiMock } = setupComponent();

    // Configurar vehículo con capacidad baja (10 kg)
    component.availableVehicleMatches.set([
      { vehicleId: 'veh-micro', vehicleCode: 'M-01', maxWeight: 10, isMatch: true }
    ]);
    component.selectedVehicleId.set('veh-micro');

    // La carga seleccionada es 12 kg (> 10 kg)
    expect(component.selectedVehicleOverweight()).toBe(true);

    // Confirmar plan: debe detectar que quedan pedidos pendientes no seleccionados
    component.confirmAndSavePlan();

    expect(apiMock.confirmRoutePlan).toHaveBeenCalled();
    expect(component.hasRemainingPendingOrders()).toBe(true);
    expect(component.feedbackMessage()?.text).toContain('Quedan');
  });
});
