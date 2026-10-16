import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, RouteGeometryDto, RoutePlanSummaryDto } from '@shared/api';
import { RutasPage, ROUTE_COLORS, currentPlans } from './rutas.page';

const plan = (id: string, vehicleCode: string, revision = 1, status = 'Proposed'): RoutePlanSummaryDto => ({
  id, code: `PLAN-${vehicleCode}`, revision, status, vehicleId: `v-${vehicleCode}`, vehicleCode,
  plannedStartUtc: '2026-10-12T12:00:00Z', totalDistanceKm: 10, totalDurationMinutes: 30, stopsCount: 2,
});

const geometry = (planId: string, vehicleCode: string, source: 'osrm' | 'approximate'): RouteGeometryDto => ({
  routePlanId: planId, vehicleCode, source, roadKm: source === 'osrm' ? 21.5 : null,
  coordinates: [[-0.10, -78.44], [-0.17, -78.48], [-0.21, -78.50], [-0.10, -78.44]],
  depot: { code: 'BOD-1', name: 'Bodega Norte', latitude: -0.10, longitude: -78.44, sequence: 0 },
  stops: [
    { code: 'P-1', name: 'Punto 1', latitude: -0.17, longitude: -78.48, sequence: 1 },
    { code: 'P-2', name: 'Punto 2', latitude: -0.21, longitude: -78.50, sequence: 2 },
  ],
});

describe('currentPlans', () => {
  it('deja la última revisión vigente de cada vehículo', () => {
    const result = currentPlans([
      plan('a1', 'VEH-01', 1), plan('a2', 'VEH-01', 2), plan('b1', 'VEH-02', 3, 'Superseded'),
      plan('b2', 'VEH-02', 1), plan('c1', 'VEH-03', 1, 'Cancelled'),
    ]);
    expect(result.map(p => p.id)).toEqual(['a2', 'b2']);
  });
});

describe('RutasPage: mapa de todas las rutas', () => {
  function render() {
    const plans = [plan('p1', 'VEH-01'), plan('p2', 'VEH-02')];
    const geometries: Record<string, RouteGeometryDto> = {
      p1: geometry('p1', 'VEH-01', 'osrm'),
      p2: geometry('p2', 'VEH-02', 'approximate'),
    };
    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => true } },
        {
          provide: ApiService,
          useValue: {
            getRoutePlans: vi.fn(() => of(plans)),
            getRoutePlanById: vi.fn((id: string) => of({ ...plans.find(p => p.id === id), stops: [] })),
            getRoutePlanGeometry: vi.fn((id: string) => of(geometries[id])),
            getDrivers: vi.fn(() => of([{ accountId: 'drv-1', displayName: 'Conductor 1' }])),
            getServicePoints: vi.fn(() => of([])),
            getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])),
            getEntityData: vi.fn(() => of([])),
            getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();
    return fixture;
  }

  it('dibuja cada ruta con un color distinto y resalta la seleccionada', () => {
    const page = render().componentInstance;
    const lines = page.mapPolylines();

    expect(lines).toHaveLength(2);
    expect(new Set(lines.map(l => l.color)).size).toBe(2);
    expect(lines.map(l => l.color)).toEqual(expect.arrayContaining([ROUTE_COLORS[0], ROUTE_COLORS[1]]));
    const selected = lines.find(l => l.id === 'p1')!;
    const other = lines.find(l => l.id === 'p2')!;
    expect(selected.weight).toBeGreaterThan(other.weight!);
    expect(other.opacity).toBeLessThan(selected.opacity!);
    // La seleccionada se dibuja al final (encima) y el trazado aproximado va punteado.
    expect(lines[lines.length - 1].id).toBe('p1');
    expect(other.dashArray).toBeTruthy();
    expect(selected.dashArray).toBeUndefined();
  });

  it('marca la bodega una sola vez, numera las paradas con el color de su ruta y arma la leyenda', () => {
    const fixture = render();
    const page = fixture.componentInstance;
    const markers = page.mapMarkers();

    expect(markers.filter(m => m.id.startsWith('depot-'))).toHaveLength(1);
    const stops = markers.filter(m => m.pinLabel);
    expect(stops.map(m => m.pinLabel)).toEqual(['1', '2']);
    expect(stops.every(m => m.color === ROUTE_COLORS[0])).toBe(true);

    const legend = page.routeLegend();
    expect(legend.map(i => i.vehicleCode)).toEqual(['VEH-01', 'VEH-02']);
    expect(legend[0].km).toBe(21.5);
    expect(page.routeLegendHasApproximate()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Las líneas punteadas son aproximadas');
  });
});

