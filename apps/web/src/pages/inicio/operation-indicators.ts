import { ReceiptExceptionsResponse, RoutePlanSummaryDto, SimulationComparisonDetail } from '@shared/api';

export interface OperationIndicator {
  name: string;
  value: number | null;
  unit: string;
  source: 'simulación' | 'recepciones' | 'rutas publicadas' | 'sin dato';
  detail: string;
}

export function operationIndicators(
  comparison: SimulationComparisonDetail | null,
  receipts: ReceiptExceptionsResponse | null,
  plans: RoutePlanSummaryDto[] | null,
): OperationIndicator[] {
  const rows: OperationIndicator[] = [
    { name: 'Costos/km', value: null, unit: 'USD/km', source: 'sin dato', detail: 'Sin comparación completada con costo y kilómetros válidos.' },
    { name: 'Faltantes', value: null, unit: 'quiebres evitados', source: 'sin dato', detail: 'La API de comparación no expone los quiebres evitados frente al escenario base.' },
    { name: 'Servicio', value: null, unit: '%', source: 'sin dato', detail: 'La API no ofrece un total completo de pedidos entregados y planificados del mismo período.' },
    { name: 'Tiempo', value: null, unit: 'min estimados', source: 'sin dato', detail: 'Sin rutas publicadas con duración estimada disponible.' },
    { name: 'Mermas', value: null, unit: 'líneas rechazadas por Daño', source: 'sin dato', detail: 'No se pudo consultar las recepciones con novedades.' },
    { name: 'Exactitud', value: null, unit: '%', source: 'sin dato', detail: 'La API de excepciones omite las recepciones sin novedades y el total de recepciones.' },
  ];

  if (comparison?.status === 'Completed') {
    const runs = comparison.runs.filter(run => run.policyCode === 'istpetdev-v1');
    const costs = comparison.metrics.filter(metric => metric.policyCode === 'istpetdev-v1' && metric.code === 'costo_transporte');
    const distances = comparison.metrics.filter(metric => metric.policyCode === 'istpetdev-v1' && metric.code === 'km_total');
    const valid = (metrics: typeof costs, unit: string) => metrics.length === runs.length && metrics.every(metric =>
      metric.unit === unit && !metric.notApplicableReason && typeof metric.value === 'number' && Number.isFinite(metric.value) && metric.value >= 0);
    // El DTO no identifica la corrida de cada métrica: sumar la misma política evita emparejamientos inventados.
    if (runs.length > 0 && runs.every(run => run.status === 'Completed') && valid(costs, 'USD') && valid(distances, 'km')) {
      const cost = costs.reduce((sum, metric) => sum + metric.value!, 0);
      const km = distances.reduce((sum, metric) => sum + metric.value!, 0);
      if (km > 0 && Number.isFinite(cost / km)) {
        rows[0] = {
          ...rows[0], value: cost / km, source: 'simulación',
          detail: `Comparación ${comparison.comparisonId} · ${comparison.datasetVersion} · istpetdev-v1 · semillas ${[...new Set(runs.map(run => run.seed))].join(', ')} · escenarios ${[...new Set(runs.map(run => run.scenarioCode))].join(', ')} · costo total ÷ km totales de ${runs.length} corridas.`,
        };
      }
    }
  }

  const published = plans?.filter(plan => plan.status === 'Published') ?? [];
  if (published.length > 0 && published.every(plan => Number.isFinite(plan.totalDurationMinutes) && plan.totalDurationMinutes > 0)) {
    const minutes = published.reduce((sum, plan) => sum + plan.totalDurationMinutes, 0);
    if (Number.isFinite(minutes)) {
      rows[3] = {
        ...rows[3], value: minutes, source: 'rutas publicadas',
        detail: `Rutas publicadas consultadas: ${published.length} (hasta 50). Suma de tiempos estimados; no es tiempo real de viaje.`,
      };
    }
  }

  if (receipts) {
    rows[4] = {
      ...rows[4], source: 'recepciones',
      value: receipts.items.reduce((sum, receipt) => sum + receipt.lines.filter(line => line.quantityRejected > 0 && line.rejectionReason === 'Daño').length, 0),
      detail: `Recepciones con novedades consultadas: ${receipts.items.length} de ${receipts.total}.${receipts.total > receipts.items.length ? ' Conteo parcial: recepciones más recientes (máximo 100).' : ''} No mide unidades ni costo de la merma.`,
    };
  }

  return rows;
}
