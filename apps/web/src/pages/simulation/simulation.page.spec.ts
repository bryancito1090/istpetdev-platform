import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { SimulationsApiService, SimulationComparisonSummary, SimulationComparisonDetail } from '@shared/api';
import { SimulationPage } from './simulation.page';

describe('SimulationPage (Spec 005 - T9 & T10)', () => {
  const sampleSummary: SimulationComparisonSummary = {
    comparisonId: '11111111-1111-1111-1111-111111111111',
    status: 'Completed',
    datasetVersion: 'national-v1',
    createdAtUtc: '2026-10-14T10:00:00Z',
    completedAtUtc: '2026-10-14T10:05:00Z',
    progressPercentage: 100,
    origin: 'computed'
  };

  const sampleDetail: SimulationComparisonDetail = {
    comparisonId: '11111111-1111-1111-1111-111111111111',
    status: 'Completed',
    datasetVersion: 'national-v1',
    progressPercentage: 100,
    createdAtUtc: '2026-10-14T10:00:00Z',
    completedAtUtc: '2026-10-14T10:05:00Z',
    origin: 'computed',
    hostName: 'dedicated-bench',
    commitHash: 'cbcd623',
    failureReason: undefined,
    assumptions: ['Matriz vial precargada / aproximación euclidiana etiquetada'],
    runs: [
      { id: 'run-1', seed: 42, scenarioCode: 'base', policyCode: 'istpetdev-v1', status: 'Completed', progressPercentage: 100 }
    ],
    aggregated: [
      { policy: 'istpetdev-v1', metricCode: 'costo_transporte', min: 100, median: 120, max: 150, adverseValue: 180 },
      { policy: 'istpetdev-v1', metricCode: 'km_total', min: 80, median: 90, max: 100, adverseValue: 120 }
    ],
    metrics: []
  };

  it('oculta al Auditor los botones de acción para lanzar e importar corridas', () => {
    TestBed.configureTestingModule({
      imports: [SimulationPage],
      providers: [
        { provide: AuthSession, useValue: { canWrite: () => false } },
        {
          provide: SimulationsApiService,
          useValue: {
            listComparisons: vi.fn(() => of([sampleSummary])),
            getComparison: vi.fn(() => of(sampleDetail))
          }
        }
      ]
    });

    const fixture = TestBed.createComponent(SimulationPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Detalle de 1 corridas del comparador');
    expect(fixture.nativeElement.textContent).not.toContain('1080 jornadas');
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.some(b => /Nueva Corrida|Importar/.test(b.textContent ?? ''))).toBe(false);
  });

  it('muestra botones de acción cuando el usuario tiene permisos de escritura', () => {
    TestBed.configureTestingModule({
      imports: [SimulationPage],
      providers: [
        { provide: AuthSession, useValue: { canWrite: () => true } },
        {
          provide: SimulationsApiService,
          useValue: {
            listComparisons: vi.fn(() => of([sampleSummary])),
            getComparison: vi.fn(() => of(sampleDetail))
          }
        }
      ]
    });

    const fixture = TestBed.createComponent(SimulationPage);
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.some(b => b.textContent?.includes('Nueva Corrida'))).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Quiebres de Stock');
    expect(fixture.nativeElement.textContent).toContain('Demanda No Atendida');
    expect(fixture.nativeElement.textContent).toContain('Costo de Transporte');
    expect(fixture.nativeElement.textContent).toContain('Puntualidad & Trazabilidad');
    expect(fixture.nativeElement.textContent).toContain('Escenario Adverso (Seed 9999)');
  });

  it('muestra barra de progreso y no revela cifras fijas falsas si la simulación está en ejecución', () => {
    const inProgressDetail: SimulationComparisonDetail = {
      ...sampleDetail,
      status: 'Running',
      progressPercentage: 45
    };

    TestBed.configureTestingModule({
      imports: [SimulationPage],
      providers: [
        { provide: AuthSession, useValue: { canWrite: () => true } },
        {
          provide: SimulationsApiService,
          useValue: {
            listComparisons: vi.fn(() => of([{ ...sampleSummary, status: 'Running', progressPercentage: 45 }])),
            getComparison: vi.fn(() => of(inProgressDetail))
          }
        }
      ]
    });

    const fixture = TestBed.createComponent(SimulationPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Simulación en ejecución (90 días)');
    expect(fixture.nativeElement.textContent).toContain('45%');
  });
});
