import {
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
  Output,
  EventEmitter,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import { buildExplorationSpurs, downsampleRoute, indexAtFraction, progressiveRoute } from './route-animation';

export interface MapMarker {
  id: string;
  latitude: number;
  longitude: number;
  title: string;
  description?: string;
  badge?: string;
  badgeColor?: 'blue' | 'emerald' | 'amber' | 'red';
  /** Color libre (por ejemplo, el de la ruta del vehículo); dibuja un círculo con pinLabel. */
  color?: string;
  pinLabel?: string;
}

export interface MapPolyline {
  id: string;
  coordinates: [number, number][]; // [lat, lng]
  color?: string;
  weight?: number;
  dashArray?: string;
  opacity?: number;
  /** Texto al pasar el cursor sobre la línea (por ejemplo, vehículo y km). */
  tooltip?: string;
}

// Los títulos y descripciones vienen de datos importados: se escapan antes de insertarlos como HTML.
function escapeHtml(value: string | undefined): string {
  return (value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]!));
}

function isCssColor(value: string): boolean {
  return /^#[0-9a-fA-F]{3,8}$/.test(value);
}

@Component({
  selector: 'ui-map-view',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative w-full h-full min-h-[400px] rounded-xl overflow-hidden border border-slate-200 shadow-sm">
      <div #mapContainer class="w-full h-full min-h-[400px] z-0"></div>
      
      <!-- Overlay de leyenda cartográfica -->
      @if (showLegend) {
        <div class="absolute bottom-4 left-4 z-[400] bg-white/95 backdrop-blur px-3 py-2 rounded-lg border border-slate-200 shadow-md text-xs text-slate-700">
          <div class="flex items-center gap-3">
            <div class="flex items-center gap-1.5">
              <span class="w-2.5 h-2.5 rounded-full bg-amber-500 inline-block"></span>
              <span class="font-medium">Bodega</span>
            </div>
            <div class="flex items-center gap-1.5">
              <span class="w-2.5 h-2.5 rounded-full bg-primary-600 inline-block"></span>
              <span class="font-medium">Punto de Entrega</span>
            </div>
            <div class="flex items-center gap-1.5">
              <span class="w-4 h-0.5 bg-primary-600 inline-block"></span>
              <span class="font-medium">Vial continuo</span>
            </div>
            <div class="flex items-center gap-1.5">
              <span class="w-4 h-0.5 border-b-2 border-dashed border-primary-500 inline-block"></span>
              <span class="font-medium">Aproximación punteada</span>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      height: 100%;
    }
  `]
})
export class MapViewComponent implements OnInit, OnChanges, OnDestroy {
  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;

  @Input() center: [number, number] = [-1.8312, -78.1834]; // Centro de Ecuador
  @Input() zoom = 7;
  @Input() markers: MapMarker[] = [];
  @Input() polylines: MapPolyline[] = [];
  @Input() showLegend = true;
  /** Ruta que se anima al dibujarse; las demás se muestran de inmediato. */
  @Input() animatedPolylineId: string | null = null;
  @Input() animationMode: 'exploration' | 'journey' = 'exploration';
  @Input() animationPaused = false;
  @Output() animationComplete = new EventEmitter<string>();

  @Output() markerClick = new EventEmitter<MapMarker>();
  @Output() mapClick = new EventEmitter<[number, number]>();

  private static readonly ROUTE_ANIMATION_MS = 2400;

  private map?: L.Map;
  private markersLayer?: L.LayerGroup;
  private polylinesLayer?: L.LayerGroup;
  private boundsSignature = '';
  private resumeJourney?: () => void;
  private animationLayer?: L.LayerGroup;
  private animationFrameId?: number;
  private lastAnimatedKey: string | null = null;
  private polylinesSignature = '';

  ngOnInit(): void {
    this.initMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['animationPaused'] && !this.animationPaused) this.resumeJourney?.();
    if (this.map) {
      if (changes['markers']) {
        this.renderMarkers();
      }
      if (changes['polylines']) {
        this.renderPolylines();
      } else if (changes['animatedPolylineId']) {
        this.renderPolylines();
      }
    }
  }

  ngOnDestroy(): void {
    this.cancelRouteAnimation();
    if (this.map) {
      this.map.remove();
    }
  }

  public invalidateSize(): void {
    setTimeout(() => {
      this.map?.invalidateSize();
    }, 150);
  }

  private initMap(): void {
    const defaultIcon = L.icon({
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41]
    });
    L.Marker.prototype.options.icon = defaultIcon;

    this.map = L.map(this.mapContainer.nativeElement, {
      zoomControl: true,
      attributionControl: true
    }).setView(this.center, this.zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    this.map.on('click', (e: L.LeafletMouseEvent) => {
      this.mapClick.emit([e.latlng.lat, e.latlng.lng]);
    });

    this.markersLayer = L.layerGroup().addTo(this.map);
    this.polylinesLayer = L.layerGroup().addTo(this.map);
    this.animationLayer = L.layerGroup().addTo(this.map);

    this.renderMarkers();
    this.renderPolylines();

    this.invalidateSize();
  }

  private renderMarkers(): void {
    if (!this.map || !this.markersLayer) return;
    this.markersLayer.clearLayers();

    this.markers.forEach((m) => {
      const popupContent = `
        <div style="font-family: inherit; font-size: 13px; line-height: 1.4; min-width: 140px;">
          <strong style="color: #0f172a; font-size: 14px;">${escapeHtml(m.title)}</strong>
          ${m.badge ? `<div style="margin-top: 4px;"><span style="background: ${m.badgeColor === 'amber' ? '#fef3c7; color: #92400e' : m.badgeColor === 'emerald' ? '#d1fae5; color: #065f46' : '#e6f7fa; color: #026e82'}; font-size: 11px; font-weight: bold; padding: 2px 6px; border-radius: 4px;">${escapeHtml(m.badge)}</span></div>` : ''}
          ${m.description ? `<p style="margin-top: 6px; color: #475569; font-size: 12px; margin-bottom: 0;">${escapeHtml(m.description)}</p>` : ''}
        </div>
      `;

      let icon: L.Icon | L.DivIcon | undefined = undefined;
      if (m.color && isCssColor(m.color)) {
        icon = L.divIcon({
          className: 'custom-route-stop-pin',
          html: `<div style="background-color: ${m.color}; color: white; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: bold; font-size: 10px; border: 2px solid white; box-shadow: 0 1px 4px rgba(0,0,0,0.4);">${escapeHtml(m.pinLabel)}</div>`,
          iconSize: [22, 22],
          iconAnchor: [11, 11],
          popupAnchor: [0, -11]
        });
      } else if (m.badgeColor === 'amber') {
        icon = L.divIcon({
          className: 'custom-warehouse-pin',
          html: `<div style="background-color: #d97706; color: white; width: 26px; height: 26px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: bold; font-size: 11px; border: 2px solid white; box-shadow: 0 2px 5px rgba(0,0,0,0.4);">B</div>`,
          iconSize: [26, 26],
          iconAnchor: [13, 13],
          popupAnchor: [0, -13]
        });
      } else if (m.badgeColor === 'emerald') {
        icon = L.divIcon({
          className: 'custom-start-pin',
          html: `<div style="background-color: #059669; color: white; width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: bold; font-size: 11px; border: 2px solid white; box-shadow: 0 2px 5px rgba(0,0,0,0.4);">1</div>`,
          iconSize: [24, 24],
          iconAnchor: [12, 12],
          popupAnchor: [0, -12]
        });
      }

      const marker = icon 
        ? L.marker([m.latitude, m.longitude], { icon }).bindPopup(popupContent)
        : L.marker([m.latitude, m.longitude]).bindPopup(popupContent);

      marker.on('click', () => {
        this.markerClick.emit(m);
      });

      this.markersLayer?.addLayer(marker);
    });

    const signature = this.markers.map(m => `${m.id}:${m.latitude}:${m.longitude}`).sort().join('|');
    if (this.markers.length > 0 && this.map && signature !== this.boundsSignature) {
      this.boundsSignature = signature;
      const group = L.featureGroup(this.markers.map(m => L.marker([m.latitude, m.longitude])));
      this.map.fitBounds(group.getBounds().pad(0.2));
    }
  }

  private renderPolylines(): void {
    if (!this.map || !this.polylinesLayer) return;
    const signature = JSON.stringify([this.animationMode, this.animatedPolylineId, this.polylines]);
    if (signature === this.polylinesSignature) return;
    this.polylinesSignature = signature;
    this.cancelRouteAnimation();
    this.polylinesLayer.clearLayers();

    const animated = this.animatedPolylineId
      ? this.polylines.find(line => line.id === this.animatedPolylineId)
      : undefined;
    const canAnimate = !!animated
      && (!animated.dashArray || this.animationMode === 'journey')
      && animated.coordinates.length > 1
      && !this.prefersReducedMotion();
    const animationKey = canAnimate ? this.animationKey(animated!) : null;
    const animateNow = canAnimate && (this.animationMode === 'journey' || animationKey !== this.lastAnimatedKey);

    if (!canAnimate) this.lastAnimatedKey = null;

    this.polylines.forEach((line) => {
      if (animateNow && line === animated) return;
      this.polylinesLayer?.addLayer(this.buildPolyline(line));
    });

    if (animateNow) {
      this.lastAnimatedKey = animationKey;
      if (this.animationMode === 'journey') this.animateJourney(animated!);
      else this.animateRoute(animated!);
    }
  }

  private buildPolyline(line: MapPolyline): L.Polyline {
    const polyline = L.polyline(line.coordinates, {
      color: line.color || '#00A7C4',
      weight: line.weight || 4,
      dashArray: line.dashArray || undefined,
      opacity: line.opacity ?? 0.85
    });
    if (line.tooltip) polyline.bindTooltip(escapeHtml(line.tooltip), { sticky: true });
    return polyline;
  }

  private animateRoute(line: MapPolyline): void {
    if (!this.map || !this.animationLayer || !this.polylinesLayer) return;

    const canRequestFrames = typeof requestAnimationFrame === 'function';
    const coordinates = downsampleRoute(line.coordinates);
    if (!canRequestFrames || coordinates.length < 2) {
      this.polylinesLayer.addLayer(this.buildPolyline(line));
      return;
    }

    const reversed = [...coordinates].reverse();
    const spurs = buildExplorationSpurs(coordinates, line.id);
    const spurLayers = spurs.map(spur => {
      const layer = L.polyline(spur.coordinates, {
        color: '#3b82f6',
        weight: 2,
        opacity: 0,
        interactive: false
      }).addTo(this.animationLayer!);
      return { layer, revealAt: Math.min(0.75, Math.min(spur.at, 1 - spur.at) * 1.5) };
    });

    const forwardLine = L.polyline([], {
      color: '#06b6d4',
      weight: 4,
      opacity: 0.95,
      interactive: false
    }).addTo(this.animationLayer);
    const backwardLine = L.polyline([], {
      color: '#06b6d4',
      weight: 4,
      opacity: 0.95,
      interactive: false
    }).addTo(this.animationLayer);
    const headOptions: L.CircleMarkerOptions = {
      radius: 5,
      color: '#ffffff',
      weight: 2,
      fillColor: '#06b6d4',
      fillOpacity: 1,
      interactive: false
    };
    const forwardHead = L.circleMarker(coordinates[0], headOptions).addTo(this.animationLayer);
    const backwardHead = L.circleMarker(coordinates[coordinates.length - 1], headOptions).addTo(this.animationLayer);
    const startedAt = performance.now();

    const tick = (now: number): void => {
      const progress = Math.min(1, (now - startedAt) / MapViewComponent.ROUTE_ANIMATION_MS);
      const eased = progress < 0.5
        ? 2 * progress * progress
        : 1 - Math.pow(-2 * progress + 2, 2) / 2;
      const reveal = Math.round(indexAtFraction(coordinates, eased * 0.5));

      forwardLine.setLatLngs(coordinates.slice(0, reveal + 1));
      backwardLine.setLatLngs(reversed.slice(0, reveal + 1));
      forwardHead.setLatLng(coordinates[reveal]);
      backwardHead.setLatLng(reversed[reveal]);

      for (const spur of spurLayers) {
        const opacity = eased < spur.revealAt
          ? 0
          : eased <= 0.85 ? 1 : Math.max(0, (1 - eased) / 0.15);
        spur.layer.setStyle({ opacity: 0.34 * opacity });
      }

      if (progress < 1) {
        this.animationFrameId = requestAnimationFrame(tick);
        return;
      }

      this.animationFrameId = undefined;
      this.animationLayer?.clearLayers();
      this.polylinesLayer?.addLayer(this.buildPolyline(line));
    };

    this.animationFrameId = requestAnimationFrame(tick);
  }

  private animateJourney(line: MapPolyline): void {
    if (!this.animationLayer || !this.polylinesLayer) return;
    const coordinates = downsampleRoute(line.coordinates);
    const path = L.polyline([], { color: line.color, weight: 6, opacity: 1, dashArray: line.dashArray, interactive: false }).addTo(this.animationLayer);
    const head = L.circleMarker(coordinates[0], { radius: 6, color: '#fff', weight: 3, fillColor: line.color,
      fillOpacity: 1, interactive: false }).addTo(this.animationLayer);
    let previousTime = performance.now();
    let elapsed = 0;
    const tick = (now: number): void => {
      if (this.animationPaused) { this.animationFrameId = undefined; return; }
      elapsed += now - previousTime;
      previousTime = now;
      const progress = Math.min(1, elapsed / 2000);
      const points = progressiveRoute(coordinates, progress);
      path.setLatLngs(points);
      head.setLatLng(points[points.length - 1]);
      if (progress < 1) { this.animationFrameId = requestAnimationFrame(tick); return; }
      this.animationFrameId = undefined;
      this.resumeJourney = undefined;
      this.animationLayer?.clearLayers();
      this.polylinesLayer?.addLayer(this.buildPolyline(line));
      this.animationComplete.emit(line.id);
    };
    this.resumeJourney = () => { if (this.animationFrameId === undefined) { previousTime = performance.now(); this.animationFrameId = requestAnimationFrame(tick); } };
    this.animationFrameId = requestAnimationFrame(tick);
  }

  private cancelRouteAnimation(): void {
    this.resumeJourney = undefined;
    if (this.animationFrameId !== undefined && typeof cancelAnimationFrame === 'function') {
      cancelAnimationFrame(this.animationFrameId);
    }
    this.animationFrameId = undefined;
    this.animationLayer?.clearLayers();
  }

  private animationKey(line: MapPolyline): string {
    const first = line.coordinates[0];
    const last = line.coordinates[line.coordinates.length - 1];
    return `${line.id}|${line.coordinates.length}|${first[0]},${first[1]}|${last[0]},${last[1]}`;
  }

  private prefersReducedMotion(): boolean {
    return typeof window !== 'undefined'
      && typeof window.matchMedia === 'function'
      && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
}
