import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '@shared/api';
import { MapViewComponent, MapMarker, MapPolyline, ButtonComponent, BadgeComponent, CardComponent } from '@shared/ui';
import { LucideTriangleAlert, LucideZap } from '@lucide/angular';
import { ServicePoint } from '@istpetdev/shared-core';

@Component({
  selector: 'widget-route-map',
  standalone: true,
  imports: [CommonModule, MapViewComponent, ButtonComponent, BadgeComponent, CardComponent, LucideTriangleAlert, LucideZap],
  template: `
    <ui-card [padding]="'none'">
      <!-- Encabezado de control del mapa -->
      <div class="p-4 border-b border-slate-200 bg-slate-50 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 class="text-base font-bold text-slate-900">Mapa Logístico y Optimización de Rutas</h2>
          <p class="text-xs text-slate-500">Monitoreo de red hospitalaria y cálculo de abastecimiento nacional</p>
        </div>

        <div class="flex items-center gap-2">
          <!-- Toggle de modo de ruta con botones accesibles -->
          <div class="inline-flex rounded-lg border border-slate-300 p-0.5 bg-white text-xs">
            <button
              type="button"
              (click)="setRouteMode('optimized')"
              [class.bg-primary-600]="routeMode() === 'optimized'"
              [class.text-white]="routeMode() === 'optimized'"
              [class.text-slate-600]="routeMode() !== 'optimized'"
              class="px-3 py-1.5 font-medium rounded-md transition-colors">
              Ruta Optimizada
            </button>
            <button
              type="button"
              (click)="setRouteMode('baseline')"
              [class.bg-slate-700]="routeMode() === 'baseline'"
              [class.text-white]="routeMode() === 'baseline'"
              [class.text-slate-600]="routeMode() !== 'baseline'"
              class="px-3 py-1.5 font-medium rounded-md transition-colors">
              Ruta Tradicional
            </button>
          </div>

          <!-- Botón de Simulación de Incidente para el Pitch (Iconos Lucide, sin emojis) -->
          <ui-button
            [variant]="incidentActive() ? 'destructive' : 'simulation'"
            size="sm"
            (buttonClick)="toggleIncident()"
            [ariaLabel]="incidentActive() ? 'Desactivar desvío por incidente' : 'Simular incidente en vía'">
            @if (incidentActive()) {
              <svg lucideTriangleAlert [size]="14" class="stroke-[2.2] mr-1.5 text-white"></svg>
              <span>Incidente Activo (Desvío)</span>
            } @else {
              <svg lucideZap [size]="14" class="stroke-[2.2] mr-1.5 text-accent-400"></svg>
              <span>Simular Incidente Vía</span>
            }
          </ui-button>
        </div>
      </div>

      <!-- Métricas comparativas rápidas de la ruta -->
      <div class="grid grid-cols-2 sm:grid-cols-4 border-b border-slate-200 text-xs divide-x divide-slate-100 bg-white">
        <div class="p-3">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold">Distancia Total</span>
          <span class="text-base font-bold font-mono" [class.text-primary-600]="routeMode() === 'optimized'">
            {{ routeMode() === 'optimized' ? (incidentActive() ? '418 km' : '385 km') : '512 km' }}
          </span>
          <span class="text-[10px] text-secondary-700 font-semibold block">
            {{ routeMode() === 'optimized' ? (incidentActive() ? 'Ahorro: 18.3%' : 'Ahorro: 24.8%') : 'Línea base' }}
          </span>
        </div>

        <div class="p-3">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold">Tiempo Estimado</span>
          <span class="text-base font-bold font-mono text-slate-800">
            {{ routeMode() === 'optimized' ? (incidentActive() ? '7h 15m' : '6h 10m') : '9h 40m' }}
          </span>
          <span class="text-[10px] text-slate-500 block">Flota C-01 (Quito Hub)</span>
        </div>

        <div class="p-3">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold">Hospitales Atendidos</span>
          <span class="text-base font-bold font-mono text-slate-800">
            {{ hospitalCount() }} Hospitales
          </span>
          <span class="text-[10px] text-secondary-700 font-semibold block">
            {{ totalPointsCount() }} nodos en red PostGIS
          </span>
        </div>

        <div class="p-3">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold mb-1">Estado de Custodia</span>
          <ui-badge 
            [variant]="incidentActive() ? 'warning' : 'success'" 
            size="sm" 
            [withDot]="true" 
            [label]="incidentActive() ? 'En Contingencia' : 'Verificada'">
          </ui-badge>
          <span class="text-[10px] text-slate-500 block mt-1 font-mono">
            {{ incidentActive() ? 'Desvío en monitoreo' : 'Cadena inmutable' }}
          </span>
        </div>
      </div>

      <!-- Contenedor del Mapa -->
      <div class="h-[480px] w-full">
        <ui-map-view
          [markers]="markers()"
          [polylines]="polylines()">
        </ui-map-view>
      </div>
    </ui-card>
  `
})
export class RouteMapWidget implements OnInit {
  private readonly apiService = inject(ApiService);

  readonly routeMode = signal<'optimized' | 'baseline'>('optimized');
  readonly incidentActive = signal(false);

  readonly markers = signal<MapMarker[]>([]);
  readonly polylines = signal<MapPolyline[]>([]);

