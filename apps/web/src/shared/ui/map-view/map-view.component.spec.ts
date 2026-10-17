import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { MapPolyline, MapViewComponent } from './map-view.component';

const route: MapPolyline = {
  id: 'p1',
  coordinates: [[0, 0], [0.01, 0.01], [0.02, 0.02], [0.03, 0.03], [0.04, 0.04]],
  color: '#00A7C4',
  weight: 5
};

type LayerAccess = {
  polylinesLayer?: { getLayers(): unknown[] };
  animationLayer?: { getLayers(): unknown[] };
};

describe('MapViewComponent: animación de ruta', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('muestra la ruta final y limpia la exploración al terminar', () => {
    const frames: FrameRequestCallback[] = [];
    vi.stubGlobal('matchMedia', vi.fn(() => ({ matches: false })));
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => {
      frames.push(callback);
      return frames.length;
    });
    vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => undefined);

    const fixture = TestBed.createComponent(MapViewComponent);
    const component = fixture.componentInstance;
    component.polylines = [route];
    component.animatedPolylineId = route.id;
    fixture.detectChanges();

    const layers = component as unknown as LayerAccess;
    expect(layers.polylinesLayer?.getLayers()).toHaveLength(0);
    expect(layers.animationLayer?.getLayers().length).toBeGreaterThan(0);

    const now = performance.now();
    while (frames.length > 0) {
      frames.shift()!(now + 3000);
    }

    expect(layers.polylinesLayer?.getLayers()).toHaveLength(1);
    expect(layers.animationLayer?.getLayers()).toHaveLength(0);
    fixture.destroy();
  });

  it('conserva la reproducción activa al refrescar el mismo trazado', () => {
    vi.stubGlobal('matchMedia', vi.fn(() => ({ matches: false })));
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation(() => 1);
    const cancel = vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => undefined);
    const fixture = TestBed.createComponent(MapViewComponent);
    fixture.componentRef.setInput('polylines', [route]);
    fixture.componentRef.setInput('animatedPolylineId', route.id);
    fixture.componentRef.setInput('animationMode', 'journey');
    fixture.detectChanges();
    cancel.mockClear();
    fixture.componentRef.setInput('polylines', [{ ...route, coordinates: route.coordinates.map(point => [...point]) }]);
    fixture.detectChanges();
    expect(cancel).not.toHaveBeenCalled();
    expect((fixture.componentInstance as unknown as LayerAccess).animationLayer?.getLayers().length).toBeGreaterThan(0);
    fixture.destroy();
  });

  it('no anima los trazados aproximados punteados', () => {
    const fixture = TestBed.createComponent(MapViewComponent);
    const component = fixture.componentInstance;
    component.polylines = [{ ...route, dashArray: '6 6' }];
    component.animatedPolylineId = route.id;
    fixture.detectChanges();

    const layers = component as unknown as LayerAccess;
    expect(layers.polylinesLayer?.getLayers()).toHaveLength(1);
    expect(layers.animationLayer?.getLayers()).toHaveLength(0);
    fixture.destroy();
  });
});
