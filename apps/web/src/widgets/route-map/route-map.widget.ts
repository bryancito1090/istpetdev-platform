import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '@shared/api';
import { MapViewComponent, MapMarker, MapPolyline, ButtonComponent, BadgeComponent, CardComponent } from '@shared/ui';
import { LucideTriangleAlert, LucideZap, LucideTruck, LucideCheck, LucideActivity } from '@lucide/angular';
import { ServicePoint } from '@istpetdev/shared-core';

interface DeliveryStop {
  id: string;
  code: string;
  name: string;
  eta: string;
  status: 'completed' | 'in_transit' | 'scheduled';
  cargo: string;
  huCode: string;
}

@Component({
  selector: 'widget-route-map',
  standalone: true,
  imports: [
    CommonModule, 
    MapViewComponent, 
    ButtonComponent, 
    BadgeComponent, 
    CardComponent, 
    LucideTriangleAlert, 
    LucideZap,
    LucideTruck,
    LucideCheck,
    LucideActivity
  ],
  template: `
    <ui-card [padding]="'none'">
      <!-- Encabezado de control del mapa y ruteo -->
      <div class="p-4 sm:p-5 border-b border-slate-200 bg-white flex flex-wrap items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-2 mb-1">
            <span class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-[11px] font-mono font-semibold bg-primary-50 text-primary-700 border border-primary-200">
              <svg lucideTruck [size]="12" class="stroke-[2.2]"></svg>
              FLOTA C-01 • MERCEDES SPRINTER PBX-4821
            </span>
            <span class="text-xs text-slate-400 font-mono">|</span>
            <span class="text-xs font-mono text-slate-500">MANIFIESTO #MF-2026-084</span>
          </div>
          <h2 class="text-base sm:text-lg font-bold text-slate-900 tracking-tight">
            Ruteo Dinámico VRP y Despacho de Suministros
          </h2>
          <p class="text-xs text-slate-500">
            Optimización multi-parada con cálculo de geocercas PostGIS y respuesta a incidentes en tiempo real
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-2.5">
          <!-- Toggle de modo de ruta con botones de diseño Enterprise -->
          <div class="inline-flex rounded-lg border border-slate-200 p-1 bg-slate-50 text-xs shadow-inner">
            <button
              type="button"
              (click)="setRouteMode('optimized')"
              [class.bg-white]="routeMode() === 'optimized'"
              [class.shadow-sm]="routeMode() === 'optimized'"
              [class.text-primary-700]="routeMode() === 'optimized'"
              [class.font-bold]="routeMode() === 'optimized'"
              [class.text-slate-600]="routeMode() !== 'optimized'"
              class="px-3 py-1.5 rounded-md transition-all">
              Ruta Optimizada VRP
            </button>
            <button
              type="button"
              (click)="setRouteMode('baseline')"
              [class.bg-white]="routeMode() === 'baseline'"
              [class.shadow-sm]="routeMode() === 'baseline'"
              [class.text-slate-800]="routeMode() === 'baseline'"
              [class.font-bold]="routeMode() === 'baseline'"
              [class.text-slate-600]="routeMode() !== 'baseline'"
              class="px-3 py-1.5 rounded-md transition-all">
              Ruta Tradicional Fija
            </button>
          </div>

          <!-- Botón de Simulación de Incidente para el Pitch de la Expo -->
          <ui-button
            [variant]="incidentActive() ? 'destructive' : 'simulation'"
            size="sm"
            (buttonClick)="toggleIncident()"
            [ariaLabel]="incidentActive() ? 'Desactivar desvío por incidente' : 'Simular cierre vial y recalcular'">
            @if (incidentActive()) {
              <svg lucideTriangleAlert [size]="14" class="stroke-[2.2] mr-1.5 text-white"></svg>
              <span>Desvío Activo (Alóag Cerrada)</span>
            } @else {
              <svg lucideZap [size]="14" class="stroke-[2.2] mr-1.5 text-accent-400"></svg>
              <span>Simular Cierre Vial</span>
            }
          </ui-button>
        </div>
      </div>

      <!-- Barra de métricas rápidas de telemetría vial -->
      <div class="grid grid-cols-2 sm:grid-cols-4 border-b border-slate-200 text-xs divide-x divide-slate-100 bg-slate-50/60">
        <div class="p-3 sm:px-4">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold tracking-wider">Distancia Total</span>
          <span class="text-base sm:text-lg font-bold font-mono" [class.text-primary-600]="routeMode() === 'optimized'">
            {{ routeMode() === 'optimized' ? (incidentActive() ? '418 km' : '385 km') : '512 km' }}
          </span>
          <span class="text-[11px] text-secondary-700 font-semibold block">
            {{ routeMode() === 'optimized' ? (incidentActive() ? 'Ahorro: 18.3%' : 'Ahorro: 24.8% en diesel') : 'Línea base sin optimizar' }}
          </span>
        </div>

        <div class="p-3 sm:px-4">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold tracking-wider">Tiempo Estimado</span>
          <span class="text-base sm:text-lg font-bold font-mono text-slate-800">
            {{ routeMode() === 'optimized' ? (incidentActive() ? '7h 15m' : '6h 10m') : '9h 40m' }}
          </span>
          <span class="text-[11px] text-slate-500 block">
            {{ incidentActive() ? 'Desvío por Quevedo' : 'Itinerario a tiempo' }}
          </span>
        </div>

        <div class="p-3 sm:px-4">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold tracking-wider">Puntos de Servicio</span>
          <span class="text-base sm:text-lg font-bold font-mono text-slate-800">
            {{ servicePointCount() }} Sedes
          </span>
          <span class="text-[11px] text-slate-500 block">
            5 Provincias interconectadas
          </span>
        </div>

        <div class="p-3 sm:px-4">
          <span class="text-slate-500 block uppercase text-[10px] font-semibold tracking-wider mb-1">Cadena de Custodia</span>
          <ui-badge 
            [variant]="incidentActive() ? 'warning' : 'success'" 
            size="sm" 
            [withDot]="true" 
            [label]="incidentActive() ? 'Contingencia GPS' : 'Custodia Inmutable'">
          </ui-badge>
          <span class="text-[11px] text-slate-500 block mt-1 font-mono">
            {{ incidentActive() ? 'Rerouting activo' : 'UUID v7 verificado' }}
          </span>
        </div>
      </div>

      <!-- Layout Split-Screen 2:1 (Mapa 65% + Manifiesto de Carga 35%) -->
      <div class="grid grid-cols-1 lg:grid-cols-12 divide-y lg:divide-y-0 lg:divide-x divide-slate-200">
        <!-- Columna Izquierda: Mapa Leaflet Interactivo (8 columnas) -->
        <div class="lg:col-span-8 relative">
          <div class="h-[520px] w-full">
            <ui-map-view
              [markers]="markers()"
              [polylines]="polylines()">
            </ui-map-view>
          </div>

          <!-- Alerta Flotante cuando el incidente está simulado -->
          @if (incidentActive()) {
            <div class="absolute bottom-4 left-4 right-4 sm:right-auto sm:max-w-md bg-slate-900/90 text-white p-3 rounded-xl backdrop-blur-md shadow-lg border border-amber-500/40 text-xs flex items-start gap-2.5 z-[1000]">
              <svg lucideTriangleAlert [size]="16" class="text-amber-400 shrink-0 mt-0.5 stroke-[2.2]"></svg>
              <div>
                <p class="font-bold text-amber-300">Desvío de Emergencia Detectado (Vía Alóag Cerrada)</p>
                <p class="text-slate-300 text-[11px] mt-0.5">
                  El motor PostGIS recalculó la ruta vía Calacalí - La Concordia - Quevedo. Todas las entregas permanecen dentro de la ventana de seguridad horaria.
                </p>
              </div>
            </div>
          }
        </div>

        <!-- Columna Derecha: Manifiesto de Carga y Secuencia de Paradas (4 columnas) -->
        <div class="lg:col-span-4 bg-slate-50/70 p-4 sm:p-5 flex flex-col justify-between space-y-5">
          <!-- Sección de Capacidad del Vehículo -->
          <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-sm space-y-3">
            <div class="flex items-center justify-between text-xs">
              <span class="font-bold text-slate-800 uppercase tracking-wider text-[11px]">Capacidad Flota C-01</span>
              <span class="font-mono font-bold text-primary-700">74% Ocupado</span>
            </div>

            <div class="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
              <div class="bg-primary-600 h-2 rounded-full transition-all duration-500" style="width: 74%"></div>
            </div>

            <div class="grid grid-cols-3 gap-2 pt-1 text-center text-[10px]">
              <div class="bg-slate-50 p-1.5 rounded border border-slate-100">
                <span class="text-slate-400 block font-medium">Químicos</span>
                <span class="font-mono font-bold text-slate-800">45 Gal</span>
              </div>
              <div class="bg-slate-50 p-1.5 rounded border border-slate-100">
                <span class="text-slate-400 block font-medium">Kits M.F.</span>
                <span class="font-mono font-bold text-slate-800">180 Kits</span>
              </div>
              <div class="bg-slate-50 p-1.5 rounded border border-slate-100">
                <span class="text-slate-400 block font-medium">Equipos</span>
                <span class="font-mono font-bold text-slate-800">10 Und</span>
              </div>
            </div>
          </div>

          <!-- Secuencia de Paradas e Itinerario con Trazabilidad QR -->
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">
                Itinerario y Validación de Entrega
              </h3>
              <span class="text-[11px] font-mono text-slate-400">5 Paradas</span>
            </div>

            <div class="space-y-2.5">
              @for (stop of deliveryStops; track stop.id) {
                <div class="p-3 bg-white rounded-lg border border-slate-200/80 shadow-xs flex items-start gap-3 transition-colors hover:border-slate-300">
                  <div class="mt-0.5 shrink-0">
                    @if (stop.status === 'completed') {
                      <span class="h-5 w-5 rounded-full bg-emerald-100 text-emerald-700 flex items-center justify-center">
                        <svg lucideCheck [size]="12" class="stroke-[2.5]"></svg>
                      </span>
                    } @else if (stop.status === 'in_transit') {
                      <span class="h-5 w-5 rounded-full bg-primary-100 text-primary-700 flex items-center justify-center animate-pulse">
                        <svg lucideTruck [size]="12" class="stroke-[2.2]"></svg>
                      </span>
                    } @else {
                      <span class="h-5 w-5 rounded-full bg-slate-100 text-slate-400 flex items-center justify-center font-mono text-[10px]">
                        •
                      </span>
                    }
                  </div>

                  <div class="flex-1 min-w-0">
                    <div class="flex items-center justify-between gap-1">
                      <p class="text-xs font-bold text-slate-900 truncate">{{ stop.name }}</p>
                      <span class="font-mono text-[10px] text-slate-500 shrink-0">{{ stop.eta }}</span>
                    </div>
                    <p class="text-[11px] text-slate-500 mt-0.5 truncate">{{ stop.cargo }}</p>
                    <div class="flex items-center gap-2 mt-1">
                      <span class="font-mono text-[10px] text-primary-700 bg-primary-50 px-1.5 py-0.2 rounded font-medium">
                        {{ stop.huCode }}
                      </span>
                      @if (stop.status === 'completed') {
                        <span class="text-[10px] text-emerald-700 font-semibold">QR Verificado</span>
                      } @else if (stop.status === 'in_transit') {
                        <span class="text-[10px] text-primary-700 font-semibold">Próxima Parada</span>
                      } @else {
                        <span class="text-[10px] text-slate-400">En cola</span>
                      }
                    </div>
                  </div>
                </div>
              }
            </div>
          </div>

          <!-- Pie del Manifiesto con Firma Electrónica -->
          <div class="pt-2 border-t border-slate-200 text-[11px] text-slate-500 flex items-center justify-between">
            <span class="flex items-center gap-1">
              <svg lucideActivity [size]="12" class="text-emerald-500 stroke-[2]"></svg>
              <span>GPS Telemetry Link OK</span>
            </span>
            <span class="font-mono text-[10px]">ECDSA-P256 Offline Ready</span>
          </div>
        </div>
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

  readonly servicePointCount = computed(() => this.markers().length);

  readonly deliveryStops: DeliveryStop[] = [
    {
      id: 'stop-01',
      code: 'HUB-UIO',
      name: 'Bodega Calderón (Hub Central)',
      eta: '06:00 (Despachado)',
      status: 'completed',
      cargo: 'Carga total 1,850 kg inspeccionada',
      huCode: 'ORIGEN-UIO'
    },
    {
      id: 'stop-02',
      code: 'HOSP-SD',
      name: 'Hospital Gustavo Domínguez',
      eta: '08:30 (Recibido)',
      status: 'completed',
      cargo: '15 Gal Amonio + 50 Bolsas Rojas',
      huCode: 'HU-EC-2026-001'
    },
    {
      id: 'stop-03',
      code: 'HOSP-GYE',
      name: 'Hospital Teodoro Maldonado',
      eta: '13:45 (ETA 1h 15m)',
      status: 'in_transit',
      cargo: '20 Gal Detergente Neutro + 60 Paños',
      huCode: 'HU-EC-2026-002'
    },
    {
      id: 'stop-04',
      code: 'HOSP-CUE',
      name: 'Hospital Vicente Corral',
      eta: '18:00 (Programado)',
      status: 'scheduled',
      cargo: '10 Gal Desengrasante + 4 Mopas',
      huCode: 'HU-EC-2026-003'
    },
    {
      id: 'stop-05',
      code: 'HOSP-AMB',
      name: 'Hospital Docente Ambato',
      eta: '21:30 (Programado)',
      status: 'scheduled',
      cargo: 'Reposición preventiva de emergencia',
      huCode: 'HU-EC-2026-004'
    }
  ];

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
        badge: p.type === 'BODEGA_CENTRAL' ? 'BODEGA HUB' : 'PUNTO DE SERVICIO',
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

