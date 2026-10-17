import { describe, expect, it } from 'vitest';
import {
  COMPARISON_ROWS,
  DEMO_QUITO_FUEL_REFERENCE,
  DEMO_QUITO_PITCH_OPTIONS,
  formatCriticalCell,
  formatDriverCell,
  formatFuelCell,
  formatHelpersCell,
  formatKmCell,
  formatPendingOrdersCell,
  formatRisksCell,
  formatShrinkageCell,
  formatTotalCostCell,
  formatWearCell,
  RouteOptionComparisonData
} from './route-comparison.model';

describe('Comparación A/B/C en el Asistente de Rutas (#242)', () => {
  const optPropiaSample: RouteOptionComparisonData = {
    optionId: 'A',
    title: 'A · Propia',
    category: 'Propia',
    tagline: 'más barata',
    isRecommended: false,
    totalDistanceKm: 48,
    fuelCost: 14.20,
    wearCost: 8.40,
    driverCost: 24.00,
    helpersCount: 1,
    helpersCost: 12.00,
    expectedShrinkageCost: 6.50,
    totalCost: 65.10,
    criticalTotal: 3,
    criticalOnTime: 2,
    pendingOrdersCount: 1,
    risks: 'hora pico'
  };

  const optContratadaSample: RouteOptionComparisonData = {
    optionId: 'C',
    title: 'C · Contratada',
    category: 'Contratada',
    tagline: 'operador externo',
    isRecommended: false,
    totalDistanceKm: 48,
    fuelCostIncluded: true,
    wearCostIncluded: true,
    driverCostIncluded: true,
    helpersIncluded: true,
    expectedShrinkageCost: 5.00,
    totalCost: 85.00,
    criticalTotal: 3,
    criticalOnTime: 3,
    pendingOrdersCount: 0,
    risks: 'respuesta en 4 h'
  };

  const emptyDataOption: RouteOptionComparisonData = {
    optionId: 'X',
    title: 'X · Sin datos',
    category: 'Propia',
    tagline: 'desconocido',
    isRecommended: false,
    totalDistanceKm: null,
    fuelCost: null,
    wearCost: undefined,
    driverCost: null,
    helpersCount: null,
    expectedShrinkageCost: null,
    totalCost: null,
    criticalTotal: null,
    criticalOnTime: null,
    pendingOrdersCount: null,
    risks: null
  };

  describe('Regla: Celdas sin dato muestran «sin dato», NUNCA 0', () => {
    it('muestra «sin dato» en vez de 0 para km ausente o inválido', () => {
      expect(formatKmCell(emptyDataOption)).toBe('sin dato');
      expect(formatKmCell({ ...emptyDataOption, totalDistanceKm: 0 })).toBe('sin dato');
      expect(formatKmCell(optPropiaSample)).toBe('48 km');
    });

    it('muestra «sin dato» en vez de 0 para combustible, o «incluido en la tarifa» si es contratada', () => {
      expect(formatFuelCell(emptyDataOption)).toBe('sin dato');
      expect(formatFuelCell({ ...emptyDataOption, fuelCost: 0 })).toBe('sin dato');
      expect(formatFuelCell(optPropiaSample)).toBe('$ 14.20');
      expect(formatFuelCell(optContratadaSample)).toBe('incluido en la tarifa');
    });

    it('muestra «sin dato» en vez de 0 para desgaste, o «incluido en la tarifa» si es contratada', () => {
      expect(formatWearCell(emptyDataOption)).toBe('sin dato');
      expect(formatWearCell({ ...emptyDataOption, wearCost: 0 })).toBe('sin dato');
      expect(formatWearCell(optPropiaSample)).toBe('$ 8.40');
      expect(formatWearCell(optContratadaSample)).toBe('incluido en la tarifa');
    });

    it('muestra «sin dato» en vez de 0 para conductor, o «incluido en la tarifa» si es contratada', () => {
      expect(formatDriverCell(emptyDataOption)).toBe('sin dato');
      expect(formatDriverCell({ ...emptyDataOption, driverCost: 0 })).toBe('sin dato');
      expect(formatDriverCell(optPropiaSample)).toBe('$ 24.00');
      expect(formatDriverCell(optContratadaSample)).toBe('incluido en la tarifa');
    });

    it('muestra «sin dato» para ayudantes no registrados, o «del operador» si es contratada', () => {
      expect(formatHelpersCell(emptyDataOption)).toBe('sin dato');
      expect(formatHelpersCell(optContratadaSample)).toBe('del operador');
      expect(formatHelpersCell(optPropiaSample)).toBe('1 + 1 ayud. ($ 12.00)');
      expect(formatHelpersCell({ ...optPropiaSample, helpersCount: 0, helpersCost: 0 })).toBe('Solo conductor (0 ayud.)');
    });

    it('muestra «sin dato» en vez de 0 para merma esperada', () => {
      expect(formatShrinkageCell(emptyDataOption)).toBe('sin dato');
      expect(formatShrinkageCell({ ...emptyDataOption, expectedShrinkageCost: 0 })).toBe('sin dato');
      expect(formatShrinkageCell(optPropiaSample)).toBe('$ 6.50');
    });

    it('muestra «sin dato» en vez de 0 para costo total', () => {
      expect(formatTotalCostCell(emptyDataOption)).toBe('sin dato');
      expect(formatTotalCostCell({ ...emptyDataOption, totalCost: 0 })).toBe('sin dato');
      expect(formatTotalCostCell(optPropiaSample)).toBe('$ 65.10');
    });

    it('muestra «sin dato» para críticos cuando no hay datos', () => {
      expect(formatCriticalCell(emptyDataOption)).toBe('sin dato');
      expect(formatCriticalCell(optPropiaSample)).toBe('2 de 3');
      expect(formatCriticalCell(optContratadaSample)).toBe('3 de 3');
    });

    it('muestra «sin dato» para pedidos pendientes sin valor, y 0 sólo cuando explícitamente es cero', () => {
      expect(formatPendingOrdersCell(emptyDataOption)).toBe('sin dato');
      expect(formatPendingOrdersCell(optPropiaSample)).toBe('1 pedido');
      expect(formatPendingOrdersCell(optContratadaSample)).toBe('0');
      expect(formatPendingOrdersCell({ ...optPropiaSample, pendingOrdersCount: 2 })).toBe('2 pedidos');
    });

    it('muestra «sin dato» para riesgos ausentes, o el texto cuando existe', () => {
      expect(formatRisksCell(emptyDataOption)).toBe('sin dato');
      expect(formatRisksCell({ ...emptyDataOption, risks: '' })).toBe('sin dato');
      expect(formatRisksCell(optPropiaSample)).toBe('hora pico');
      expect(formatRisksCell(optContratadaSample)).toBe('respuesta en 4 h');
    });
  });

  describe('Dataset Pitch Demo (PR #239 / #200)', () => {
    it('incluye 3 opciones (A, B, C) con la opción B recomendada y justificada', () => {
      expect(DEMO_QUITO_PITCH_OPTIONS.length).toBe(3);
      const optA = DEMO_QUITO_PITCH_OPTIONS.find(o => o.optionId === 'A')!;
      const optB = DEMO_QUITO_PITCH_OPTIONS.find(o => o.optionId === 'B')!;
      const optC = DEMO_QUITO_PITCH_OPTIONS.find(o => o.optionId === 'C')!;

      expect(optA).toBeDefined();
      expect(optB).toBeDefined();
      expect(optC).toBeDefined();

      expect(optB.isRecommended).toBe(true);
      expect(optA.isRecommended).toBe(false);
      expect(optC.isRecommended).toBe(false);
      expect(optB.justification).toContain('cumple los 3 hospitales críticos');
    });

    it('incluye referencia fechada y fuente de combustible', () => {
      expect(DEMO_QUITO_FUEL_REFERENCE.pricePerGallon).toBeGreaterThan(0);
      expect(DEMO_QUITO_FUEL_REFERENCE.date).toBe('16-oct-2026');
      expect(DEMO_QUITO_FUEL_REFERENCE.source).toContain('Petroecuador');
    });

    it('contiene las 10 filas comparativas obligatorias requeridas por el issue #242', () => {
      const keys = COMPARISON_ROWS.map(r => r.key);
      expect(keys).toEqual([
        'km',
        'combustible',
        'desgaste',
        'conductor',
        'ayudantes',
        'merma',
        'totalCost',
        'criticos',
        'pendientes',
        'riesgos'
      ]);
      expect(keys.length).toBe(10);
    });
  });

  describe('Criterio de Aceptación: Si la API no trae opción C, muestra solo A y B sin romperse', () => {
    it('formatea correctamente cuando solo existen opciones A y B', () => {
      const onlyAB = DEMO_QUITO_PITCH_OPTIONS.slice(0, 2);
      expect(onlyAB.length).toBe(2);

      for (const row of COMPARISON_ROWS) {
        const valA = row.format(onlyAB[0]);
        const valB = row.format(onlyAB[1]);
        expect(valA).toBeTruthy();
        expect(valB).toBeTruthy();
        if (row.key !== 'pendientes') {
          expect(valA).not.toBe('0');
        }
      }
    });
  });
});
