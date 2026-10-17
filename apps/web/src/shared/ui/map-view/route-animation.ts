export type LatLng = [number, number];

export interface ExplorationSpur {
  coordinates: LatLng[];
  /** Fracción del recorrido donde nace la rama (0 = inicio, 1 = fin). */
  at: number;
}

const METERS_PER_DEGREE = 111_320;
const DEFAULT_SPUR_COUNT = 36;

export function distanceMeters(a: LatLng, b: LatLng): number {
  const midLat = ((a[0] + b[0]) / 2) * Math.PI / 180;
  const north = (b[0] - a[0]) * METERS_PER_DEGREE;
  const east = (b[1] - a[1]) * METERS_PER_DEGREE * Math.cos(midLat);
  return Math.hypot(north, east);
}

/** Recorrido progresivo por distancia, incluida una posición entre dos vértices. */
export function progressiveRoute(coordinates: LatLng[], fraction: number): LatLng[] {
  if (coordinates.length < 2 || fraction <= 0) return coordinates.slice(0, 1);
  if (fraction >= 1) return coordinates.slice();
  const distances = cumulativeDistances(coordinates);
  const target = distances[distances.length - 1] * fraction;
  for (let i = 1; i < coordinates.length; i++) {
    if (distances[i] < target) continue;
    const length = distances[i] - distances[i - 1];
    const part = length > 0 ? (target - distances[i - 1]) / length : 0;
    const previous = coordinates[i - 1], next = coordinates[i];
    return [...coordinates.slice(0, i), [previous[0] + (next[0] - previous[0]) * part, previous[1] + (next[1] - previous[1]) * part]];
  }
  return coordinates.slice();
}

function cumulativeDistances(coordinates: LatLng[]): number[] {
  const cumulative = [0];
  for (let i = 1; i < coordinates.length; i++) {
    cumulative.push(cumulative[i - 1] + distanceMeters(coordinates[i - 1], coordinates[i]));
  }
  return cumulative;
}

export function indexAtFraction(coordinates: LatLng[], fraction: number): number {
  if (coordinates.length < 2) return 0;
  if (fraction <= 0) return 0;
  if (fraction >= 1) return coordinates.length - 1;

  const cumulative = cumulativeDistances(coordinates);
  const target = cumulative[cumulative.length - 1] * fraction;
  for (let i = 1; i < cumulative.length; i++) {
    if (cumulative[i] >= target) return i;
  }
  return coordinates.length - 1;
}

export function downsampleRoute(coordinates: LatLng[], maxPoints = 320): LatLng[] {
  if (coordinates.length <= maxPoints) return coordinates.slice();
  if (maxPoints < 2) return [coordinates[0]];

  const cumulative = cumulativeDistances(coordinates);
  const total = cumulative[cumulative.length - 1];
  if (total === 0) return [coordinates[0], coordinates[coordinates.length - 1]];

  const sampled: LatLng[] = [];
  let cursor = 0;
  for (let i = 0; i < maxPoints; i++) {
    const target = total * i / (maxPoints - 1);
    while (cursor < cumulative.length - 1 && cumulative[cursor] < target) cursor++;
    const point = coordinates[cursor];
    const previous = sampled[sampled.length - 1];
    if (!previous || previous[0] !== point[0] || previous[1] !== point[1]) sampled.push(point);
  }
  const last = coordinates[coordinates.length - 1];
  if (sampled[sampled.length - 1][0] !== last[0] || sampled[sampled.length - 1][1] !== last[1]) {
    sampled.push(last);
  }
  return sampled;
}

function hashSeed(value: string): number {
  let hash = 2166136261;
  for (let i = 0; i < value.length; i++) {
    hash ^= value.charCodeAt(i);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

function mulberry32(seed: number): () => number {
  let state = seed;
  return () => {
    state = (state + 0x6d2b79f5) | 0;
    let t = Math.imul(state ^ (state >>> 15), 1 | state);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function offset(point: LatLng, bearing: number, meters: number): LatLng {
  const north = Math.cos(bearing) * meters;
  const east = Math.sin(bearing) * meters;
  const lat = point[0] + north / METERS_PER_DEGREE;
  const lng = point[1] + east / (METERS_PER_DEGREE * Math.cos(point[0] * Math.PI / 180));
  return [lat, lng];
}

function bearingBetween(a: LatLng, b: LatLng): number {
  const midLat = ((a[0] + b[0]) / 2) * Math.PI / 180;
  const north = (b[0] - a[0]) * METERS_PER_DEGREE;
  const east = (b[1] - a[1]) * METERS_PER_DEGREE * Math.cos(midLat);
  if (north === 0 && east === 0) return 0;
  return Math.atan2(east, north);
}

/**
 * Ramas decorativas: no son la red vial ni un árbol de búsqueda real.
 * Se generan de forma determinista para que la animación no parpadee entre renders.
 */
export function buildExplorationSpurs(
  coordinates: LatLng[],
  seed: string,
  count = DEFAULT_SPUR_COUNT
): ExplorationSpur[] {
  if (coordinates.length < 2 || count <= 0) return [];

  const random = mulberry32(hashSeed(seed));
  const spurs: ExplorationSpur[] = [];

  for (let i = 0; i < count; i++) {
    const jitter = (random() - 0.5) * 0.6 / count;
    const at = Math.min(0.98, Math.max(0.02, (i + 0.5) / count + jitter));
    const index = Math.min(coordinates.length - 2, indexAtFraction(coordinates, at));
    const start = coordinates[index];
    const heading = bearingBetween(start, coordinates[index + 1])
      + (random() < 0.5 ? -1 : 1) * (40 + random() * 90) * Math.PI / 180;
    const segments = 2 + Math.floor(random() * 2);
    const points: LatLng[] = [start];
    let point = start;
    let segmentHeading = heading;

    for (let s = 0; s < segments; s++) {
      point = offset(point, segmentHeading, 80 + random() * 180);
      points.push(point);
      segmentHeading += (random() - 0.5) * 70 * Math.PI / 180;
    }

    spurs.push({ coordinates: points, at });
  }

  return spurs;
}
