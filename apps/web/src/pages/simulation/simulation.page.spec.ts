import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { SimulationsApiService, SimulationComparisonSummary, SimulationComparisonDetail } from '@shared/api';
import { SimulationPage } from './simulation.page';

describe('SimulationPage — simulador comparativo', () => {
  const sampleSummary: SimulationComparisonSummary = {
    comparisonId: '11111111-1111-1111-1111-111111111111',
    status: 'Completed',
    datasetVersion: 'demo-quito-planificacion',
    createdAtUtc: '2026-10-14T10:00:00Z',
    completedAtUtc: '2026-10-14T10:05:00Z',
    progressPercentage: 100,
    origin: 'computed',
    datasetHash: 'abcdef1234567890abcdef'
  };

  const policies = ['istpetdev-v1', 'reactive-v1', 'fixed7-v1'];
  const runs = [
    ...policies.map((policy, index) => ({
      id: `base-${index}`,
      seed: 42,
      scenarioCode: 'base',
      policyCode: policy,
      status: 'Completed',
      progressPercentage: 100
    })),
    ...policies.map((policy, index) => ({
      id: `adverse-${index}`,
      seed: 9999,
      scenarioCode: 'adverse',
      policyCode: policy,
      status: 'Completed',
      progressPercentage: 100
    }))
  ];

  const sampleDetail: SimulationComparisonDetail = {
    comparisonId: '11111111-1111-1111-1111-111111111111',
    status: 'Completed',
    datasetVersion: 'demo-quito-planificacion',
    progressPercentage: 100,
    createdAtUtc: '2026-10-14T10:00:00Z',
    completedAtUtc: '2026-10-14T10:05:00Z',
    origin: 'computed',
    hostName: 'dedicated-bench',
    commitHash: 'cbcd623',
    failureReason: undefined,
    assumptions: ['Dataset importado: demo-quito-planificacion', 'Quiebres por clave punto–SKU–día'],
    runs,
    aggregated: [
      { policy: 'istpetdev-v1', metricCode: 'costo_transporte', min: 110, median: 120, max: 130, adverseValue: 180 },
      { policy: 'reactive-v1', metricCode: 'costo_transporte', min: 90, median: 100, max: 110, adverseValue: 140 },
      { policy: 'fixed7-v1', metricCode: 'costo_transporte', min: 130, median: 140, max: 150, adverseValue: 190 },
      { policy: 'istpetdev-v1', metricCode: 'km_total', min: 80, median: 90, max: 100, adverseValue: 120 },
      { policy: 'reactive-v1', metricCode: 'km_total', min: 70, median: 80, max: 90, adverseValue: 100 },
      { policy: 'fixed7-v1', metricCode: 'km_total', min: 95, median: 100, max: 105, adverseValue: 130 },
      { policy: 'istpetdev-v1', metricCode: 'entregas_completadas', min: 550, median: 563, max: 570, adverseValue: 557 },
      { policy: 'reactive-v1', metricCode: 'entregas_completadas', min: 360, median: 365, max: 370, adverseValue: 364 },
      { policy: 'fixed7-v1', metricCode: 'entregas_completadas', min: 620, median: 624, max: 630, adverseValue: 624 },
      { policy: 'istpetdev-v1', metricCode: 'puntualidad', min: 71, median: 75.5, max: 80, adverseValue: 68 },
      { policy: 'istpetdev-v1', metricCode: 'quiebres_evitados_reactive-v1', min: 1, median: 2, max: 3, adverseValue: 1 },
      { policy: 'istpetdev-v1', metricCode: 'quiebres_nuevos_reactive-v1', min: 0, median: 1, max: 2, adverseValue: 0 },
      { policy: 'istpetdev-v1', metricCode: 'quiebres_diferencia_neta_reactive-v1', min: 0, median: 1, max: 2, adverseValue: 1 },
      { policy: 'istpetdev-v1', metricCode: 'demanda_no_atendida', sku: 'PRD-0001', min: 8, median: 12, max: 15, adverseValue: 20 },
      { policy: 'istpetdev-v1', metricCode: 'demanda_no_atendida', sku: 'PRD-0003', min: 2, median: 4, max: 6, adverseValue: 5 }
    ],
    metrics: [
      { code: 'puntualidad', policyCode: 'istpetdev-v1', seed: 42, scenarioCode: 'base', numerator: 75, denominator: 100, value: 75, unit: '%' },
      { code: 'demanda_no_atendida', policyCode: 'istpetdev-v1', sku: 'PRD-0001', seed: 42, scenarioCode: 'base', numerator: 12, denominator: undefined, value: 12, unit: 'L' },
      { code: 'demanda_no_atendida', policyCode: 'istpetdev-v1', sku: 'PRD-0003', seed: 42, scenarioCode: 'base', numerator: 4, denominator: undefined, value: 4, unit: 'kg' },
      { code: 'trazabilidad_completa', policyCode: 'istpetdev-v1', seed: 42, scenarioCode: 'base', numerator: 0, denominator: 0, value: undefined, unit: '%', notApplicableReason: 'not_simulated' }
    ],
    datasetHash: 'abcdef1234567890abcdef'
  };

  function configure(canWrite: boolean, detail: SimulationComparisonDetail = sampleDetail) {
    TestBed.configureTestingModule({
      imports: [SimulationPage],
      providers: [
        { provide: AuthSession, useValue: { canWrite: () => canWrite } },
        {
          provide: SimulationsApiService,
          useValue: {
            listComparisons: vi.fn(() => of([{ ...sampleSummary, status: detail.status, progressPercentage: detail.progressPercentage }])),
            getComparison: vi.fn(() => of(detail)),
            createComparison: vi.fn(),
            exportComparison: vi.fn(),
            importComparison: vi.fn()
          }
        }
      ]
    });
    const fixture = TestBed.createComponent(SimulationPage);
    fixture.detectChanges();
    return fixture;
  }

  it('oculta al Auditor las acciones de escritura y no muestra jerga interna', () => {
    const fixture = configure(false);
    const text = fixture.nativeElement.textContent;

    expect(text).not.toContain('Spec 005');
    expect(text).not.toContain('CE-05');
    expect(text).not.toContain('G3');
    expect(text).not.toContain('G4');
    expect(text).not.toContain('not_simulated');
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.some(b => /Nueva corrida|Importar/.test(b.textContent ?? ''))).toBe(false);
  });

  it('muestra acciones, métricas reales y comparación por política cuando puede escribir', () => {
    const fixture = configure(true);
    const text = fixture.nativeElement.textContent;

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.some(b => b.textContent?.includes('Nueva corrida'))).toBe(true);
    expect(text).toContain('Quiebres de stock');
    expect(text).toContain('Demanda no atendida');
    expect(text).toContain('Costo de transporte');
    expect(text).toContain('Puntualidad de demanda');
    expect(text).toContain('PRD-0001');
    expect(text).toContain('PRD-0003');
    expect(text).toContain('75.5 %');
    expect(text).toContain('12.00 L');
    expect(text).toContain('4.00 kg');
    expect(text).not.toContain('SKU-01');
    expect(text).not.toContain('Sin quiebres colapsados');
    expect(text).toContain('6/6 corridas completadas');
    expect(text).toContain('Semilla 42');
    expect(text).toContain('Semilla 9999');
  });

  it('cambia la comparación al elegir la política de referencia', () => {
    const fixture = configure(true);
    expect(fixture.nativeElement.textContent).toContain('20.0 % más que Reactiva');

    const fixedButton = (Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[])
      .find(button => button.textContent?.includes('Semanal fija'));
    expect(fixedButton).toBeTruthy();
    fixedButton!.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('14.3 % menos que Semanal fija');
    const text = fixture.nativeElement.textContent;
    expect(text).not.toContain('20.0 % más que Reactiva');
  });

  it('muestra "Sin dato" cuando la corrida no publicó métricas comparadas', () => {
    const fixture = configure(true, {
      ...sampleDetail,
      aggregated: [],
      metrics: []
    });
    const text = fixture.nativeElement.textContent;

    expect(text).toContain('Sin dato');
    expect(text).not.toContain('0 quiebres');
    expect(text).not.toContain('100.0 %');
  });

  it('muestra barra de progreso y no revela cifras si la simulación está en ejecución', () => {
    const fixture = configure(true, {
      ...sampleDetail,
      status: 'Running',
      progressPercentage: 45,
      aggregated: [],
      metrics: []
    });
    const text = fixture.nativeElement.textContent;

    expect(text).toContain('Simulación en ejecución');
    expect(text).toContain('45%');
    expect(text).not.toContain('Quiebres de stock');
    expect(text).not.toContain('Puntualidad de demanda');
  });
});
