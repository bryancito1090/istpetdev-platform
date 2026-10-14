import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import { ApiService, RoutePlanSummaryDto, RoutePlanDetailDto } from '@shared/api';
import { MapViewComponent, MapMarker, MapPolyline } from '@shared/ui';

@Component({
  selector: 'page-rutas',
  standalone: true,
  imports: [CommonModule, RouterLink, MapViewComponent],
  template: `
    <div class="max-w-5xl mx-auto px-4 py-8 space-y-6">
      <!-- Encabezado -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
            Planificación y Publicación de Rutas
          </h1>
          <p class="text-sm text-slate-500 mt-1">
            Revisa las rutas generadas para atender los pedidos de reabastecimiento y publícalas a los conductores.
          </p>
        </div>

        @if (session.canWrite()) {
          <button
            type="button"
            (click)="generatePlan()"
            [disabled]="actionLoading()"
            class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors disabled:opacity-50">
            {{ actionLoading() ? 'Calculando...' : '➕ Generar Nueva Ruta' }}
          </button>
        }
      </div>

      <!-- Banner de retroalimentación -->
      @if (feedbackMessage()) {
        <div 
          class="p-4 rounded-xl text-xs font-medium flex items-center justify-between transition-all"
          [ngClass]="feedbackMessage()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-rose-50 text-rose-800 border border-rose-200'">
          <div class="flex items-center gap-2">
            <span>{{ feedbackMessage()!.type === 'success' ? '✓' : '⚠' }}</span>
            <span>{{ feedbackMessage()!.text }}</span>
          </div>
          <button 
            type="button" 
            (click)="feedbackMessage.set(null)" 
            class="text-slate-400 hover:text-slate-600 font-bold ml-3 text-sm">
            ✕
          </button>
        </div>
      }

      <!-- Estado cargando -->
      @if (loading()) {
        <div class="p-8 text-center text-slate-500 text-sm bg-white border border-slate-200 rounded-xl">
          Cargando rutas de despacho...
        </div>
      }

      <!-- Estado vacío -->
      @if (!loading() && routePlans().length === 0) {
        <div class="p-8 text-center bg-white border border-slate-200 rounded-xl space-y-3">
          <p class="text-slate-600 text-sm">
            No hay rutas creadas en el sistema todavía.
          </p>
          <div class="flex justify-center gap-3">
            @if (session.canWrite()) {
              <button
                type="button"
                (click)="generatePlan()"
                [disabled]="actionLoading()"
                class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors disabled:opacity-50">
                {{ actionLoading() ? 'Calculando...' : 'Generar primera ruta ahora' }}
              </button>
            }
            <a 
              routerLink="/alertas" 
              class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-slate-100 text-slate-700 hover:bg-slate-200 transition-colors">
              Ver alertas pendientes
            </a>
          </div>
        </div>
      }

      <!-- Listado y detalle de rutas -->
      @if (!loading() && routePlans().length > 0) {
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- Columna izquierda: Lista de rutas -->
          <div class="space-y-3">
            <h2 class="text-xs font-bold text-slate-500 uppercase tracking-wider">
              Rutas encontradas ({{ routePlans().length }})
            </h2>
            <div class="space-y-2">
              @for (plan of routePlans(); track plan.id) {
                <div 
                  (click)="selectPlan(plan.id)"
                  class="p-4 rounded-xl border cursor-pointer transition-all bg-white"
                  [ngClass]="selectedPlanId() === plan.id ? 'border-primary-600 ring-2 ring-primary-100' : 'border-slate-200 hover:border-slate-300'">
                  <div class="flex items-center justify-between">
                    <span class="font-bold text-sm text-slate-900">{{ plan.code }}</span>
                    <span 
                      class="text-[10px] font-mono px-2 py-0.5 rounded uppercase font-semibold"
                      [ngClass]="{
                        'bg-emerald-50 text-emerald-700 border border-emerald-200': plan.status === 'Published',
                        'bg-amber-50 text-amber-700 border border-amber-200': plan.status === 'Proposed',
                        'bg-slate-50 text-slate-600 border border-slate-200': plan.status !== 'Published' && plan.status !== 'Proposed'
                      }">
                      {{ plan.status }}
                    </span>
                  </div>
                  <div class="text-xs text-slate-500 mt-2 flex justify-between font-mono">
                    <span>Vehículo: {{ plan.vehicleCode || 'C-01' }}</span>
                    <span>{{ plan.totalDistanceKm }} km</span>
                  </div>
                </div>
              }
            </div>
          </div>

          <!-- Columna derecha: Detalle y Paradas -->
          <div class="lg:col-span-2 space-y-4">
            @if (selectedDetail()) {
              <div class="bg-white border border-slate-200 rounded-xl p-5 space-y-4">
                <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-4">
                  <div>
                    <h3 class="font-bold text-slate-900 text-base">{{ selectedDetail()!.code }}</h3>
                    <p class="text-xs text-slate-500 font-mono mt-0.5">
                      Distancia: {{ selectedDetail()!.totalDistanceKm }} km | Tiempo: {{ selectedDetail()!.totalDurationMinutes }} min
                    </p>
                  </div>

                  @if (selectedDetail()!.status !== 'Published') {
                    @if (session.canWrite()) {
                      <button
                        type="button"
                        (click)="publishPlan(selectedDetail()!.id)"
                        [disabled]="actionLoading()"
                        class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-50">
                        {{ actionLoading() ? 'Publicando...' : '✓ Publicar a Conductor' }}
                      </button>
                    } @else {
                      <span class="text-xs font-semibold text-amber-700 bg-amber-50 border border-amber-200 px-3 py-1 rounded-lg">
                        Pendiente de publicación
                      </span>
                    }
                  } @else {
                    <span class="text-xs font-semibold text-emerald-700 bg-emerald-50 border border-emerald-200 px-3 py-1 rounded-lg">
                      ✓ Ruta en Ejecución
                    </span>
                  }
                </div>

                <!-- Mapa simple con los puntos -->
                @if (mapMarkers().length > 0) {
                  <div class="h-64 rounded-lg overflow-hidden border border-slate-200">
                    <ui-map-view 
                      [markers]="mapMarkers()" 
                      [polylines]="mapPolylines()"
                      [zoom]="7">
                    </ui-map-view>
                  </div>
                }

                <!-- Tabla de paradas ordenadas -->
                <div>
                  <h4 class="text-xs font-bold text-slate-700 uppercase mb-2">
                    Itinerario de Paradas
                  </h4>
                  <div class="border border-slate-200 rounded-lg overflow-hidden">
                    <table class="w-full text-left text-xs">
                      <thead class="bg-slate-50 text-[11px] text-slate-500 uppercase border-b border-slate-200">
                        <tr>
                          <th class="p-2.5">#</th>
                          <th class="p-2.5">Punto de Entrega</th>
                          <th class="p-2.5">Hora ETA</th>
                          <th class="p-2.5 text-right">Estado</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-slate-100">
                        @for (stop of selectedDetail()!.stops; track stop.sequence) {
                          <tr>
                            <td class="p-2.5 font-bold font-mono">{{ stop.sequence }}</td>
                            <td class="p-2.5 font-semibold text-slate-800">
                              {{ stop.servicePointName || stop.servicePointCode }}
                            </td>
                            <td class="p-2.5 font-mono text-slate-500">
                              {{ stop.etaUtc ? (stop.etaUtc | date:'shortTime') : 'Pendiente' }}
                            </td>
                            <td class="p-2.5 text-right font-mono text-[11px]">
                              {{ stop.status }}
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            } @else {
              <div class="bg-slate-50 border border-slate-200 rounded-xl p-8 text-center text-xs text-slate-500">
                Selecciona una ruta de la izquierda para ver su itinerario y mapa.
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class RutasPage implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);

  readonly routePlans = signal<RoutePlanSummaryDto[]>([]);
  readonly selectedPlanId = signal<string | null>(null);
  readonly selectedDetail = signal<RoutePlanDetailDto | null>(null);
  readonly loading = signal(true);
  readonly actionLoading = signal(false);
  readonly feedbackMessage = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  readonly mapMarkers = signal<MapMarker[]>([]);
  readonly mapPolylines = signal<MapPolyline[]>([]);

  ngOnInit(): void {
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading.set(true);
    this.apiService.getRoutePlans().subscribe(plans => {
      this.loading.set(false);
      this.routePlans.set(plans || []);
      if (plans && plans.length > 0) {
        if (!this.selectedPlanId() || !plans.some(p => p.id === this.selectedPlanId())) {
          this.selectPlan(plans[0].id);
        }
      } else {
        this.selectedPlanId.set(null);
        this.selectedDetail.set(null);
      }
    });
  }

  selectPlan(id: string): void {
    this.selectedPlanId.set(id);
    this.apiService.getRoutePlanById(id).subscribe(detail => {
      this.selectedDetail.set(detail);
      this.buildMap(detail);
    });
  }

  generatePlan(): void {
    if (!this.session.canWrite()) return;
    this.actionLoading.set(true);
    this.feedbackMessage.set(null);
    this.apiService.createRoutePlan({}).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        const count = res?.routes?.length ?? 0;
        this.feedbackMessage.set({
          type: 'success',
          text: count > 0 
            ? `¡Se generaron ${count} ruta(s) de distribución exitosamente!`
            : 'Planificación completada: no hay pedidos de reabastecimiento pendientes.'
        });
        if (count > 0) {
          const first = res.routes[0];
          const firstId = first.id || first.routePlanId;
          if (firstId) {
            this.selectedPlanId.set(firstId);
          }
        }
        this.loadPlans();
      },
      error: (err) => {
        this.actionLoading.set(false);
        const errMsg = err?.error?.message || err?.message || 'Error al solicitar el cálculo de rutas al motor logístico.';
        this.feedbackMessage.set({
          type: 'error',
          text: errMsg
        });
      }
    });
  }

  publishPlan(id: string): void {
    if (!this.session.canWrite()) return;
    this.actionLoading.set(true);
    this.feedbackMessage.set(null);
    this.apiService.publishRoutePlan(id).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.feedbackMessage.set({
          type: 'success',
          text: 'Ruta publicada al conductor exitosamente.'
        });
        this.selectPlan(id);
        this.loadPlans();
      },
      error: (err) => {
        this.actionLoading.set(false);
        const errMsg = err?.error?.message || err?.message || 'Error al publicar la ruta.';
        this.feedbackMessage.set({
          type: 'error',
          text: errMsg
        });
      }
    });
  }

  private buildMap(detail: RoutePlanDetailDto | null): void {
    if (!detail) {
      this.mapMarkers.set([]);
      this.mapPolylines.set([]);
      return;
    }

    // Traemos las coordenadas de los puntos de servicio
    this.apiService.getServicePoints().subscribe(points => {
      const markers: MapMarker[] = [];
      const coords: [number, number][] = [];

      for (const stop of detail.stops) {
        const pt = points.find(p => p.id === stop.servicePointId || p.code === stop.servicePointCode);
        if (pt && pt.latitude && pt.longitude) {
          markers.push({
            id: stop.servicePointId,
            latitude: pt.latitude,
            longitude: pt.longitude,
            title: `${stop.sequence}. ${pt.name || pt.code}`,
            badge: `#${stop.sequence}`
          });
          coords.push([pt.latitude, pt.longitude]);
        }
      }

      this.mapMarkers.set(markers);
      if (coords.length > 1) {
        this.mapPolylines.set([{
          id: 'route-line',
          coordinates: coords,
          color: '#00A7C4',
          weight: 4
        }]);
      } else {
        this.mapPolylines.set([]);
      }
    });
  }
}
