import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { DemoSimulationComponent } from './demo-simulation.component';
import { FIELD_API } from '../../field-api.interface';
import { TodayRoutePlanDto, DemoAdvanceResponse } from '@istpetdev/shared-core';

describe('DemoSimulationComponent #320', () => {
  let component: DemoSimulationComponent;
  let routerMock: { navigate: ReturnType<typeof vi.fn> };
  let fieldApiMock: {
    getTodayRoutePlans: ReturnType<typeof vi.fn>;
    advanceDemo: ReturnType<typeof vi.fn>;
  };

  const sampleRoute: TodayRoutePlanDto = {
    routePlanId: 'rp-today-1',
    code: 'RP-20261017-001',
    vehicleCode: 'VEH-01',
    driverName: 'Conductor Demo',
    status: 'Published',
    totalStops: 4,
    completedStops: 1,
    nextCheckpoint: 'Parada 2: Centro de Salud',
    plannedStartUtc: '2026-10-17T08:00:00Z',
    stops: [
      {
        stopId: 'stop-1',
        sequenceNumber: 1,
        kind: 'Delivery',
        pointName: 'Hospital Central',
        status: 'Completed',
        plannedArrivalUtc: '2026-10-17T08:30:00Z',
        actualArrivalUtc: '2026-10-17T08:30:00Z'
      },
      {
        stopId: 'stop-2',
        sequenceNumber: 2,
        kind: 'Delivery',
        pointName: 'Centro de Salud',
        status: 'Pending',
        plannedArrivalUtc: '2026-10-17T09:15:00Z'
      }
    ]
  };

  beforeEach(() => {
    routerMock = { navigate: vi.fn() };
    fieldApiMock = {
      getTodayRoutePlans: vi.fn(() => of([sampleRoute])),
      advanceDemo: vi.fn(() => of({
        completedStops: [
          { sequence: 2, deliveryId: 'stop-2', receiptId: 'rcpt-1', acceptedAtUtc: '2026-10-17T09:15:00Z' }
        ],
        checkpointCompleted: 'Parada 2 completada',
        nextCheckpoint: 'Parada 3: Dispensario Norte',
        totalStops: 4,
        completedStopsCount: 2,
        status: 'Success'
      } as DemoAdvanceResponse))
    };

    TestBed.configureTestingModule({
      imports: [DemoSimulationComponent],
      providers: [
        { provide: Router, useValue: routerMock },
        { provide: FIELD_API, useValue: fieldApiMock }
      ]
    });

    const fixture = TestBed.createComponent(DemoSimulationComponent);
    component = fixture.componentInstance;
  });

  it('debe cargar y listar las rutas publicadas del día al inicializarse', () => {
    component.ngOnInit();
    expect(fieldApiMock.getTodayRoutePlans).toHaveBeenCalled();
    expect(component.routes().length).toBe(1);
    expect(component.routes()[0].code).toBe('RP-20261017-001');
    expect(component.isCompleted(component.routes()[0])).toBe(false);
    expect(component.isInProgress(component.routes()[0])).toBe(true);
  });

  it('debe ejecutar modo completo llamando advanceDemo con mode=complete y withIncidents', () => {
    component.ngOnInit();
    component.withIncidents.set(true);
    component.runMode(component.routes()[0], 'complete');

    expect(fieldApiMock.advanceDemo).toHaveBeenCalledWith(
      'rp-today-1',
      expect.objectContaining({
        mode: 'complete',
        withIncidents: true,
        noveltyRate: 0.25
      })
    );
    expect(component.lastResult()?.type).toBe('success');
  });

  it('debe ejecutar modo media ruta llamando advanceDemo con mode=half', () => {
    component.ngOnInit();
    component.runMode(component.routes()[0], 'half');

    expect(fieldApiMock.advanceDemo).toHaveBeenCalledWith(
      'rp-today-1',
      expect.objectContaining({
        mode: 'half',
        withIncidents: true
      })
    );
    expect(component.lastResult()?.type).toBe('success');
  });

  it('debe ejecutar modo paso a paso sin novedades aleatorias (withIncidents=false)', () => {
    component.ngOnInit();
    component.withIncidents.set(true); // Aunque esté marcado el checkbox, next nunca lleva incidentes
    component.runMode(component.routes()[0], 'next');

    expect(fieldApiMock.advanceDemo).toHaveBeenCalledWith(
      'rp-today-1',
      expect.objectContaining({
        mode: 'next',
        withIncidents: false
      })
    );
    expect(component.lastResult()?.type).toBe('success');
  });

  it('debe manejar errores de simulación y mostrar mensaje de retroalimentación', () => {
    fieldApiMock.advanceDemo.mockReturnValueOnce(throwError(() => ({
      error: { detail: 'Simulación de campo deshabilitada.' }
    })));

    component.ngOnInit();
    component.runMode(component.routes()[0], 'complete');

    expect(component.lastResult()?.type).toBe('error');
    expect(component.lastResult()?.message).toContain('deshabilitada');
  });

  it('debe navegar hacia atrás al presionar goBack', () => {
    component.goBack();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/home']);
  });
});
