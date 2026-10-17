import type { MapMarker, MapPolyline } from '@shared/ui';

export interface JourneyStop {
  sequence: number;
  name: string;
  kind: string;
  position: { lat: number; lon: number };
  legDistanceKm?: number;
  legDurationMinutes?: number;
}
export interface JourneyRoute {
  id: string;
  vehicleCode: string;
  color: string;
  approximate: boolean;
  coordinates: [number, number][];
  stops: JourneyStop[];
}
export interface JourneyLeg {
  from: JourneyStop;
  to: JourneyStop;
  coordinates: [number, number][];
}

/** Segmentación visual del trazado recibido; las distancias y los tiempos siguen siendo los del servidor. */
export function journeyLegs(route: JourneyRoute): JourneyLeg[] {
  const legs: JourneyLeg[] = [];
  let start = 0;
  for (let index = 1; index < route.stops.length; index++) {
    const to = route.stops[index], from = route.stops[index - 1];
    let end = start, nearest = Infinity;
    for (let i = start; i < route.coordinates.length; i++) {
      const distance = Math.hypot(route.coordinates[i][0] - to.position.lat,
        (route.coordinates[i][1] - to.position.lon) * Math.cos(to.position.lat * Math.PI / 180));
      if (distance < nearest) { nearest = distance; end = i; }
    }
    if (index === route.stops.length - 1) end = route.coordinates.length - 1;
    const coordinates = route.coordinates.slice(start, end + 1);
    if (coordinates.length === 1) coordinates.push(coordinates[0]);
    legs.push({ from, to, coordinates });
    start = end;
  }
  return legs;
}

export function journeyMarkers(route: JourneyRoute): MapMarker[] {
  return route.stops.map(stop => ({ id: `${route.id}:${stop.sequence}`, latitude: stop.position.lat,
    longitude: stop.position.lon, title: `${route.vehicleCode} · ${stop.name}`, color: route.color,
    pinLabel: `${stop.sequence}`, description: stop.kind === 'Pickup' ? 'Recogida en bodega' : 'Entrega' }));
}

export function journeyBackground(route: JourneyRoute, focused: boolean): MapPolyline {
  return { id: route.id, coordinates: route.coordinates, color: focused ? route.color : '#9aa9bc', weight: 3,
    opacity: focused ? .18 : .35, dashArray: route.approximate ? '6 6' : undefined, tooltip: route.vehicleCode };
}
