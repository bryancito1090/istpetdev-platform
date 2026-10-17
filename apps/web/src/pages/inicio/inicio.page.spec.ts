import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, FieldExceptionsApiService, SimulationsApiService, SimulationComparisonDetail } from '@shared/api';
import { InicioPage } from './inicio.page';
const comparisonFixture: SimulationComparisonDetail = {
  comparisonId: 'comparison-235', status: 'Completed', datasetVersion: 'synthetic-235',
  progressPercentage: 100, createdAtUtc: '2026-10-16T20:00:00Z', origin: 'computed', assumptions: [], aggregated: [],
  runs: [{ id: 'run-42', seed: 42, scenarioCode: 'base', policyCode: 'istpetdev-v1', status: 'Completed', progressPercentage: 100 }],
  metrics: [
    { code: 'costo_transporte', policyCode: 'istpetdev-v1', numerator: 150, value: 150, unit: 'USD' },
    { code: 'km_total', policyCode: 'istpetdev-v1', numerator: 50, value: 50, unit: 'km' },
  ],
};

describe('InicioPage (FR-21)', () => {
  it('oculta al Auditor el enlace de escritura a Datos', () => {
    TestBed.configureTestingModule({
      imports: [InicioPage],
      providers: [
        provideRouter([]),
        { provide: SimulationsApiService, useValue: {} },
        { provide: FieldExceptionsApiService, useValue: {} },
        { provide: AuthSession, useValue: { canWrite: () => false } },
        { provide: ApiService, useValue: { getServicePoints: () => of([]), getRisks: () => of({ items: [] }) } },
      ],
    });
    const fixture = TestBed.createComponent(InicioPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a[href="/datos"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('1. Cargar Datos Maestros');
  });
});


describe('InicioPage: tarjeta de Gerencia (#235)', () => {
  function setup(role = 'Gerencia') {
    const simulations = {
      listComparisons: vi.fn(() => of([
        { comparisonId: 'older', status: 'Completed', createdAtUtc: '2026-10-15T00:00:00Z' },
        { comparisonId: 'running', status: 'Running', createdAtUtc: '2026-10-17T00:00:00Z' },
        { comparisonId: 'comparison-235', status: 'Completed', createdAtUtc: '2026-10-16T20:00:00Z' },
      ])),
      getComparison: vi.fn(() => of(comparisonFixture)),
    };
    const exceptions = { listReceiptExceptions: vi.fn(() => of({ items: [], total: 0 })) };
    TestBed.configureTestingModule({
      imports: [InicioPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { role: () => role, canWrite: () => role !== 'Auditor' } },
        { provide: ApiService, useValue: { getRoutePlans: () => of([]) } },
        { provide: SimulationsApiService, useValue: simulations },
        { provide: FieldExceptionsApiService, useValue: exceptions },
      ],
    });
    return { fixture: TestBed.createComponent(InicioPage), simulations, exceptions };
  }

  it('renderiza seis filas, unidad y procedencia de la última comparación completada', () => {
    const { fixture, simulations } = setup();
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('[data-operation-indicator]');
    expect(rows.length).toBe(6);
    expect(rows[0].textContent).toContain('3 USD/km');
    expect(rows[0].textContent).toContain('simulación');
    expect(rows[0].textContent).toContain('semillas 42');
    expect(rows[1].querySelector('dd').textContent.trim()).toBe('sin dato');
    expect(simulations.getComparison).toHaveBeenCalledWith('comparison-235');
  });

  it('conserva recepciones cuando falla simulación y permite reintentar sin dejar cifras anteriores', () => {
    const { fixture, simulations } = setup();
    fixture.detectChanges();
    simulations.listComparisons.mockReturnValue(throwError(() => new Error('offline')));
    fixture.componentInstance.refreshIndicators();
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('[data-operation-indicator]');
    expect(rows[0].querySelector('dd').textContent.trim()).toBe('sin dato');
    expect(rows[4].textContent).toContain('0 líneas rechazadas');
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain('reintentar');
    simulations.listComparisons.mockReturnValue(of([]));
    fixture.nativeElement.querySelector('section button').click();
    fixture.detectChanges();
    expect(fixture.componentInstance.indicatorError()).toBe('');
  });

  it('deja Mermas sin dato ante un error de recepciones, sin descartar simulación', () => {
    const { fixture, exceptions } = setup();
    exceptions.listReceiptExceptions.mockReturnValue(throwError(() => new Error('503')));
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('[data-operation-indicator]');
    expect(rows[0].textContent).toContain('3 USD/km');
    expect(rows[4].querySelector('dd').textContent.trim()).toBe('sin dato');
    expect(fixture.componentInstance.indicatorError()).toContain('recepciones');
  });

  it('detecta el error de rutas que el adaptador convierte en lista vacía', () => {
    const { fixture } = setup();
    Object.assign(TestBed.inject(ApiService), { loadErrors: () => ({ rutas: '503' }) });
    fixture.detectChanges();
    expect(fixture.componentInstance.indicators()[3].value).toBeNull();
    expect(fixture.componentInstance.indicatorError()).toContain('rutas');
  });

  it('mantiene las seis filas y skeletons durante la consulta y libera al destruir', () => {
    const { fixture, simulations } = setup();
    const pending = new Subject<[]>();
    simulations.listComparisons.mockReturnValue(pending);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[data-operation-indicator]').length).toBe(6);
    expect(fixture.nativeElement.querySelectorAll('[aria-label="Consultando indicador"]').length).toBe(6);
    fixture.destroy();
    expect(pending.observed).toBe(false);
  });

  it.each(['Auditor', 'Planificador'])('no consulta indicadores ni presenta la tarjeta para %s', role => {
    const { fixture, simulations, exceptions } = setup(role);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#operation-indicators-title')).toBeNull();
    expect(simulations.listComparisons).not.toHaveBeenCalled();
    expect(exceptions.listReceiptExceptions).not.toHaveBeenCalled();
  });
});
