import { describe, expect, it } from 'vitest';
import {
  COMPARISON_ROWS,
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

  describe('Regla: ausencia y cero son distintos', () => {
    it('distingue ausencia de cero explícito para km ausente o inválido', () => {
      expect(formatKmCell(emptyDataOption)).toBe('sin dato');
      expect(formatKmCell({ ...emptyDataOption, totalDistanceKm: 0 })).toBe('0 km');
      expect(formatKmCell(optPropiaSample)).toBe('48 km');
    });

    it('distingue ausencia de cero explícito para combustible, o «incluido en la tarifa» si es contratada', () => {
      expect(formatFuelCell(emptyDataOption)).toBe('sin dato');
      expect(formatFuelCell({ ...emptyDataOption, fuelCost: 0 })).toBe('$ 0.00');
      expect(formatFuelCell(optPropiaSample)).toBe('$ 14.20');
      expect(formatFuelCell(optContratadaSample)).toBe('incluido en la tarifa');
    });

    it('distingue ausencia de cero explícito para desgaste, o «incluido en la tarifa» si es contratada', () => {
      expect(formatWearCell(emptyDataOption)).toBe('sin dato');
      expect(formatWearCell({ ...emptyDataOption, wearCost: 0 })).toBe('$ 0.00');
      expect(formatWearCell(optPropiaSample)).toBe('$ 8.40');
      expect(formatWearCell(optContratadaSample)).toBe('incluido en la tarifa');
    });

    it('distingue ausencia de cero explícito para conductor, o «incluido en la tarifa» si es contratada', () => {
      expect(formatDriverCell(emptyDataOption)).toBe('sin dato');
      expect(formatDriverCell({ ...emptyDataOption, driverCost: 0 })).toBe('$ 0.00');
      expect(formatDriverCell(optPropiaSample)).toBe('$ 24.00');
      expect(formatDriverCell(optContratadaSample)).toBe('incluido en la tarifa');
    });

    it('muestra «sin dato» para ayudantes no registrados, o «del operador» si es contratada', () => {
      expect(formatHelpersCell(emptyDataOption)).toBe('sin dato');
      expect(formatHelpersCell(optContratadaSample)).toBe('del operador');
      expect(formatHelpersCell(optPropiaSample)).toBe('1 + 1 ayud. ($ 12.00)');
      expect(formatHelpersCell({ ...optPropiaSample, helpersCount: 0, helpersCost: 0 })).toBe('Solo conductor (0 ayud.)');
    });

    it('distingue ausencia de cero explícito para merma esperada', () => {
      expect(formatShrinkageCell(emptyDataOption)).toBe('sin dato');
      expect(formatShrinkageCell({ ...emptyDataOption, expectedShrinkageCost: 0 })).toBe('$ 0.00');
      expect(formatShrinkageCell(optPropiaSample)).toBe('$ 6.50');
    });

    it('distingue ausencia de cero explícito para costo total', () => {
      expect(formatTotalCostCell(emptyDataOption)).toBe('sin dato');
      expect(formatTotalCostCell({ ...emptyDataOption, totalCost: 0 })).toBe('$ 0.00');
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

  describe('Filas comparativas', () => {
    it('incluye el desglose de costos, tarifa y métricas de servicio', () => {
      expect(COMPARISON_ROWS.map(row => row.key)).toEqual(['km', 'combustible', 'desgaste', 'conductor', 'ayudantes', 'merma', 'tarifa', 'totalCost', 'criticos', 'pendientes', 'riesgos']);
    });
  });

  describe('Criterio de Aceptación: Si la API no trae opción C, muestra solo A y B sin romperse', () => {
    it('formatea correctamente cuando solo existen opciones A y B', () => {
      const onlyAB = [optPropiaSample, { ...optPropiaSample, optionId: 'B' }];
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
