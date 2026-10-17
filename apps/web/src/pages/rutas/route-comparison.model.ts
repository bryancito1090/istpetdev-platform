/**
 * Modelos, formateadores y datos demo para la comparación A/B/C en el Asistente de Rutas (#242).
 * Alineado con '02 Producto/Planificacion potenciada - propuesta del evento.md'.
 */

import type { SavedRouteComparisonRunDto, SourcingComparisonDto, RouteSuggestionOptionDto } from '@shared/api';

export interface RouteOptionComparisonData {
  optionId: string; // 'A' | 'B' | 'C'
  title: string;
  category: 'Propia' | 'Contratada' | 'sin dato';
  tagline: string;
  isRecommended: boolean;
  justification?: string;
  totalDistanceKm?: number | null;
  fuelCost?: number | null;
  fuelCostIncluded?: boolean;
  wearCost?: number | null;
  wearCostIncluded?: boolean;
  driverCost?: number | null;
  driverCostIncluded?: boolean;
  helpersCount?: number | null;
  helpersCost?: number | null;
  helpersIncluded?: boolean;
  expectedShrinkageCost?: number | null;
  totalCost?: number | null;
  criticalTotal?: number | null;
  criticalOnTime?: number | null;
  pendingOrdersCount?: number | null;
  risks?: string | null;
  operatorResponseTime?: string | null;
  carrierTariff?: number | null;
  notes?: string[];
}

/** Mapeo único de la respuesta calculada, reutilizable por una corrida guardada. */
export function mapSourcingComparison(result: SourcingComparisonDto): RouteOptionComparisonData[] {
  return result.options.map(opt => {
    const bd = opt.costBreakdown;
    const recommended = result.recommendedOptionId != null
      ? opt.optionId === result.recommendedOptionId : opt.isRecommended === true;
    return {
      optionId: opt.optionId,
      title: opt.title,
      category: opt.carrierType === 'Propia' || opt.carrierType === 'Contratada' ? opt.carrierType : 'sin dato',
      tagline: opt.description || '',
      isRecommended: recommended,
      justification: recommended ? opt.justification ?? result.justification ?? undefined : undefined,
      totalDistanceKm: opt.totalKm,
      fuelCost: bd?.fuel,
      wearCost: bd?.wear,
      driverCost: bd?.driver,
      helpersCount: bd?.helperCount,
      helpersCost: bd?.helpers,
      expectedShrinkageCost: bd?.shrink,
      totalCost: bd?.total ?? opt.totalCost,
      carrierTariff: bd?.carrierTariff,
      criticalTotal: opt.criticalDeliveriesTotal,
      criticalOnTime: opt.criticalDeliveriesOnTime,
      pendingOrdersCount: opt.pendingOrdersCount,
      risks: opt.risks?.join(' · '),
      notes: bd?.notes ?? []
    };
  });
}

/** Compatibilidad con API anterior: mostrar solo métricas efectivamente recibidas. */
export function mapRouteSuggestion(opt: RouteSuggestionOptionDto): RouteOptionComparisonData {
  return {
    optionId: opt.sourcingOptionId ?? opt.optionId,
    title: opt.title,
    category: 'sin dato',
    tagline: opt.description,
    isRecommended: false,
    totalDistanceKm: opt.evaluation?.totalDistanceKm,
    totalCost: opt.evaluation?.totalCost,
    notes: ['La API no entregó el contrato comparativo de abastecimiento.']
  };
}

export function isValidSavedRouteComparisonRun(value: unknown): value is SavedRouteComparisonRunDto {
  if (!value || typeof value !== 'object') return false;
  const run = value as Partial<SavedRouteComparisonRunDto>;
  const metadata = run.metadata;
  const comparison = run.response?.comparison;
  return metadata?.fixture === 'demo-quito-planificacion'
    && typeof metadata.commit === 'string' && metadata.commit.length > 0
    && typeof metadata.capturedAtUtc === 'string' && !Number.isNaN(Date.parse(metadata.capturedAtUtc))
    && typeof metadata.configSha256 === 'string' && /^[a-f\d]{64}$/i.test(metadata.configSha256)
    && Array.isArray(run.response?.options) && run.response.options.length > 0
    && Array.isArray(comparison?.options) && comparison.options.length > 0
    && comparison.options.some(option => option.carrierType === 'Contratada')
    && comparison.fuelPrice?.value != null && typeof comparison.fuelPrice.date === 'string'
    && typeof comparison.fuelPrice.source === 'string';
}

export interface ComparisonRowDefinition {
  key: string;
  label: string;
  format: (opt: RouteOptionComparisonData) => string;
  isHighlight?: boolean;
}

/**
 * Regla de negocio #242:
 * Celdas sin dato: «sin dato», nunca 0.
 */
