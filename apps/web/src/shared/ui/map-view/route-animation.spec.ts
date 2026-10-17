import { describe, expect, it } from 'vitest';
import {
  buildExplorationSpurs,
  distanceMeters,
  downsampleRoute,
  indexAtFraction,
  progressiveRoute,
  type LatLng
} from './route-animation';

function straightRoute(points: number): LatLng[] {
  return Array.from({ length: points }, (_, i) => [0, i / 1000] as LatLng);
}

describe('route-animation', () => {
  it('interpela el avance de un tramo incluso con solo dos coordenadas', () => {
    const route: LatLng[] = [[0, 0], [0, 2]];
    expect(progressiveRoute(route, .25)).toEqual([[0, 0], [0, .5]]);
    expect(progressiveRoute(route, 1)).toEqual(route);
    expect(progressiveRoute(route, 0)).toEqual([[0, 0]]);
  });
  it('reduce el trazado sin perder los extremos ni el orden', () => {
    const route = straightRoute(2000);
    const sampled = downsampleRoute(route, 320);

    expect(sampled.length).toBeLessThanOrEqual(321);
    expect(sampled[0]).toEqual(route[0]);
    expect(sampled[sampled.length - 1]).toEqual(route[route.length - 1]);
    for (let i = 1; i < sampled.length; i++) {
      expect(sampled[i][1]).toBeGreaterThan(sampled[i - 1][1]);
    }
  });

  it('mantiene el trazado original cuando ya es corto', () => {
    const route = straightRoute(10);
    expect(downsampleRoute(route, 320)).toEqual(route);
  });

  it('ubica la fracción por distancia acumulada', () => {
    const route: LatLng[] = [[0, 0], [0, 1], [0, 2]];
    expect(indexAtFraction(route, 0)).toBe(0);
    expect(indexAtFraction(route, 0.5)).toBe(1);
    expect(indexAtFraction(route, 1)).toBe(2);
    expect(indexAtFraction(route, -1)).toBe(0);
    expect(indexAtFraction(route, 2)).toBe(2);
  });

  it('genera ramas deterministas, cercanas al trazado y con segmentos acotados', () => {
    const route = straightRoute(120);
    const first = buildExplorationSpurs(route, 'ruta-1', 12);
    const second = buildExplorationSpurs(route, 'ruta-1', 12);
    const other = buildExplorationSpurs(route, 'ruta-2', 12);

    expect(first).toEqual(second);
    expect(first).not.toEqual(other);
    expect(first).toHaveLength(12);

    for (const spur of first) {
      expect(spur.at).toBeGreaterThanOrEqual(0.02);
      expect(spur.at).toBeLessThanOrEqual(0.98);
      expect(spur.coordinates.length).toBeGreaterThanOrEqual(3);
      expect(route.some(([lat, lng]) => lat === spur.coordinates[0][0] && lng === spur.coordinates[0][1])).toBe(true);
      for (const [lat, lng] of spur.coordinates) {
        expect(Number.isFinite(lat)).toBe(true);
        expect(Number.isFinite(lng)).toBe(true);
      }
      for (let i = 1; i < spur.coordinates.length; i++) {
        expect(distanceMeters(spur.coordinates[i - 1], spur.coordinates[i])).toBeLessThanOrEqual(261);
      }
    }
  });

  it('no genera ramas sin trazado', () => {
    expect(buildExplorationSpurs([[0, 0]], 'ruta-1')).toEqual([]);
  });
});
