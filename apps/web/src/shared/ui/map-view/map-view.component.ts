import {
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';

export interface MapMarker {
  id: string;
  latitude: number;
  longitude: number;
  title: string;
  description?: string;
  badge?: string;
  badgeColor?: 'blue' | 'emerald' | 'amber' | 'red';
}

export interface MapPolyline {
  id: string;
  coordinates: [number, number][]; // [lat, lng]
  color?: string;
  weight?: number;
  dashArray?: string;
}

@Component({
  selector: 'ui-map-view',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative w-full h-full min-h-[400px] rounded-xl overflow-hidden border border-slate-200 shadow-sm">
      <div #mapContainer class="w-full h-full min-h-[400px] z-0"></div>
      
      <!-- Overlay de leyenda cartográfica -->
      <div class="absolute bottom-4 left-4 z-[400] bg-white/95 backdrop-blur px-3 py-2 rounded-lg border border-slate-200 shadow-md text-xs text-slate-700">
        <div class="flex items-center gap-3">
          <div class="flex items-center gap-1.5">
            <span class="w-2.5 h-2.5 rounded-full bg-primary-600 inline-block"></span>
            <span class="font-medium">Punto de Servicio</span>
          </div>
          <div class="flex items-center gap-1.5">
            <span class="w-4 h-0.5 bg-primary-600 inline-block"></span>
            <span class="font-medium">Ruta Optimizada</span>
          </div>
        </div>
      </div>
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

  private map?: L.Map;
  private markersLayer?: L.LayerGroup;
  private polylinesLayer?: L.LayerGroup;

  ngOnInit(): void {
    this.initMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.map) {
      if (changes['markers']) {
        this.renderMarkers();
      }
      if (changes['polylines']) {
        this.renderPolylines();
      }
    }
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }

  private initMap(): void {
    // Configurar iconos por defecto de Leaflet para evitar problemas de assets
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

    this.markersLayer = L.layerGroup().addTo(this.map);
    this.polylinesLayer = L.layerGroup().addTo(this.map);

    this.renderMarkers();
    this.renderPolylines();

    // Redimensionar correctamente tras el renderizado inicial
    setTimeout(() => {
      this.map?.invalidateSize();
    }, 200);
  }

  private renderMarkers(): void {
    if (!this.map || !this.markersLayer) return;
    this.markersLayer.clearLayers();

    this.markers.forEach((m) => {
      const popupContent = `
        <div style="font-family: inherit; font-size: 13px; line-height: 1.4;">
          <strong style="color: #0f172a; font-size: 14px;">${m.title}</strong>
          ${m.badge ? `<div style="margin-top: 4px;"><span style="background: #e6f7fa; color: #026e82; font-size: 11px; font-weight: bold; padding: 2px 6px; border-radius: 4px;">${m.badge}</span></div>` : ''}
          ${m.description ? `<p style="margin-top: 6px; color: #475569; font-size: 12px; margin-bottom: 0;">${m.description}</p>` : ''}
        </div>
      `;

      const marker = L.marker([m.latitude, m.longitude]).bindPopup(popupContent);
      this.markersLayer?.addLayer(marker);
    });

    if (this.markers.length > 0 && this.map) {
      const group = L.featureGroup(this.markers.map(m => L.marker([m.latitude, m.longitude])));
      this.map.fitBounds(group.getBounds().pad(0.2));
    }
  }

  private renderPolylines(): void {
    if (!this.map || !this.polylinesLayer) return;
    this.polylinesLayer.clearLayers();

    this.polylines.forEach((line) => {
      const polyline = L.polyline(line.coordinates, {
        color: line.color || '#00A7C4',
        weight: line.weight || 4,
        dashArray: line.dashArray || undefined,
        opacity: 0.85
      });
      this.polylinesLayer?.addLayer(polyline);
    });
  }
}