export function formatKmCell(opt: RouteOptionComparisonData): string {
  if (opt.totalDistanceKm == null || Number.isNaN(opt.totalDistanceKm) || opt.totalDistanceKm < 0) {
    return 'sin dato';
  }
  return `${Math.round(opt.totalDistanceKm)} km`;
}

export function formatFuelCell(opt: RouteOptionComparisonData): string {
  if (opt.fuelCostIncluded) {
    return 'incluido en la tarifa';
  }
  if (opt.fuelCost == null || Number.isNaN(opt.fuelCost) || opt.fuelCost < 0) {
    return 'sin dato';
  }
  return `$ ${opt.fuelCost.toFixed(2)}`;
}

export function formatWearCell(opt: RouteOptionComparisonData): string {
  if (opt.wearCostIncluded) {
    return 'incluido en la tarifa';
  }
  if (opt.wearCost == null || Number.isNaN(opt.wearCost) || opt.wearCost < 0) {
    return 'sin dato';
  }
  return `$ ${opt.wearCost.toFixed(2)}`;
}

export function formatDriverCell(opt: RouteOptionComparisonData): string {
  if (opt.driverCostIncluded) {
    return 'incluido en la tarifa';
  }
  if (opt.driverCost == null || Number.isNaN(opt.driverCost) || opt.driverCost < 0) {
    return 'sin dato';
  }
  return `$ ${opt.driverCost.toFixed(2)}`;
}

export function formatHelpersCell(opt: RouteOptionComparisonData): string {
  if (opt.helpersIncluded) {
    return 'del operador';
  }
  if (opt.helpersCount == null || Number.isNaN(opt.helpersCount)) {
    return 'sin dato';
  }
  if (opt.helpersCount === 0) {
    return 'Solo conductor (0 ayud.)';
  }
  const costPart = opt.helpersCost != null && opt.helpersCost >= 0 ? ` ($ ${opt.helpersCost.toFixed(2)})` : '';
  return `1 + ${opt.helpersCount} ayud.${costPart}`;
}

export function formatShrinkageCell(opt: RouteOptionComparisonData): string {
  if (opt.expectedShrinkageCost == null || Number.isNaN(opt.expectedShrinkageCost) || opt.expectedShrinkageCost < 0) {
    return 'sin dato';
  }
  return `$ ${opt.expectedShrinkageCost.toFixed(2)}`;
}

export function formatTotalCostCell(opt: RouteOptionComparisonData): string {
  if (opt.totalCost == null || Number.isNaN(opt.totalCost) || opt.totalCost < 0) {
    return 'sin dato';
  }
  return `$ ${opt.totalCost.toFixed(2)}`;
}

export function formatCriticalCell(opt: RouteOptionComparisonData): string {
  if (opt.criticalTotal == null || opt.criticalOnTime == null || opt.criticalTotal < 0) {
    return 'sin dato';
  }
  return `${opt.criticalOnTime} de ${opt.criticalTotal}`;
}

export function formatPendingOrdersCell(opt: RouteOptionComparisonData): string {
  if (opt.pendingOrdersCount == null || Number.isNaN(opt.pendingOrdersCount)) {
    return 'sin dato';
  }
  if (opt.pendingOrdersCount === 0) {
    return '0';
  }
  if (opt.pendingOrdersCount === 1) {
    return '1 pedido';
  }
  return `${opt.pendingOrdersCount} pedidos`;
}

export function formatRisksCell(opt: RouteOptionComparisonData): string {
  if (opt.risks == null || opt.risks.trim() === '') {
    return 'sin dato';
  }
  return opt.risks;
}

export const COMPARISON_ROWS: ComparisonRowDefinition[] = [
  { key: 'km', label: 'Kilómetros', format: formatKmCell },
  { key: 'combustible', label: 'Combustible', format: formatFuelCell },
  { key: 'desgaste', label: 'Desgaste vehículo', format: formatWearCell },
  { key: 'conductor', label: 'Conductor', format: formatDriverCell },
  { key: 'ayudantes', label: 'Ayudantes (n.º y costo)', format: formatHelpersCell },
  { key: 'merma', label: 'Merma esperada', format: formatShrinkageCell },
  { key: 'tarifa', label: 'Tarifa operador', format: opt => opt.carrierTariff == null ? 'sin dato' : '$ ' + opt.carrierTariff.toFixed(2) },
  { key: 'totalCost', label: 'Costo total', format: formatTotalCostCell, isHighlight: true },
  { key: 'criticos', label: 'Críticos a tiempo', format: formatCriticalCell },
  { key: 'pendientes', label: 'Pedidos pendientes', format: formatPendingOrdersCell },
  { key: 'riesgos', label: 'Riesgos', format: formatRisksCell },
];
