import { describe, expect, it } from 'vitest';
import type { PlanningAlternative } from '@shared/api';
import { alternativeLabels, assignedVehicleCount, routeComparisonLabels } from './planning-decision.model';

const option = (id: string, cost: number | null, distance = 20): PlanningAlternative => ({
  optionId: id, title: id, description: '', isRecommended: false, carrierType: 'Propia', blockedReason: null,
  routes: [], totalCost: cost, totalDistanceKm: distance, pendingOrdersCount: 0, status: 'Available'
});

describe('Mensajes de alternativas de rutas', () => {
  it('distingue menor costo, menor distancia y mayor costo sin fijarlos a A/B/C', () => {
    const options = [option('A', 80, 20), option('B', 60, 30), option('C', 100, 40)];
    expect(options.map(o => alternativeLabels(o, options)[0])).toEqual(['Menor distancia', 'Menor costo', 'Mayor costo']);
  });
  it('reconoce empates visibles y no compara coberturas parciales con completas', () => {
    const options = [option('A', 20), option('B', 20.001), option('C', 40)];
    expect(alternativeLabels(options[0], options)).toContain('Menor costo (empate)');
    options[0].pendingOrdersCount = 1;
    expect(alternativeLabels(options[0], options)).toContain('Cobertura parcial');
    expect(alternativeLabels(options[1], options)).toContain('Menor costo');
  });
  it('mantiene el cero real y no clasifica datos ausentes como más económicos', () => {
    const options = [option('A', null), option('B', 0), option('C', 10)];
    expect(alternativeLabels(options[1], options)).toContain('Menor costo');
    expect(alternativeLabels(options[0], options)).not.toContain('Menor costo');
    expect(alternativeLabels(options[0], [options[0]])).toEqual(['Costo sin dato']);
  });
  it('explica costo y rapidez a la vez, sin asignarlos por letra', () => {
    const routes = [{ totalCost: 20, totalDistanceKm: 12, totalDurationMinutes: 60 },
      { totalCost: 40, totalDistanceKm: 8, totalDurationMinutes: 30 }];
    expect(routeComparisonLabels(routes[0], routes)).toEqual(['Menor costo']);
    expect(routeComparisonLabels(routes[1], routes)).toEqual(['Mayor costo', 'Menor distancia', 'Más rápida']);
    expect(routeComparisonLabels({ totalCost: null }, routes)).toEqual(['Costo sin dato']);
    const options = [option('A', 20), option('B', 40)];
    options[0].pendingOrdersCount = 1;
    expect(alternativeLabels(options[0], options)).toEqual(['Cobertura parcial', 'Propuesta para revisión']);
  });
  it('muestra empates sin inventar rapidez para una alternativa contratada sin itinerario', () => {
    const options = [option('A', 20), option('B', 20), option('C', 40)];
    options[2].carrierType = 'Contratada'; options[2].blockedReason = 'No operable';
    expect(alternativeLabels(options[0], options)).toContain('Menor costo (empate)');
    expect(alternativeLabels(options[2], options)).toContain('Mayor costo');
    expect(alternativeLabels(options[2], options)).not.toContain('Más rápida');
    expect(routeComparisonLabels({ totalCost: 5 }, [{ totalCost: 5 }, {}])).toEqual(['Propuesta para revisión']);
  });
  it('cuenta asignaciones reales distintas y conserva el estado de descarte', () => {
    const candidate = option('A', 20);
    candidate.routes = [{ vehicleId: 'v1' }, { vehicleId: 'v1' }, { vehicleId: 'v2' }] as PlanningAlternative['routes'];
    expect(assignedVehicleCount(candidate)).toBe(2);
    candidate.status = 'Discarded';
    expect(alternativeLabels(candidate, [candidate])).toEqual(['Alternativa descartada']);
  });
});
