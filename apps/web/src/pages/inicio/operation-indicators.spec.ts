import { describe, expect, it } from 'vitest';
import { ReceiptExceptionDto, ReceiptExceptionsResponse, RoutePlanSummaryDto, SimulationComparisonDetail, SimulationMetricDto } from '@shared/api';
import { operationIndicators } from './operation-indicators';

const comparisonFixture: SimulationComparisonDetail = {
  comparisonId: 'comparison-235', status: 'Completed', datasetVersion: 'synthetic-235',
  progressPercentage: 100, createdAtUtc: '2026-10-16T20:00:00Z', origin: 'computed', assumptions: [], aggregated: [],
  runs: [42, 101].map(seed => ({ id: `run-${seed}`, seed, scenarioCode: 'base', policyCode: 'istpetdev-v1', status: 'Completed', progressPercentage: 100 })),
  metrics: [
    { code: 'costo_transporte', policyCode: 'istpetdev-v1', numerator: 100, value: 100, unit: 'USD' },
    { code: 'km_total', policyCode: 'istpetdev-v1', numerator: 50, value: 50, unit: 'km' },
    { code: 'costo_transporte', policyCode: 'istpetdev-v1', numerator: 500, value: 500, unit: 'USD' },
    { code: 'km_total', policyCode: 'istpetdev-v1', numerator: 150, value: 150, unit: 'km' },
    { code: 'costo_transporte', policyCode: 'baseline', numerator: 9999, value: 9999, unit: 'USD' },
  ],
};

const plan = (duration: number, status = 'Published'): RoutePlanSummaryDto => ({
  id: 'route-235', code: 'R-235', revision: 1, status, vehicleId: 'vehicle-235',
  plannedStartUtc: '2026-10-16T20:00:00Z', totalDistanceKm: 50, totalDurationMinutes: duration, stopsCount: 2,
});
const receipts: ReceiptExceptionsResponse = {
  total: 101,
  items: [{ lines: [
    { quantityRejected: 7, rejectionReason: 'Daño' },
    { quantityRejected: 1, rejectionReason: 'Faltante' },
    { quantityRejected: 0, rejectionReason: 'Daño' },
    { quantityRejected: 2, adjustmentQuantity: 3, rejectionReason: 'Daño' },
  ] } as ReceiptExceptionDto],
};

describe('Indicadores de la operación (#235)', () => {
  it('mantiene seis filas en orden sin convertir ausencia en cero', () => {
    const rows = operationIndicators(null, null, null);
    expect(rows.map(row => row.name)).toEqual(['Costos/km', 'Faltantes', 'Servicio', 'Tiempo', 'Mermas', 'Exactitud']);
    expect(rows.every(row => row.value === null && row.source === 'sin dato')).toBe(true);
  });

  it('divide totales de la misma política sin emparejar métricas ni promediar razones', () => {
    const row = operationIndicators(comparisonFixture, null, null)[0];
    expect(row.value).toBe(3); // (100 + 500) / (50 + 150), no la media de 2 y 3.33.
    expect(row.source).toBe('simulación');
    expect(row.detail).toContain('comparison-235');
    expect(row.detail).toContain('semillas 42, 101');
    expect(row.detail).toContain('escenarios base');
    expect(row.detail).toContain('2 corridas');
  });

  it.each([undefined, null, NaN, Infinity, -1, 0])('no divide por km inválidos: %s', value => {
    const comparison = structuredClone(comparisonFixture);
    comparison.metrics.filter(metric => metric.code === 'km_total').forEach(metric => metric.value = value as number);
    expect(operationIndicators(comparison, null, null)[0].value).toBeNull();
  });

  it('rechaza métricas faltantes, no aplicables, unidades distintas y comparaciones incompletas', () => {
    for (const mutate of [
      (c: SimulationComparisonDetail) => c.metrics.splice(0, 1),
      (c: SimulationComparisonDetail) => c.metrics[0].notApplicableReason = 'not_simulated',
      (c: SimulationComparisonDetail) => c.metrics[0].unit = 'EUR',
      (c: SimulationComparisonDetail) => c.status = 'Running',
      (c: SimulationComparisonDetail) => c.runs[0].status = 'Failed',
      (c: SimulationComparisonDetail) => c.metrics[0].value = null as unknown as number,
    ]) {
      const comparison = structuredClone(comparisonFixture);
      mutate(comparison);
      expect(operationIndicators(comparison, null, null)[0].value).toBeNull();
    }
  });

  it('distingue costo y mermas realmente cero de datos ausentes', () => {
    const comparison = structuredClone(comparisonFixture);
    comparison.metrics.filter(metric => metric.code === 'costo_transporte').forEach(metric => metric.value = 0);
    const rows = operationIndicators(comparison, { items: [], total: 0 }, null);
    expect(rows[0].value).toBe(0);
    expect(rows[4].value).toBe(0);
    expect(rows[4].source).toBe('recepciones');
  });

  it('cuenta líneas con rechazo por Daño y declara el alcance parcial', () => {
    const rows = operationIndicators(null, receipts, null);
    expect(rows[4].value).toBe(2);
    expect(rows[4].detail).toContain('Conteo parcial');
    expect(rows[5].value).toBeNull(); // total de excepciones no es total de recepciones.
  });

  it('suma solo duraciones publicadas y no presenta un subtotal si falta alguna', () => {
    expect(operationIndicators(null, null, [plan(60), plan(90), plan(999, 'Draft')])[3].value).toBe(150);
    for (const duration of [0, NaN, -1, undefined, null]) {
      expect(operationIndicators(null, null, [plan(60), plan(duration as number)])[3].value).toBeNull();
    }
    expect(operationIndicators(null, null, [plan(90, 'Draft')])[3].value).toBeNull();
  });

  it('no sustituye servicio por puntualidad ni faltantes por quiebres totales', () => {
    const comparison = structuredClone(comparisonFixture);
    comparison.metrics.push(...['puntualidad', 'stockouts', 'entregas_completadas'].map(code => ({ code, value: 100 } as SimulationMetricDto)));
    const rows = operationIndicators(comparison, receipts, [plan(60)]);
    expect(rows[1].value).toBeNull();
    expect(rows[2].value).toBeNull();
    expect(rows[5].value).toBeNull();
  });
});
