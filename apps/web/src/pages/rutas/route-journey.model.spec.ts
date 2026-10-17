import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { journeyLegs, JourneyRoute } from './route-journey.model';
import { RouteJourneyPlayer } from './route-journey-player.component';

const route: JourneyRoute = {
  id: 'vehicle-route', vehicleCode: 'VEH-01', color: '#0284c7', approximate: false,
  coordinates: [[0, 0], [0, .5], [0, 1], [0, 1.5], [0, 2]],
  stops: [0, 1, 2].map((n) => ({ sequence: n + 1, name: n ? `Entrega ${n}` : 'Recogida',
    kind: n ? 'Delivery' : 'Pickup', position: { lat: 0, lon: n }, legDistanceKm: n ? 10 : 0 }))
};

describe('Recorrido secuencial por vehículo', () => {
  it('divide la geometría en tramos contiguos y conserva sus extremos', () => {
    const legs = journeyLegs(route);
    expect(legs).toHaveLength(2);
    expect(legs[0].coordinates).toEqual([[0, 0], [0, .5], [0, 1]]);
    expect(legs[1].coordinates).toEqual([[0, 1], [0, 1.5], [0, 2]]);
    expect(legs[1].to.legDistanceKm).toBe(10);
  });
  it('avanza una parada al terminar un tramo e ignora respuestas de otra reproducción', () => {
    const fixture = TestBed.createComponent(RouteJourneyPlayer);
    fixture.componentRef.setInput('routes', [route]);
    fixture.componentRef.setInput('focusedRouteId', route.id);
    fixture.componentRef.setInput('autoplay', false);
    fixture.detectChanges();
    const player = fixture.componentInstance;
    player.play();
    const firstId = player.activePolylineId();
    player.completeLeg(firstId);
    expect(player.activeLegIndex()).toBe(1);
    player.completeLeg(firstId);
    expect(player.finished()).toBe(false);
    player.completeLeg(player.activePolylineId());
    expect(player.finished()).toBe(true);
    expect(player.playing()).toBe(false);
    fixture.destroy();
  });
  it('reproduce todos los tramos con el mapa real hasta la última parada', () => {
    const frames: FrameRequestCallback[] = [];
    vi.stubGlobal('matchMedia', vi.fn(() => ({ matches: false })));
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => { frames.push(callback); return frames.length; });
    vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => undefined);
    try {
      const longRoute = { ...route, coordinates: Array.from({ length: 7 }, (_, i) => [0, i] as [number, number]),
        stops: Array.from({ length: 7 }, (_, i) => ({ sequence: i, name: `Parada ${i}`, kind: i ? 'Delivery' : 'Pickup', position: { lat: 0, lon: i } })) };
      const fixture = TestBed.createComponent(RouteJourneyPlayer);
      fixture.componentRef.setInput('focusedRouteId', route.id);
      fixture.componentRef.setInput('routes', []);
      fixture.detectChanges();
      expect(fixture.componentInstance.playing()).toBe(false);
      fixture.componentRef.setInput('routes', [longRoute]); fixture.detectChanges();
      expect(fixture.componentInstance.playing()).toBe(true);
      for (let i = 0; i < 6; i++) {
        frames.splice(0).forEach(frame => frame(performance.now() + 2500));
        fixture.detectChanges();
        expect(fixture.componentInstance.activeLegIndex()).toBe(Math.min(i + 1, 5));
      }
      expect(fixture.componentInstance.finished()).toBe(true);
      fixture.componentRef.setInput('replayKey', 1); fixture.detectChanges();
      expect(fixture.componentInstance.activeLegIndex()).toBe(0);
      expect(fixture.componentInstance.playing()).toBe(true);
      fixture.destroy();
    } finally { vi.unstubAllGlobals(); vi.restoreAllMocks(); }
  });
  it('selecciona el tramo por orden de parada cuando la numeración empieza en cero', () => {
    const fixture = TestBed.createComponent(RouteJourneyPlayer);
    fixture.componentRef.setInput('routes', [{ ...route, stops: route.stops.map((stop, sequence) => ({ ...stop, sequence })) }]);
    fixture.componentRef.setInput('focusedRouteId', route.id);
    fixture.componentRef.setInput('autoplay', false);
    fixture.detectChanges();
    fixture.componentInstance.selectMarker({ id: `${route.id}:2`, latitude: 0, longitude: 2, title: 'Entrega 2' });
    expect(fixture.componentInstance.activeLegIndex()).toBe(1);
    fixture.destroy();
  });
  it('permite pasos manuales y respeta movimiento reducido', () => {
    vi.stubGlobal('matchMedia', vi.fn(() => ({ matches: true })));
    try {
      const fixture = TestBed.createComponent(RouteJourneyPlayer);
      fixture.componentRef.setInput('routes', [route]);
      fixture.componentRef.setInput('focusedRouteId', route.id);
      fixture.detectChanges();
      const player = fixture.componentInstance;
      expect(player.playing()).toBe(false);
      player.goToLeg(1);
      expect(player.activeLeg()?.to.name).toBe('Entrega 2');
      player.play();
      expect(player.playing()).toBe(false);
      fixture.destroy();
    } finally { vi.unstubAllGlobals(); }
  });
});