  readonly hospitalCount = computed(() => this.markers().filter(m => m.badge === 'HOSPITAL').length);
  readonly totalPointsCount = computed(() => this.markers().length);

  // Coordenadas canónicas de centros hospitalarios y bodegas en Ecuador
  private readonly defaultPoints: ServicePoint[] = [
    {
      id: 'sp-01',
      code: 'HUB-UIO',
      name: 'Centro Logístico Principal Quito (Bodega Central)',
      type: 'BODEGA_CENTRAL',
      latitude: -0.1807,
      longitude: -78.4678,
      isActive: true
    },
    {
      id: 'sp-02',
      code: 'HOSP-AMB',
      name: 'Hospital General Docente Ambato',
      type: 'HOSPITAL',
      latitude: -1.2491,
      longitude: -78.6168,
      isActive: true
    },
    {
      id: 'sp-03',
      code: 'HOSP-CUE',
      name: 'Hospital Vicente Corral Moscoso (Cuenca)',
      type: 'HOSPITAL',
      latitude: -2.9001,
      longitude: -79.0059,
      isActive: true
    },
    {
      id: 'sp-04',
      code: 'HOSP-GYE',
      name: 'Hospital Teodoro Maldonado Carbo (Guayaquil)',
      type: 'HOSPITAL',
      latitude: -2.1894,
      longitude: -79.8891,
      isActive: true
    },
    {
      id: 'sp-05',
      code: 'HOSP-SD',
      name: 'Hospital Gustavo Domínguez (Santo Domingo)',
      type: 'HOSPITAL',
      latitude: -0.2531,
      longitude: -79.1754,
      isActive: true
    }
  ];

  ngOnInit(): void {
    this.loadMapData();
  }

  loadMapData(): void {
    this.apiService.getServicePoints().subscribe((pts) => {
      const activePts = pts && pts.length > 0 ? pts : this.defaultPoints;
      
      const validPts = activePts.filter(
        (p): p is typeof p & { latitude: number; longitude: number } =>
          p.latitude !== null && p.longitude !== null
      );

      const mapMarkers: MapMarker[] = validPts.map(p => ({
        id: p.id,
        latitude: p.latitude,
        longitude: p.longitude,
        title: p.name,
        badge: p.type === 'BODEGA_CENTRAL' ? 'BODEGA HUB' : 'HOSPITAL',
        description: `Código: ${p.code} • Coordenadas: ${p.latitude.toFixed(4)}, ${p.longitude.toFixed(4)}`
      }));

      this.markers.set(mapMarkers);
      this.updatePolylines();
    });
  }

  setRouteMode(mode: 'optimized' | 'baseline'): void {
    this.routeMode.set(mode);
    this.updatePolylines();
  }

  toggleIncident(): void {
    this.incidentActive.update(v => !v);
    this.updatePolylines();
  }

  private updatePolylines(): void {
    const isOpt = this.routeMode() === 'optimized';
    const isIncident = this.incidentActive();

    // Rutas poligonales trazadas entre los puntos reales de Ecuador
    if (isOpt) {
      if (!isIncident) {
        // Ruta Optimizada Estándar: Quito -> Santo Domingo -> Guayaquil -> Cuenca -> Ambato -> Quito
        this.polylines.set([
          {
            id: 'route-opt',
            coordinates: [
              [-0.1807, -78.4678], // Quito
              [-0.2531, -79.1754], // Sto Domingo
              [-2.1894, -79.8891], // Guayaquil
              [-2.9001, -79.0059], // Cuenca
              [-1.2491, -78.6168], // Ambato
              [-0.1807, -78.4678]  // Retorno Quito
            ],
            color: '#00A7C4', // Clean Cyan oficial
            weight: 4
          }
        ]);
      } else {
        // Ruta con Incidente (Cierre Alóag): Replanificación por Calacalí - La Concordia
        this.polylines.set([
          {
            id: 'route-incident-delivered',
            coordinates: [
              [-0.1807, -78.4678],
              [-0.2531, -79.1754]
            ],
            color: '#9EC200', // Expo Lime oficial: Paradas ya ejecutadas conservadas
            weight: 4
          },
          {
            id: 'route-incident-reroute',
            coordinates: [
              [-0.2531, -79.1754],
              [-0.9500, -79.5000], // Desvío por Quevedo
              [-2.1894, -79.8891], // Guayaquil
              [-2.9001, -79.0059], // Cuenca
              [-1.2491, -78.6168], // Ambato
              [-0.1807, -78.4678]  // Retorno
            ],
            color: '#EA601F', // Energy Orange oficial: Ruta recalculada
            weight: 4,
            dashArray: '6, 8'
          }
        ]);
      }
    } else {
      // Ruta Tradicional (Sin optimizar, ida y vuelta repetitiva):
      this.polylines.set([
        {
          id: 'route-baseline',
          coordinates: [
            [-0.1807, -78.4678],
            [-2.1894, -79.8891],
            [-0.1807, -78.4678],
            [-2.9001, -79.0059],
            [-0.1807, -78.4678],
            [-1.2491, -78.6168]
          ],
          color: '#64748b',
          weight: 3,
          dashArray: '4, 4'
        }
      ]);
    }
  }
}
