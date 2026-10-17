import { describe, expect, it } from 'vitest';
import { mapSourcingComparison, formatFuelCell, formatHelpersCell, formatTotalCostCell, isValidSavedRouteComparisonRun } from './route-comparison.model';

describe('Comparación calculada #276', () => {
  it('conserva ausencia, cero y recomendación explícita sin inferir por letra', () => {
    const [a, b] = mapSourcingComparison({ options: [
      { optionId: 'A', title: 'A', costBreakdown: { fuel: 0, total: 0 } },
      { optionId: 'B', title: 'B' }
    ] });
    expect(formatFuelCell(a)).toBe('$ 0.00');
    expect(formatTotalCostCell(a)).toBe('$ 0.00');
    expect(formatFuelCell(b)).toBe('sin dato');
    expect(formatHelpersCell(b)).toBe('sin dato');
    expect(b.pendingOrdersCount).toBeUndefined();
    expect(b.criticalTotal).toBeUndefined();
    expect([a, b].some(o => o.isRecommended)).toBe(false);
    expect(b.justification).toBeUndefined();
  });

  it('mapea C por modalidad contractual y conserva tarifa, riesgos, notas y ganador', () => {
    const [option] = mapSourcingComparison({
      recommendedOptionId: 'externa-7', justification: 'Justificación calculada',
      options: [{ optionId: 'externa-7', title: 'Proveedor', carrierType: 'Contratada',
        totalKm: 19, criticalDeliveriesTotal: 2, criticalDeliveriesOnTime: 1,
        pendingOrdersCount: 0, risks: ['Respuesta contractual'],
        costBreakdown: { carrierTariff: 29, total: 31, shrink: 2, notes: ['Costo estimado'] } }]
    });
    expect(option.category).toBe('Contratada');
    expect(option.carrierTariff).toBe(29);
    expect(option.totalCost).toBe(31);
    expect(option.isRecommended).toBe(true);
    expect(option.justification).toBe('Justificación calculada');
    expect(option.risks).toBe('Respuesta contractual');
    expect(option.notes).toEqual(['Costo estimado']);
    // La modalidad por sí sola no acredita qué componentes incluye una tarifa.
    expect(option.fuelCostIncluded).toBeUndefined();
    expect(formatFuelCell(option)).toBe('sin dato');
  });

  it('valida una corrida guardada por metadatos y contrato del motor', () => {
    const run = {
      metadata: {
        commit: 'abc123', capturedAtUtc: '2026-10-16T15:00:00Z', fixture: 'demo-quito-planificacion',
        configSha256: 'a'.repeat(64)
      },
      response: {
        options: [{ optionId: 'A' }],
        comparison: {
          fuelPrice: { value: 3.11, date: '2026-10-16', source: 'Primicias' },
          options: [{ optionId: 'A' }, { optionId: 'C', carrierType: 'Contratada' }]
        }
      }
    };
    expect(isValidSavedRouteComparisonRun(run)).toBe(true);
    expect(isValidSavedRouteComparisonRun({ ...run, metadata: { ...run.metadata, configSha256: 'mal' } })).toBe(false);
    expect(isValidSavedRouteComparisonRun({ ...run, response: { ...run.response, comparison: { ...run.response.comparison, options: [{ optionId: 'A' }] } } })).toBe(false);
  });
});
