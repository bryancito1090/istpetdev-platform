/**
 * Modelos, formateadores y datos demo para la comparación A/B/C en el Asistente de Rutas (#242).
 * Alineado con '02 Producto/Planificacion potenciada - propuesta del evento.md'.
 */

export interface RouteOptionComparisonData {
  optionId: string; // 'A' | 'B' | 'C'
  title: string;
  category: 'Propia' | 'Contratada';
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
}

export interface FuelReferencePrice {
  pricePerGallon: number;
  date: string;
  source: string;
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
  if (opt.totalDistanceKm == null || Number.isNaN(opt.totalDistanceKm) || opt.totalDistanceKm <= 0) {
    return 'sin dato';
  }
  return `${Math.round(opt.totalDistanceKm)} km`;
}

export function formatFuelCell(opt: RouteOptionComparisonData): string {
  if (opt.fuelCostIncluded) {
    return 'incluido en la tarifa';
  }
  if (opt.fuelCost == null || Number.isNaN(opt.fuelCost) || opt.fuelCost <= 0) {
    return 'sin dato';
  }
  return `$ ${opt.fuelCost.toFixed(2)}`;
}

export function formatWearCell(opt: RouteOptionComparisonData): string {
  if (opt.wearCostIncluded) {
    return 'incluido en la tarifa';
  }
  if (opt.wearCost == null || Number.isNaN(opt.wearCost) || opt.wearCost <= 0) {
    return 'sin dato';
  }
  return `$ ${opt.wearCost.toFixed(2)}`;
}

export function formatDriverCell(opt: RouteOptionComparisonData): string {
  if (opt.driverCostIncluded) {
    return 'incluido en la tarifa';
  }
  if (opt.driverCost == null || Number.isNaN(opt.driverCost) || opt.driverCost <= 0) {
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
  const costPart = opt.helpersCost != null && opt.helpersCost > 0 ? ` ($ ${opt.helpersCost.toFixed(2)})` : '';
  return `1 + ${opt.helpersCount} ayud.${costPart}`;
}

export function formatShrinkageCell(opt: RouteOptionComparisonData): string {
  if (opt.expectedShrinkageCost == null || Number.isNaN(opt.expectedShrinkageCost) || opt.expectedShrinkageCost <= 0) {
    return 'sin dato';
  }
  return `$ ${opt.expectedShrinkageCost.toFixed(2)}`;
}

export function formatTotalCostCell(opt: RouteOptionComparisonData): string {
  if (opt.totalCost == null || Number.isNaN(opt.totalCost) || opt.totalCost <= 0) {
    return 'sin dato';
  }
  return `$ ${opt.totalCost.toFixed(2)}`;
}

export function formatCriticalCell(opt: RouteOptionComparisonData): string {
  if (opt.criticalTotal == null || opt.criticalOnTime == null || opt.criticalTotal <= 0) {
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
  { key: 'totalCost', label: 'Costo total', format: formatTotalCostCell, isHighlight: true },
  { key: 'criticos', label: 'Críticos a tiempo', format: formatCriticalCell },
  { key: 'pendientes', label: 'Pedidos pendientes', format: formatPendingOrdersCell },
  { key: 'riesgos', label: 'Riesgos', format: formatRisksCell },
];

/**
 * Dataset sintético demo-quito-planificacion (Pitch #200 / PR #239).
 */
export const DEMO_QUITO_PITCH_OPTIONS: RouteOptionComparisonData[] = [
  {
    optionId: 'A',
    title: 'A · Propia',
    category: 'Propia',
    tagline: 'más barata',
    isRecommended: false,
    justification: 'Opción con menor costo directo, pero retrasa la entrega en Hospital San Francisco y supera la hora pico matutina.',
    totalDistanceKm: 48,
    fuelCost: 14.20,
    fuelCostIncluded: false,
    wearCost: 8.40,
    wearCostIncluded: false,
    driverCost: 24.00,
    driverCostIncluded: false,
    helpersCount: 1,
    helpersCost: 12.00,
    helpersIncluded: false,
    expectedShrinkageCost: 6.50,
    totalCost: 65.10,
    criticalTotal: 3,
    criticalOnTime: 2,
    pendingOrdersCount: 1,
    risks: 'hora pico (07:00–09:00)',
    operatorResponseTime: null
  },
  {
    optionId: 'B',
    title: 'B · Propia',
    category: 'Propia',
    tagline: 'críticos 1ro',
    isRecommended: true,
    justification: 'Cuesta $6.20 más que A, pero cumple los 3 hospitales críticos sin riesgo de mora y con 0 pedidos pendientes.',
    totalDistanceKm: 55,
    fuelCost: 16.50,
    fuelCostIncluded: false,
    wearCost: 9.60,
    wearCostIncluded: false,
    driverCost: 28.00,
    driverCostIncluded: false,
    helpersCount: 1,
    helpersCost: 14.00,
    helpersIncluded: false,
    expectedShrinkageCost: 3.20,
    totalCost: 71.30,
    criticalTotal: 3,
    criticalOnTime: 3,
    pendingOrdersCount: 0,
    risks: '—',
    operatorResponseTime: null
  },
  {
    optionId: 'C',
    title: 'C · Contratada',
    category: 'Contratada',
    tagline: 'operador externo',
    isRecommended: false,
    justification: 'Cubre el 100% de la demanda sin desgastar flota propia; tarifa base $35 + $0.80/km + $0.05/kg con respuesta en 4 h.',
    totalDistanceKm: 48,
    fuelCost: null,
    fuelCostIncluded: true,
    wearCost: null,
    wearCostIncluded: true,
    driverCost: null,
    driverCostIncluded: true,
    helpersCount: null,
    helpersCost: null,
    helpersIncluded: true,
    expectedShrinkageCost: 5.00,
    totalCost: 85.00,
    criticalTotal: 3,
    criticalOnTime: 3,
    pendingOrdersCount: 0,
    risks: 'respuesta en 4 h',
    operatorResponseTime: '4 h'
  }
];

export const DEMO_QUITO_FUEL_REFERENCE: FuelReferencePrice = {
  pricePerGallon: 2.40,
  date: '16-oct-2026',
  source: 'Petroecuador (Sintético demo)'
};
