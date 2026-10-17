import type { PlanningAlternative } from '@shared/api';

export const FLEET_COLORS = ['#00c2ff', '#00e5a3', '#c4a5ff', '#ffbd66', '#fb91b3', '#85baff'];

export const planningDecisionStorageKey = (accountId?: string) => `istpet.route-decision.${accountId || 'session'}`;

interface RouteComparisonMetrics {
  totalCost?: number | null;
  totalDistanceKm?: number | null;
  totalDurationMinutes?: number | null;
}

/** Describe métricas; no presupone que dos rutas atiendan los mismos destinos. */
export function routeComparisonLabels(route: RouteComparisonMetrics, routes: RouteComparisonMetrics[]): string[] {
  const labels: string[] = [];
  const value = (candidate: RouteComparisonMetrics, field: keyof RouteComparisonMetrics) => {
    const number = candidate[field];
    return number != null && Number.isFinite(number) && number >= 0 ? Math.round(number * 100) : null;
  };
  for (const [field, minimum, maximum] of [
    ['totalCost', 'Menor costo', 'Mayor costo'],
    ['totalDistanceKm', 'Menor distancia', null],
    ['totalDurationMinutes', 'Más rápida', null]
  ] as const) {
    const current = value(route, field);
    const numbers = routes.map(r => value(r, field)).filter((n): n is number => n !== null);
    if (current === null || numbers.length < 2) continue;
    const min = Math.min(...numbers), max = Math.max(...numbers);
    if (min === max) { if (field === 'totalCost') labels.push('Mismo costo'); continue; }
    const label = current === min ? minimum : current === max ? maximum : null;
    if (label) labels.push(label + (numbers.filter(n => n === current).length > 1 ? ' (empate)' : ''));
  }
  if (!labels.length) labels.push(route.totalCost == null ? 'Costo sin dato' : routes.filter(r => value(r, 'totalCost') !== null).length > 1 ? 'Costo intermedio' : 'Propuesta para revisión');
  return labels;
}

export function alternativeLabels(option: PlanningAlternative, options: PlanningAlternative[]): string[] {
  if (option.status === 'Discarded') return ['Alternativa descartada'];
  if (option.blockedReason && option.carrierType !== 'Contratada') return ['Sin recorrido viable'];
  const comparable = options.filter(o => o.pendingOrdersCount === option.pendingOrdersCount
    && (!o.blockedReason || o.carrierType === 'Contratada'));
  const metrics = (o: PlanningAlternative): RouteComparisonMetrics => ({ ...o,
    totalDurationMinutes: o.routes.length ? Math.max(...o.routes.map(r => r.evaluation.totalDurationMinutes)) : null });
  const labels = routeComparisonLabels(metrics(option), comparable.map(metrics));
  return option.pendingOrdersCount ? ['Cobertura parcial', ...labels] : labels;
}

export function assignedVehicleCount(option: PlanningAlternative): number {
  return new Set(option.routes.map(route => route.vehicleId)).size;
}