describe('RutasPage: reintento del trazado por calles', () => {
  it('vuelve a pedir solo las rutas en rectas y deja de pedir cuando OSRM responde', () => {
    vi.useFakeTimers();
    try {
      const plans = [plan('p1', 'VEH-01'), plan('p2', 'VEH-02')];
      let p2Source: 'osrm' | 'approximate' = 'approximate';
      const getRoutePlanGeometry = vi.fn((id: string) => of(geometry(id, id === 'p1' ? 'VEH-01' : 'VEH-02', id === 'p1' ? 'osrm' : p2Source)));
      TestBed.configureTestingModule({
        imports: [RutasPage],
        providers: [
          provideRouter([]),
          { provide: AuthSession, useValue: { canWrite: () => true } },
          {
            provide: ApiService,
            useValue: {
              getRoutePlans: vi.fn(() => of(plans)),
              getRoutePlanById: vi.fn((id: string) => of({ ...plans.find(p => p.id === id), stops: [] })),
              getRoutePlanGeometry,
              getDrivers: vi.fn(() => of([])),
              getServicePoints: vi.fn(() => of([])),
              getDeliveryOrders: vi.fn(() => of([])),
              getWarehouses: vi.fn(() => of([])),
              getEntityData: vi.fn(() => of([])),
              getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
            },
          },
        ],
      });
      const fixture = TestBed.createComponent(RutasPage);
      fixture.detectChanges();
      const page = fixture.componentInstance;
      expect(getRoutePlanGeometry).toHaveBeenCalledTimes(2);

      // Primer reintento: solo p2, que sigue en rectas.
      vi.advanceTimersByTime(RutasPage.GEOMETRY_RETRY_DELAYS_MS[0]);
      expect(getRoutePlanGeometry).toHaveBeenCalledTimes(3);
      expect(getRoutePlanGeometry).toHaveBeenLastCalledWith('p2');

      // OSRM ya responde: p2 pasa a calles y no se programan más reintentos.
      p2Source = 'osrm';
      vi.advanceTimersByTime(RutasPage.GEOMETRY_RETRY_DELAYS_MS[1]);
      expect(getRoutePlanGeometry).toHaveBeenCalledTimes(4);
      expect(page.mapPolylines().every(l => !l.dashArray)).toBe(true);
      vi.advanceTimersByTime(60000);
      expect(getRoutePlanGeometry).toHaveBeenCalledTimes(4);
    } finally {
      vi.useRealTimers();
    }
  });
});

describe('RutasPage: publicar con conductor', () => {
  it('no publica sin conductor y, al elegirlo, envía la asignación del vehículo', () => {
    const publishRoutePlan = vi.fn(() => of({}));
    const proposed = plan('p1', 'VEH-01');
    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => true } },
        {
          provide: ApiService,
          useValue: {
            getRoutePlans: vi.fn(() => of([proposed])),
            getRoutePlanById: vi.fn(() => of({ ...proposed, stops: [] })),
            getRoutePlanGeometry: vi.fn(() => of(null)),
            getDrivers: vi.fn(() => of([{ accountId: 'drv-1', displayName: 'Conductor 1' }])),
            publishRoutePlan,
            getServicePoints: vi.fn(() => of([])),
            getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])),
            getEntityData: vi.fn(() => of([])),
            getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();
    const page = fixture.componentInstance;

    page.publishPlan('p1');
    expect(publishRoutePlan).not.toHaveBeenCalled();
    expect(page.feedbackMessage()?.text).toContain('Elige el conductor');

    page.selectedDriverId.set('drv-1');
    page.publishPlan('p1');
    expect(publishRoutePlan).toHaveBeenCalledWith('p1', { vehicleCode: 'VEH-01', driverAccountId: 'drv-1' });
  });
});
