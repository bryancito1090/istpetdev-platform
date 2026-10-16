import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, RoutePlanDetailDto, WarehouseStockDto } from '@shared/api';
import { RutasPage } from './rutas.page';

describe('RutasPage - Prioridad de Entrega, Costo de Priorizar y Stock por Bodega (#196)', () => {
  const mockPlan = {
    id: 'plan-001',
    code: 'PLAN-20261016-VEH-01',
    revision: 1,
    status: 'Proposed',
    vehicleId: 'veh-01',
    vehicleCode: 'VEH-01',
    plannedStartUtc: '2026-10-16T08:00:00Z',
    totalDistanceKm: 42.5,
    totalDurationMinutes: 180,
    stopsCount: 3,
  };

  const mockDetail: RoutePlanDetailDto = {
    id: 'plan-001',
    code: 'PLAN-20261016-VEH-01',
    revision: 1,
    status: 'Proposed',
    vehicleId: 'veh-01',
    vehicleCode: 'VEH-01',
    plannedStartUtc: '2026-10-16T08:00:00Z',
    plannedEndUtc: '2026-10-16T12:00:00Z',
    totalDistanceKm: 42.5,
    totalDurationMinutes: 180,
    polyline: '',
    originWarehouseName: 'Bodega Central Quito (BOD-UIO)',
    driverAccountId: null,
    driverName: 'Carlos Chofer',
    deliveriesCount: 2,
    unassignedDeliveries: 0,
    stops: [
      {
        sequence: 1,
        servicePointId: 'bod-01',
        servicePointCode: 'BOD-UIO',
        servicePointName: 'Bodega Central Quito',
        etaUtc: '2026-10-16T08:00:00Z',
        plannedDepartureUtc: '2026-10-16T08:30:00Z',
        serviceMinutes: 30,
        windowStart: '07:00',
        windowEnd: '18:00',
        status: 'Completed',
        ordersCount: 0,
        kind: 'Pickup',
        deliveryId: null,
        deliveryCode: '',
        deliveryStatus: '',
        handlingUnits: [],
        lines: []
      },
      {
        sequence: 2,
        servicePointId: 'sp-hp01',
        servicePointCode: 'HP-01',
        servicePointName: 'Hospital Pediátrico Baca Ortiz',
        etaUtc: '2026-10-16T10:30:00Z',
        plannedDepartureUtc: '2026-10-16T11:00:00Z',
        serviceMinutes: 30,
        windowStart: '08:00',
        windowEnd: '17:00',
        status: 'Pending',
        ordersCount: 1,
        kind: 'Delivery',
        deliveryId: 'del-01',
        deliveryCode: 'DEL-HP01',
        deliveryStatus: 'Proposed',
        handlingUnits: [{ id: 'hu-1', code: 'HU-001', status: 'Sealed', sealedAtUtc: '2026-10-16T08:00:00Z' }],
        lines: [
          {
            productId: 'prod-des01',
            sku: 'DES-01',
            productName: 'Desinfectante Quirúrgico Grado Hospitalario',
            plannedQuantity: 20,
            baseUnit: 'Litros',
            lotCode: 'LOT-2026-A',
            deliveryStatus: 'Proposed'
          }
        ]
      }
    ],
    productsSummary: [
      {
        sku: 'DES-01',
        productName: 'Desinfectante Quirúrgico Grado Hospitalario',
        totalPlannedQuantity: 20,
        baseUnit: 'Litros',
        stopsCount: 1,
        lots: [{ lotCode: 'LOT-2026-A', quantity: 20 }],
        deliveryStatuses: ['Proposed']
      }
    ],
    driverRoutes: []
  };

  const mockStockData: WarehouseStockDto[] = [
    {
      locationId: 'bod-01',
      locationCode: 'BOD-UIO',
      locationName: 'Bodega Central Quito',
      productId: 'prod-des01',
      sku: 'DES-01',
      productName: 'Desinfectante Quirúrgico Grado Hospitalario',
      quantityOnHand: 450,
      quantityReserved: 30,
      availableQuantity: 420,
      coverageDays: 25,
      minStock: 50,
      maxStock: 500,
      status: 'Normal'
    },
    {
      locationId: 'bod-02',
      locationCode: 'BOD-GYE',
      locationName: 'Bodega Litoral Guayaquil',
      productId: 'prod-des01',
      sku: 'DES-01',
      productName: 'Desinfectante Quirúrgico Grado Hospitalario',
      quantityOnHand: 15,
      quantityReserved: 10,
      availableQuantity: 5,
      coverageDays: 2,
      minStock: 40,
      maxStock: 300,
      status: 'Critico'
    }
  ];

  function setupTestBed(canWrite = true) {
    const apiServiceMock = {
      getRoutePlans: vi.fn(() => of([mockPlan])),
      getRoutePlanById: vi.fn(() => of(mockDetail)),
      getRoutePlanGeometry: vi.fn(() => of(null)),
      getDrivers: vi.fn(() => of([{ accountId: 'drv-1', displayName: 'Carlos Chofer' }])),
      getServicePoints: vi.fn(() => of([
        { id: 'sp-hp01', code: 'HP-01', name: 'Hospital Pediátrico Baca Ortiz', criticality: 'alta' }
      ])),
      getDeliveryOrders: vi.fn(() => of([])),
      getWarehouses: vi.fn(() => of([
        { id: 'bod-01', code: 'BOD-UIO', name: 'Bodega Central Quito' }
      ])),
      getEntityData: vi.fn(() => of([])),
      getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
      createRoutePlan: vi.fn((req: any) => of({
        routes: [
          {
            id: 'plan-001',
            code: 'PLAN-20261016-VEH-01',
            vehicleCode: 'VEH-01',
            km: req?.criticalDeadlineLocal ? 48.2 : 42.5,
            cost: req?.criticalDeadlineLocal ? 21.15 : 18.40,
            stops: [
              {
                sequence: 1,
                pointCode: 'BOD-UIO',
                servicePointCode: 'BOD-UIO',
                servicePointName: 'Bodega Central Quito',
                arrivalUtc: '2026-10-16T08:00:00Z'
              },
              {
                sequence: 2,
                pointCode: 'HP-01',
                servicePointCode: 'HP-01',
                servicePointName: 'Hospital Pediátrico Baca Ortiz',
                criticality: 'alta',
                isCritical: true,
                arrivalUtc: req?.criticalDeadlineLocal ? '2026-10-16T10:30:00Z' : '2026-10-16T13:45:00Z'
              }
            ]
          }
        ],
        unserved: []
      })),
      getWarehouseStock: vi.fn(() => of(mockStockData))
    };

    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => canWrite } },
        { provide: ApiService, useValue: apiServiceMock }
      ]
    });

    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, api: apiServiceMock };
  }

  // ========================================================
  // PIEZA 1: SELECTOR "HORA LÍMITE PARA CRÍTICOS"
  // ========================================================
  it('Pieza 1: el selector de hora límite inicia vacío por defecto y se envía en createRoutePlan', () => {
    const { fixture, component, api } = setupTestBed();

    // 1. Criterio: vacío por defecto
    expect(component.criticalDeadline()).toBe('');

    // 2. Criterio: sin hora límite, createRoutePlan se llama con {} (comportamiento idéntico al actual)
    component.generatePlan();
    expect(api.createRoutePlan).toHaveBeenCalledWith({});

    // 3. Seleccionar hora límite (p. ej. 11:00)
    const selectEl = fixture.nativeElement.querySelector('[data-testid="critical-deadline-select"]') as HTMLSelectElement;
    expect(selectEl).toBeTruthy();
    selectEl.value = '11:00';
    selectEl.dispatchEvent(new Event('change'));
    component.criticalDeadline.set('11:00');
    fixture.detectChanges();

    expect(component.criticalDeadline()).toBe('11:00');

    // 4. Con hora límite seleccionada, createRoutePlan envía criticalDeadlineLocal
    component.generatePlan();
    expect(api.createRoutePlan).toHaveBeenCalledWith({ criticalDeadlineLocal: '11:00' });
    expect(component.feedbackMessage()?.text).toContain('Hora límite para críticos: 11:00');
  });

  // ========================================================
  // PIEZA 2: COMPARACIÓN "COSTO DE PRIORIZAR"
  // ========================================================
  it('Pieza 2: calcula y presenta la comparación de costo, km, no atendidos y hora de llegada de críticos', () => {
    const { fixture, component, api } = setupTestBed();

    // Ejecutar comparación
    component.criticalDeadline.set('11:00');
    component.comparePrioritization('11:00');
    fixture.detectChanges();

    // 1. Debe haber llamado a createRoutePlan dos veces: base ({}) y priorizado ({ criticalDeadlineLocal: '11:00' })
    expect(api.createRoutePlan).toHaveBeenCalledWith({});
    expect(api.createRoutePlan).toHaveBeenCalledWith({ criticalDeadlineLocal: '11:00' });

    // 2. Verificar modelo de comparación calculado
    const comparison = component.prioritizationComparison();
    expect(comparison).toBeTruthy();
    expect(comparison!.baseline.totalKm).toBe(42.5);
    expect(comparison!.prioritized.totalKm).toBe(48.2);
    expect(comparison!.diffKm).toBe(5.7);
    expect(comparison!.baseline.totalCost).toBe(18.4);
    expect(comparison!.prioritized.totalCost).toBe(21.15);
    expect(comparison!.diffCost).toBe(2.75);
    expect(comparison!.diffUnserved).toBe(0);

    // 3. Verificar que los críticos llegan antes de la hora límite (11:00)
    expect(comparison!.criticalPoints.length).toBeGreaterThan(0);
    const hp01 = comparison!.criticalPoints.find(p => p.pointCode === 'HP-01');
    expect(hp01).toBeTruthy();
    expect(hp01!.meetsDeadline).toBe(true);

    // 4. Verificar renderizado en el DOM de la tarjeta comparativa y tabla de críticos
    const cardEl = fixture.nativeElement.querySelector('[data-testid="prioritization-comparison-card"]');
    expect(cardEl).toBeTruthy();
    const cardText = cardEl.textContent;
    expect(cardText).toContain('Análisis Comparativo: Costo de Priorizar Entregas Críticas');
    expect(cardText).toContain('Hora Límite: 11:00');
    expect(cardText).toContain('Hospital Pediátrico Baca Ortiz');
    expect(cardText).toContain('Llega antes de 11:00');
  });

  // ========================================================
  // PIEZA 3: PANEL DE STOCK POR BODEGA
  // ========================================================
  it('Pieza 3: presenta el stock por bodega del producto o parada seleccionada con el endpoint nuevo', () => {
    const { fixture, component, api } = setupTestBed();

    // 1. Activar pestaña de stock por bodega
    component.detailTab.set('stock-bodegas');
    component.loadWarehouseStockForSelected();
    fixture.detectChanges();

    expect(api.getWarehouseStock).toHaveBeenCalled();

    // 2. Verificar tabla de stock por bodega en el DOM
    const panelEl = fixture.nativeElement.querySelector('[data-testid="warehouse-stock-panel"]');
    expect(panelEl).toBeTruthy();

    const tableEl = fixture.nativeElement.querySelector('[data-testid="warehouse-stock-table"]');
    expect(tableEl).toBeTruthy();
    const tableText = tableEl.textContent;
    expect(tableText).toContain('Bodega Central Quito');
    expect(tableText).toContain('BOD-UIO');
    expect(tableText).toContain('Desinfectante Quirúrgico Grado Hospitalario');
    expect(tableText).toContain('Bodega Litoral Guayaquil');
    expect(tableText).toContain('BOD-GYE');
    expect(tableText).toContain('Crítico');

    // 3. Probar inspección desde una parada específica
    const stop2 = mockDetail.stops[1];
    component.inspectStockForStop(stop2);
    fixture.detectChanges();

    expect(component.selectedStopForStock()).toBe(stop2);
    expect(component.detailTab()).toBe('stock-bodegas');
    const contextEl = fixture.nativeElement.querySelector('[data-testid="stock-active-context-stop"]');
    expect(contextEl).toBeTruthy();
    expect(contextEl.textContent).toContain('Hospital Pediátrico Baca Ortiz');
  });
});
