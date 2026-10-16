import { Component, inject, signal, computed, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import {
  ApiService,
  RoutePlanSummaryDto,
  RoutePlanDetailDto,
  RouteStopLineDto,
  RouteProductSummaryDto,
  RouteDriverAssignmentDto,
  DeliveryOrderDto,
  RouteSuggestionOptionDto,
  RouteCandidateStop,
  ServicePoint,
  AgentRunViewDto,
  RejectedCaptureDto,
  RouteGeometryDto,
  DriverOptionDto
} from '@shared/api';
import { MapViewComponent, MapMarker, MapPolyline } from '@shared/ui';
import { forkJoin } from 'rxjs';

/** Un color por vehículo, en el orden de sus códigos; distinguibles entre sí sobre el mapa. */
export const ROUTE_COLORS = ['#2563eb', '#dc2626', '#16a34a', '#9333ea', '#ea580c', '#0891b2',
  '#c026d3', '#65a30d', '#b45309', '#0f766e', '#db2777', '#4f46e5'];

export interface RouteLegendItem {
  planId: string;
  vehicleCode: string;
  color: string;
  km: number;
  stops: number;
  source: 'osrm' | 'approximate';
  selected: boolean;
}

/** Rutas vigentes: la última revisión de cada vehículo, sin canceladas, rechazadas ni reemplazadas. */
export function currentPlans(plans: RoutePlanSummaryDto[], limit = 20): RoutePlanSummaryDto[] {
  const latest = new Map<string, RoutePlanSummaryDto>();
  for (const plan of plans) {
    if (['Superseded', 'Cancelled', 'Rejected'].includes(plan.status)) continue;
    const key = plan.vehicleCode || plan.vehicleId;
    const current = latest.get(key);
    if (!current || plan.revision > current.revision) latest.set(key, plan);
  }
  return [...latest.values()]
    .sort((a, b) => (a.vehicleCode || a.vehicleId).localeCompare(b.vehicleCode || b.vehicleId))
    .slice(0, limit);
}

@Component({
  selector: 'page-rutas',
  standalone: true,
  imports: [CommonModule, FormsModule, MapViewComponent],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8 space-y-6">
      <!-- Encabezado Principal -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
              Planificación y Asignación de Rutas
            </h1>
            @if (isAuditor()) {
              <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200">
                Panel de Auditoría (Solo Lectura)
              </span>
            }
          </div>
          <p class="text-sm text-slate-500 mt-1">
            @if (isAuditor()) {
              Auditoría y fiscalización de itinerarios: verificación de ventanas de atención, ETAs calculadas, tiempos de viaje y kilometraje.
            } @else {
              Genera rutas basadas en pedidos con abastecimiento automático desde la bodega más cercana, opciones multi-recogida y selección de vehículos.
            }
          </p>
        </div>

        @if (session.canWrite()) {
          <div class="flex items-center gap-2">
            <!-- Botón Pedir al Agente IA (n8n) -->
            <button
              type="button"
              (click)="triggerAgentWatch()"
              [disabled]="agentLoading() || actionLoading()"
              class="inline-flex items-center gap-1.5 text-xs font-semibold px-4 py-2 rounded-lg bg-indigo-600 text-white hover:bg-indigo-700 transition-colors shadow-sm disabled:opacity-50">
              @if (agentLoading()) {
                <span class="inline-block animate-spin h-3 w-3 border-2 border-white border-t-transparent rounded-full"></span>
                <span>Consultando agente…</span>
              } @else {
                <span>🤖 Pedir al Agente IA</span>
              }
            </button>

            <!-- Botón de Emergencia -->
            <button
              type="button"
              (click)="triggerEmergencyFlow()"
              [disabled]="agentLoading() || actionLoading()"
              class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-rose-600 text-white hover:bg-rose-700 transition-colors shadow-sm disabled:opacity-50">
              <span>Proponer Ruta de Emergencia</span>
            </button>

            <!-- Botón Nueva Ruta por Pedidos -->
            <button
              type="button"
              (click)="openOrderRoutingModal()"
              [disabled]="agentLoading() || actionLoading()"
              class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors shadow-sm disabled:opacity-50">
              <span>+ Nueva Ruta por Pedidos</span>
            </button>
          </div>
        }
      </div>

      <!-- Selector de Pestañas: Rutas / Bandeja de Rechazos -->
      <div class="flex items-center gap-6 border-b border-slate-200">
        <button
          type="button"
          (click)="activeTab.set('rutas')"
          class="pb-3 text-sm font-bold border-b-2 transition-colors flex items-center gap-2"
          [ngClass]="activeTab() === 'rutas' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-700'">
          <span>Rutas Planificadas</span>
          <span class="text-xs px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 font-mono">{{ routePlans().length }}</span>
        </button>

        <button
          type="button"
          (click)="activeTab.set('rechazos'); loadRejectedCaptures()"
          class="pb-3 text-sm font-bold border-b-2 transition-colors flex items-center gap-2"
          [ngClass]="activeTab() === 'rechazos' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-700'">
          <span>Bandeja de Capturas Rechazadas</span>
          @if (rejectedCaptures().length > 0) {
            <span class="text-xs px-2 py-0.5 rounded-full bg-rose-100 text-rose-700 font-bold font-mono">{{ rejectedCaptures().length }}</span>
          }
        </button>
      </div>

      @if (activeTab() === 'rutas') {
        <!-- Banner de retroalimentación -->
        @if (feedbackMessage()) {
        <div 
          class="p-4 rounded-xl text-xs font-medium flex items-center justify-between transition-all"
          [ngClass]="feedbackMessage()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-rose-50 text-rose-800 border border-rose-200'">
          <div class="flex items-center gap-2">
            <span class="font-bold">[{{ feedbackMessage()!.type === 'success' ? 'Éxito' : 'Aviso' }}]</span>
            <span>{{ feedbackMessage()!.text }}</span>
          </div>
          <button (click)="feedbackMessage.set(null)" class="text-slate-400 hover:text-slate-600 font-semibold ml-3 text-xs">Cerrar</button>
        </div>
      }

      <!-- Tarjeta del Agente IA n8n -->
      @if (agentRun()) {
        <div class="bg-gradient-to-r from-indigo-50/80 via-white to-purple-50/50 border border-indigo-200 rounded-2xl p-5 shadow-sm space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-indigo-100 pb-3">
            <div class="flex items-center gap-2.5">
              <span class="p-1.5 bg-indigo-600 text-white rounded-lg text-xs font-bold leading-none">IA</span>
              <div>
                <h3 class="text-sm font-bold text-slate-900">Propuesta del agente logístico</h3>
                <p class="text-[11px] text-slate-500">
                  Ejecución {{ agentRun()!.runId.substring(0, 8) }} · Modelo: {{ agentRun()!.model || 'No informado' }} · Latencia: {{ agentRun()!.latencyMs }} ms
                </p>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span
                class="text-xs px-2.5 py-1 rounded-full font-semibold border"
                [ngClass]="agentRun()!.status === 'Completed' ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-amber-50 text-amber-700 border-amber-200'">
                {{ agentRun()!.status === 'Completed' ? '✓ Completado' : agentRun()!.status === 'Fallback' ? '⚠ Respuesta determinista' : agentRun()!.status === 'Running' ? 'En ejecución' : 'Falló la ejecución' }}
              </span>
              <button (click)="agentRun.set(null)" class="text-slate-400 hover:text-slate-600 text-sm ml-2">✕</button>
            </div>
          </div>

          <!-- Decisiones generadas por el Agente -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            @for (decision of agentRun()!.decisions; track decision.agentDecisionId) {
              <div class="bg-white border border-indigo-100 rounded-xl p-3.5 space-y-2 text-xs shadow-xs">
                <div class="flex items-center justify-between">
                  <span class="font-bold text-slate-900">Vehículo: {{ decision.vehicleCode }}</span>
                  <span class="text-[11px] font-medium text-slate-500 bg-slate-100 px-2 py-0.5 rounded">
                    Fuente: {{ decision.source === 'Llm' ? 'DeepSeek LLM' : 'Plantilla' }}
                  </span>
                </div>
                <p class="text-slate-700 leading-relaxed">{{ decision.explanation }}</p>
                @if (decision.driverMessage) {
                  <div class="bg-slate-50 border border-slate-200 rounded-lg p-2 text-[11px] text-slate-600">
                    <span class="font-semibold text-slate-700">Mensaje al conductor:</span> "{{ decision.driverMessage }}"
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }

      <!-- Estado cargando -->
      @if (loading()) {
        <div class="p-8 text-center text-slate-500 text-sm bg-white border border-slate-200 rounded-xl">
          Cargando rutas de distribución...
        </div>
      }

      <!-- Estado vacío -->
      @if (!loading() && routePlans().length === 0) {
        <div class="p-8 text-center bg-white border border-slate-200 rounded-xl space-y-3">
          <p class="text-slate-600 text-sm">No hay rutas planificadas todavía.</p>
          <div class="flex justify-center gap-3">
            @if (session.canWrite()) {
              <button
                type="button"
                (click)="openOrderRoutingModal()"
                class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 shadow-sm">
                Crear primera ruta por pedidos
              </button>
              <button
                type="button"
                (click)="triggerEmergencyFlow()"
                class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-rose-600 text-white hover:bg-rose-700">
                Evaluar requerimientos de emergencia
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
              Rutas registradas ({{ routePlans().length }})
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
                    <div class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-600 font-mono mt-1">
                      <span><strong>Distancia:</strong> {{ selectedDetail()!.totalDistanceKm }} km</span>
                      <span><strong>Duración:</strong> {{ selectedDetail()!.totalDurationMinutes }} min</span>
                      <span><strong>Vehículo:</strong> {{ selectedDetail()!.vehicleCode || 'C-01' }}</span>
                      <span><strong>Salida:</strong> {{ selectedDetail()!.plannedStartUtc | date:'shortTime' }}</span>
                      @if (selectedDetail()!.plannedEndUtc) {
                        <span><strong>Fin Est.:</strong> {{ selectedDetail()!.plannedEndUtc | date:'shortTime' }}</span>
                      }
                    </div>
                  </div>

                  @if (selectedDetail()!.status !== 'Published') {
                    @if (session.canWrite()) {
                      <label class="text-xs text-slate-600 flex items-center gap-2">
                        Conductor
                        <select class="border border-slate-300 rounded-lg px-2 py-1.5 text-xs" aria-label="Conductor de la ruta"
                          [ngModel]="selectedDriverId()" (ngModelChange)="selectedDriverId.set($event)">
                          <option value="">Elegir conductor…</option>
                          @for (driver of drivers(); track driver.accountId) {
                            <option [value]="driver.accountId">{{ driver.displayName }}</option>
                          }
                        </select>
                      </label>
                      <button
                        type="button"
                        (click)="publishPlan(selectedDetail()!.id)"
                        [disabled]="actionLoading() || !selectedDriverId()"
                        [title]="selectedDriverId() ? '' : 'Elige el conductor: sin conductor, la ruta no aparece en su teléfono'"
                        class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-50">
                        {{ actionLoading() ? 'Publicando...' : 'Publicar a Conductor' }}
                      </button>
                      @if (drivers().length === 0) {
                        <span class="text-[11px] text-amber-700">No hay conductores activos: Gerencia debe crear una cuenta de Conductor.</span>
                      }
                    } @else {
                      <span class="text-xs font-semibold text-amber-700 bg-amber-50 border border-amber-200 px-3 py-1 rounded-lg">
                        Pendiente de publicación
                      </span>
                    }
                  } @else {
                    <span class="text-xs font-semibold text-emerald-700 bg-emerald-50 border border-emerald-200 px-3 py-1 rounded-lg">
                      Ruta Publicada al Conductor
                    </span>
                  }
                </div>

                <!-- #160: a qué bodega sale, con qué vehículo y qué conductor -->
                <div class="flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-slate-600" data-route-assignment>
                  <span><span class="text-slate-400">Bodega:</span> <strong>{{ selectedDetail()!.originWarehouseName || 'Sin bodega' }}</strong></span>
                  <span><span class="text-slate-400">Vehículo:</span> <strong>{{ selectedDetail()!.vehicleCode || '—' }}</strong></span>
                  @if (selectedDetail()!.status === 'Published') {
                    <span>
                      <span class="text-slate-400">Conductor:</span>
                      <strong [class.text-rose-700]="!selectedDetail()!.driverName">{{ selectedDetail()!.driverName || 'Sin conductor asignado' }}</strong>
                    </span>
                    @if ((selectedDetail()!.unassignedDeliveries ?? 0) > 0) {
                      <span class="text-rose-700">{{ selectedDetail()!.unassignedDeliveries }} entregas sin conductor: no aparecen en ningún teléfono.</span>
                    }
                    @if (session.canWrite()) {
                      <span class="flex items-center gap-2">
                        <select class="border border-slate-300 rounded-lg px-2 py-1 text-xs" aria-label="Reasignar conductor"
                          [ngModel]="reassignDriverId()" (ngModelChange)="reassignDriverId.set($event)">
                          <option value="">Cambiar conductor…</option>
                          @for (driver of drivers(); track driver.accountId) {
                            <option [value]="driver.accountId">{{ driver.displayName }}</option>
                          }
                        </select>
                        <button type="button" class="text-xs font-semibold text-slate-700 underline disabled:opacity-50"
                          [disabled]="!reassignDriverId() || actionLoading()" (click)="reassignDriver(selectedDetail()!.id)">
                          Asignar
                        </button>
                      </span>
                    }
                  }
                </div>

                <!-- Mapa -->
                @if (mapMarkers().length > 0) {
                  <div class="h-72 rounded-lg overflow-hidden border border-slate-200">
                    <ui-map-view 
                      [markers]="mapMarkers()" 
                      [polylines]="mapPolylines()"
                      [zoom]="7">
                    </ui-map-view>
                  </div>
                }
                @if (routeLegend().length > 0) {
                  <div class="flex flex-wrap gap-2" aria-label="Leyenda de rutas">
                    @for (item of routeLegend(); track item.planId) {
                      <button type="button" (click)="selectPlan(item.planId)" [attr.data-legend]="item.vehicleCode"
                        class="inline-flex items-center gap-2 text-[11px] px-2 py-1 rounded-lg border bg-white hover:bg-slate-50"
                        [class.border-slate-900]="item.selected" [class.border-slate-200]="!item.selected">
                        <span class="inline-block w-3 h-3 rounded-full" [style.background-color]="item.color"></span>
                        <span class="font-semibold text-slate-800">{{ item.vehicleCode }}</span>
                        <span class="text-slate-500">{{ item.km | number:'1.0-1' }} km {{ item.source === 'osrm' ? 'por calles' : 'aprox.' }} · {{ item.stops }} paradas</span>
                      </button>
                    }
                  </div>
                  @if (routeLegendHasApproximate()) {
                    <p class="text-[11px] text-amber-700">
                      Las líneas punteadas son aproximadas (rectas entre paradas): el servicio de rutas por calles no respondió o no está configurado.
                    </p>
                  }
                }

                <!-- Pestañas de Detalle de la Planificación (#164) -->
                <div class="border-b border-slate-200 flex flex-wrap gap-2 pt-2">
                  <button
                    type="button"
                    (click)="detailTab.set('paradas')"
                    class="pb-2.5 px-3 text-xs font-bold border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'paradas' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-700'">
                    <span>📍 Itinerario y Paradas</span>
                    <span class="text-[10px] px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-600 font-mono">
                      {{ selectedDetail()!.stops.length }}
                    </span>
                  </button>

                  <button
                    type="button"
                    (click)="detailTab.set('productos')"
                    class="pb-2.5 px-3 text-xs font-bold border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'productos' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-700'">
                    <span>📦 Por Producto (SKUs)</span>
                    @if (aggregatedProducts().length > 0) {
                      <span class="text-[10px] px-1.5 py-0.5 rounded-full bg-primary-50 text-primary-700 font-bold font-mono">
                        {{ aggregatedProducts().length }}
                      </span>
                    }
                  </button>

                  <button
                    type="button"
                    (click)="detailTab.set('conductor')"
                    class="pb-2.5 px-3 text-xs font-bold border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'conductor' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-700'">
                    <span>🚚 Por Conductor</span>
                    <span class="text-[10px] px-1.5 py-0.5 rounded-full font-mono"
                      [ngClass]="selectedDetail()!.driverName ? 'bg-emerald-50 text-emerald-700 font-bold' : 'bg-amber-50 text-amber-700'">
                      {{ selectedDetail()!.driverName ? 'Asignado' : 'Pendiente' }}
                    </span>
                  </button>
                </div>

                <!-- CONTENIDO PESTAÑA 1: ITINERARIO Y PARADAS -->
                @if (detailTab() === 'paradas') {
                  <div class="space-y-3">
                    <div class="flex items-center justify-between">
                      <div>
                        <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wide">
                          Paradas, Productos, Lotes y Bultos
                        </h4>
                        <span class="text-[11px] text-slate-500 font-mono">
                          {{ selectedDetail()!.stops.length }} paradas programadas · {{ totalPlannedUnits() | number:'1.0-2' }} unidades totales
                        </span>
                      </div>
                      <div class="flex items-center gap-2">
                        <button
                          type="button"
                          (click)="expandAllStops()"
                          class="text-[11px] font-semibold text-primary-600 hover:text-primary-800 underline">
                          Expandir todo
                        </button>
                        <span class="text-slate-300">|</span>
                        <button
                          type="button"
                          (click)="collapseAllStops()"
                          class="text-[11px] font-semibold text-slate-500 hover:text-slate-700 underline">
                          Colapsar todo
                        </button>
                      </div>
                    </div>

                    <div class="space-y-3">
                      @for (stop of selectedDetail()!.stops; track stop.sequence) {
                        <div class="border border-slate-200 rounded-lg overflow-hidden bg-white shadow-xs transition-all">
                          <!-- Cabecera de la parada (clic para colapsar/expandir) -->
                          <div
                            (click)="toggleStopExpansion(stop.sequence)"
                            class="p-3 bg-slate-50 hover:bg-slate-100 cursor-pointer flex flex-col sm:flex-row sm:items-center justify-between gap-2 select-none border-b border-slate-200">
                            <div class="flex items-center gap-2.5">
                              <span class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold font-mono"
                                [ngClass]="stop.sequence === 1 ? 'bg-amber-200 text-amber-900' : 'bg-primary-100 text-primary-800'">
                                {{ stop.sequence }}
                              </span>
                              <div>
                                <div class="flex items-center gap-2">
                                  <span class="px-2 py-0.5 rounded text-[10px] font-bold"
                                    [ngClass]="stop.sequence === 1 ? 'bg-amber-100 text-amber-800 border border-amber-200' : 'bg-blue-100 text-blue-800 border border-blue-200'">
                                    {{ stop.sequence === 1 ? 'RECOGIDA' : 'ENTREGA' }}
                                  </span>
                                  <span class="font-bold text-xs text-slate-900">
                                    {{ stop.servicePointName || stop.servicePointCode }}
                                  </span>
                                  <span class="text-[10px] font-mono text-slate-400">({{ stop.servicePointCode }})</span>
                                </div>
                                <div class="flex flex-wrap gap-x-3 text-[11px] text-slate-500 font-mono mt-0.5">
                                  <span><strong>Ventana:</strong> {{ stop.windowStart || '08:00' }} - {{ stop.windowEnd || '17:00' }}</span>
                                  <span><strong>Llegada ETA:</strong> <strong class="text-primary-700">{{ stop.etaUtc ? (stop.etaUtc | date:'shortTime') : '08:30' }}</strong></span>
                                  <span><strong>Atención:</strong> {{ stop.serviceMinutes || 15 }} min</span>
                                </div>
                              </div>
                            </div>

                            <div class="flex items-center gap-2 self-end sm:self-center">
                              @if (stop.lines && stop.lines.length > 0) {
                                <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 font-mono">
                                  {{ stop.lines.length }} {{ stop.lines.length === 1 ? 'producto' : 'productos' }}
                                </span>
                              }
                              @if (stop.deliveryStatus) {
                                <span class="px-2 py-0.5 rounded text-[10px] font-semibold"
                                  [ngClass]="stop.deliveryStatus === 'Delivered' ? 'bg-emerald-100 text-emerald-800' : (stop.deliveryStatus === 'Proposed' ? 'bg-amber-50 text-amber-800' : 'bg-slate-100 text-slate-700')">
                                  Entrega: {{ stop.deliveryStatus }}
                                </span>
                              }
                              <span class="px-2 py-0.5 rounded text-[10px] font-semibold"
                                [ngClass]="stop.status === 'Completed' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'">
                                Parada: {{ stop.status }}
                              </span>
                              <span class="text-xs text-slate-400 font-mono">
                                {{ expandedStopSeqs().has(stop.sequence) ? '▲' : '▼' }}
                              </span>
                            </div>
                          </div>

                          <!-- Contenido expandido: Detalle de productos, lotes y bultos de la parada -->
                          @if (expandedStopSeqs().has(stop.sequence)) {
                            <div class="p-3 bg-white space-y-3">
                              <!-- Metadatos de la entrega y bultos -->
                              @if (stop.deliveryCode || (stop.handlingUnits && stop.handlingUnits.length > 0)) {
                                <div class="flex flex-wrap items-center justify-between gap-2 p-2 rounded-lg bg-slate-50 border border-slate-100 text-[11px]">
                                  <div class="flex items-center gap-2">
                                    <span class="text-slate-500 font-mono">Entrega:</span>
                                    <strong class="font-mono text-slate-800">{{ stop.deliveryCode || 'DEL-...' }}</strong>
                                    <span class="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-white border border-slate-200">
                                      {{ stop.deliveryStatus || 'Planificada' }}
                                    </span>
                                  </div>

                                  @if (stop.handlingUnits && stop.handlingUnits.length > 0) {
                                    <div class="flex flex-wrap items-center gap-1.5">
                                      <span class="text-slate-500 font-mono">Bultos:</span>
                                      @for (hu of stop.handlingUnits; track hu.id) {
                                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-white border border-slate-300 font-mono text-[10px]">
                                          <span>📦 {{ hu.code }}</span>
                                          <span class="text-[9px] px-1 rounded bg-slate-100 text-slate-600 font-bold uppercase">{{ hu.status }}</span>
                                        </span>
                                      }
                                    </div>
                                  }
                                </div>
                              }

                              <!-- Tabla de productos y lotes de la parada -->
                              @if (stop.lines && stop.lines.length > 0) {
                                @if (stop.kind === 'Pickup') {
                                  <p class="text-xs font-semibold text-amber-800 mb-1" data-pickup-load>Cargar en esta bodega (se entrega en las paradas siguientes):</p>
                                }
                                <div class="overflow-x-auto">
                                  <table class="w-full text-left text-xs min-w-[500px]">
                                    <thead class="bg-slate-50 text-[10px] text-slate-500 uppercase border-b border-slate-200 font-mono">
                                      <tr>
                                        <th class="p-2">SKU</th>
                                        <th class="p-2">Producto</th>
                                        <th class="p-2">Lote Asignado</th>
                                        <th class="p-2 text-right">Cantidad Planificada</th>
                                        <th class="p-2">Bulto / HU</th>
                                        <th class="p-2 text-right">Estado</th>
                                      </tr>
                                    </thead>
                                    <tbody class="divide-y divide-slate-100">
                                      @for (line of stop.lines; track line.productId + (line.lotId || '') + line.sku) {
                                        <tr class="hover:bg-slate-50/50">
                                          <td class="p-2 font-mono font-bold text-slate-900">{{ line.sku }}</td>
                                          <td class="p-2 text-slate-800 font-medium">{{ line.productName }}</td>
                                          <td class="p-2">
                                            @if (line.lotCode) {
                                              <span class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-amber-50 text-amber-800 border border-amber-200 font-mono text-[10px]">
                                                <span>🏷️ {{ line.lotCode }}</span>
                                                @if (line.lotExpiresOn) {
                                                  <span class="text-[9px] text-amber-600">({{ line.lotExpiresOn }})</span>
                                                }
                                              </span>
                                            } @else {
                                              <span class="text-slate-400 font-mono text-[11px]">— Asignación al publicar —</span>
                                            }
                                          </td>
                                          <td class="p-2 text-right font-mono font-bold text-slate-900">
                                            {{ line.plannedQuantity | number:'1.0-2' }} <span class="text-slate-500 font-normal text-[10px]">{{ line.baseUnit }}</span>
                                          </td>
                                          <td class="p-2">
                                            @if (line.handlingUnitCode) {
                                              <span class="px-1.5 py-0.5 rounded bg-slate-100 text-slate-700 font-mono text-[10px]">
                                                {{ line.handlingUnitCode }}
                                              </span>
                                            } @else if (stop.handlingUnits && stop.handlingUnits.length > 0) {
                                              <span class="px-1.5 py-0.5 rounded bg-slate-100 text-slate-700 font-mono text-[10px]">
                                                {{ stop.handlingUnits[0].code }}
                                              </span>
                                            } @else {
                                              <span class="text-slate-400 font-mono text-[10px]">Sin consolidar</span>
                                            }
                                          </td>
                                          <td class="p-2 text-right">
                                            <span class="px-1.5 py-0.5 rounded text-[10px] font-semibold"
                                              [ngClass]="line.deliveryStatus === 'Delivered' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'">
                                              {{ line.deliveryStatus || stop.deliveryStatus || 'Planificada' }}
                                            </span>
                                          </td>
                                        </tr>
                                      }
                                    </tbody>
                                  </table>
                                </div>
                              } @else {
                                <div class="p-3 text-center text-xs text-slate-500 bg-slate-50 rounded-lg">
                                  @if (stop.kind === 'Pickup') {
                                    <span>Bodega de recogida: no hay productos para cargar aquí.</span>
                                  } @else {
                                    <span>Sin productos registrados para esta parada.</span>
                                  }
                                </div>
                              }
                            </div>
                          }
                        </div>
                      }
                    </div>
                  </div>
                }

                <!-- CONTENIDO PESTAÑA 2: VISTA AGREGADA POR PRODUCTO (SUMA POR SKU) -->
                @if (detailTab() === 'productos') {
                  <div class="space-y-4">
                    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                      <div>
                        <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wide">
                          Resumen Agregado de Carga por Producto (SKU)
                        </h4>
                        <p class="text-[11px] text-slate-500">
                          Total consolidado de insumos que salen del almacén en este vehículo y ruta.
                        </p>
                      </div>
                      <div class="flex items-center gap-3 text-xs font-mono">
                        <span class="px-2 py-1 rounded bg-slate-100 text-slate-700">
                          <strong>{{ aggregatedProducts().length }}</strong> SKUs distintos
                        </span>
                        <span class="px-2 py-1 rounded bg-primary-50 text-primary-800 border border-primary-200">
                          <strong>{{ totalPlannedUnits() | number:'1.0-2' }}</strong> unidades totales
                        </span>
                      </div>
                    </div>

                    @if (aggregatedProducts().length > 0) {
                      <div class="border border-slate-200 rounded-lg overflow-x-auto bg-white">
                        <table class="w-full text-left text-xs min-w-[600px]">
                          <thead class="bg-slate-50 text-[10px] text-slate-500 uppercase border-b border-slate-200 font-mono">
                            <tr>
                              <th class="p-3">SKU</th>
                              <th class="p-3">Nombre del Producto</th>
                              <th class="p-3 text-right">Cantidad Total</th>
                              <th class="p-3">Unidad</th>
                              <th class="p-3">Lotes Involucrados</th>
                              <th class="p-3 text-center">Paradas</th>
                              <th class="p-3 text-right">Estados</th>
                            </tr>
                          </thead>
                          <tbody class="divide-y divide-slate-100">
                            @for (item of aggregatedProducts(); track item.sku) {
                              <tr class="hover:bg-slate-50/50">
                                <td class="p-3 font-mono font-bold text-slate-900">{{ item.sku }}</td>
                                <td class="p-3 font-medium text-slate-800">{{ item.productName }}</td>
                                <td class="p-3 text-right font-mono font-bold text-primary-700 text-sm">
                                  {{ item.totalPlannedQuantity | number:'1.0-2' }}
                                </td>
                                <td class="p-3 font-mono text-slate-600">{{ item.baseUnit }}</td>
                                <td class="p-3">
                                  @if (item.lots && item.lots.length > 0) {
                                    <div class="flex flex-wrap gap-1">
                                      @for (l of item.lots; track l.lotCode) {
                                        <span class="px-1.5 py-0.5 rounded bg-amber-50 text-amber-800 border border-amber-200 font-mono text-[10px]">
                                          {{ l.lotCode }}: <strong>{{ l.quantity | number:'1.0-1' }}</strong>
                                        </span>
                                      }
                                    </div>
                                  } @else {
                                    <span class="text-slate-400 font-mono text-[10px]">Asignación automática</span>
                                  }
                                </td>
                                <td class="p-3 text-center font-mono">
                                  <span class="px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 font-bold text-[10px]">
                                    {{ item.stopsCount }} {{ item.stopsCount === 1 ? 'parada' : 'paradas' }}
                                  </span>
                                </td>
                                <td class="p-3 text-right">
                                  @for (st of item.deliveryStatuses; track st) {
                                    <span class="px-1.5 py-0.5 rounded text-[10px] font-semibold"
                                      [ngClass]="st === 'Delivered' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'">
                                      {{ st }}
                                    </span>
                                  }
                                </td>
                              </tr>
                            }
                          </tbody>
                        </table>
                      </div>
                    } @else {
                      <div class="p-8 text-center text-xs text-slate-500 bg-white border border-slate-200 rounded-lg">
                        No hay productos planificados en esta ruta aún.
                      </div>
                    }
                  </div>
                }

                <!-- CONTENIDO PESTAÑA 3: VISTA POR CONDUCTOR -->
                @if (detailTab() === 'conductor') {
                  <div class="space-y-4">
                    <!-- Ficha del conductor de esta ruta -->
                    <div class="bg-slate-50 border border-slate-200 rounded-xl p-4 space-y-3">
                      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                        <div class="flex items-center gap-3">
                          <div class="w-10 h-10 rounded-full bg-primary-100 text-primary-800 flex items-center justify-center font-bold text-sm">
                            🚚
                          </div>
                          <div>
                            <span class="text-[10px] uppercase font-bold text-slate-400">Conductor Asignado a Esta Ruta</span>
                            <h4 class="font-bold text-slate-900 text-sm">
                              {{ selectedDetail()!.driverName || 'Sin conductor asignado' }}
                            </h4>
                          </div>
                        </div>

                        <div class="flex items-center gap-2">
                          <span class="px-2.5 py-1 rounded-full text-xs font-semibold"
                            [ngClass]="selectedDetail()!.driverName ? 'bg-emerald-100 text-emerald-800 border border-emerald-200' : 'bg-rose-100 text-rose-800 border border-rose-200'">
                            {{ selectedDetail()!.driverName ? 'Conductor Asignado' : 'Pendiente de Asignación' }}
                          </span>
                        </div>
                      </div>

                      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-2 border-t border-slate-200/60 text-xs">
                        <div>
                          <span class="text-slate-400 block text-[10px]">Vehículo</span>
                          <strong class="font-mono text-slate-800">{{ selectedDetail()!.vehicleCode || '—' }}</strong>
                        </div>
                        <div>
                          <span class="text-slate-400 block text-[10px]">Bodega Origen</span>
                          <strong class="text-slate-800">{{ selectedDetail()!.originWarehouseName || 'Central' }}</strong>
                        </div>
                        <div>
                          <span class="text-slate-400 block text-[10px]">Entregas Totales</span>
                          <strong class="font-mono text-slate-800">{{ selectedDetail()!.deliveriesCount || selectedDetail()!.stops.length }}</strong>
                        </div>
                        <div>
                          <span class="text-slate-400 block text-[10px]">Entregas sin Conductor</span>
                          <strong [class.text-rose-700]="(selectedDetail()!.unassignedDeliveries ?? 0) > 0" class="font-mono">
                            {{ selectedDetail()!.unassignedDeliveries ?? 0 }}
                          </strong>
                        </div>
                      </div>
                    </div>

                    <!-- Rutas que lleva este conductor -->
                    <div class="space-y-2">
                      <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wide">
                        Rutas a Cargo de Este Conductor ({{ driverAssignedRoutes().length }})
                      </h4>

                      @if (driverAssignedRoutes().length > 0) {
                        <div class="border border-slate-200 rounded-lg overflow-x-auto bg-white">
                          <table class="w-full text-left text-xs min-w-[550px]">
                            <thead class="bg-slate-50 text-[10px] text-slate-500 uppercase border-b border-slate-200 font-mono">
                              <tr>
                                <th class="p-2.5">Código de Ruta</th>
                                <th class="p-2.5">Vehículo</th>
                                <th class="p-2.5">Salida Programada</th>
                                <th class="p-2.5 text-right">Distancia</th>
                                <th class="p-2.5 text-right">Duración</th>
                                <th class="p-2.5 text-center">Paradas</th>
                                <th class="p-2.5 text-right">Estado</th>
                              </tr>
                            </thead>
                            <tbody class="divide-y divide-slate-100">
                              @for (r of driverAssignedRoutes(); track r.id) {
                                <tr class="hover:bg-slate-50 cursor-pointer"
                                  [class.bg-primary-50]="r.isCurrent"
                                  (click)="selectPlan(r.id)">
                                  <td class="p-2.5 font-mono font-bold text-slate-900 flex items-center gap-1.5">
                                    <span>{{ r.code }}</span>
                                    @if (r.isCurrent) {
                                      <span class="px-1.5 py-0.2 rounded text-[9px] bg-primary-600 text-white font-semibold">Actual</span>
                                    }
                                  </td>
                                  <td class="p-2.5 font-mono text-slate-700">{{ r.vehicleCode }}</td>
                                  <td class="p-2.5 text-slate-600 font-mono text-[11px]">{{ r.plannedStartUtc | date:'short' }}</td>
                                  <td class="p-2.5 text-right font-mono">{{ r.totalDistanceKm | number:'1.0-1' }} km</td>
                                  <td class="p-2.5 text-right font-mono">{{ r.totalDurationMinutes | number:'1.0-0' }} min</td>
                                  <td class="p-2.5 text-center font-mono">{{ r.stopsCount }}</td>
                                  <td class="p-2.5 text-right">
                                    <span class="px-2 py-0.5 rounded text-[10px] font-semibold"
                                      [ngClass]="r.status === 'Published' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'">
                                      {{ r.status }}
                                    </span>
                                  </td>
                                </tr>
                              }
                            </tbody>
                          </table>
                        </div>
                      } @else {
                        <div class="p-4 bg-white border border-slate-200 rounded-lg text-xs text-slate-500 text-center">
                          @if (!selectedDetail()!.driverName) {
                            <span>Asigna un conductor a la ruta para consultar su itinerario y otras rutas asignadas.</span>
                          } @else {
                            <span>No se registran otras rutas asignadas a este conductor en la jornada.</span>
                          }
                        </div>
                      }
                    </div>

                    <!-- Cuadro global de conductores de la flota -->
                    <div class="space-y-2 pt-2">
                      <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wide">
                        Distribución de Rutas por Conductor en la Flota
                      </h4>
                      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                        @for (dw of allDriversWorkload(); track dw.driverName) {
                          <div class="p-3 bg-white border border-slate-200 rounded-xl space-y-2">
                            <div class="flex items-center justify-between">
                              <strong class="text-xs text-slate-900">{{ dw.driverName }}</strong>
                              <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-slate-100 text-slate-700 font-mono">
                                {{ dw.routesCount }} {{ dw.routesCount === 1 ? 'ruta' : 'rutas' }}
                              </span>
                            </div>
                            <div class="flex items-center justify-between text-[11px] text-slate-500 font-mono">
                              <span>{{ dw.totalStops }} paradas</span>
                              <span>{{ dw.totalKm | number:'1.0-1' }} km</span>
                            </div>
                            <div class="flex flex-wrap gap-1 pt-1 border-t border-slate-100">
                              @for (r of dw.routes; track r.id) {
                                <button
                                  type="button"
                                  (click)="selectPlan(r.id)"
                                  class="text-[10px] font-mono px-1.5 py-0.5 rounded hover:underline"
                                  [ngClass]="r.id === selectedDetail()!.id ? 'bg-primary-600 text-white' : 'bg-slate-100 text-slate-700'">
                                  {{ r.code }}
                                </button>
                              }
                            </div>
                          </div>
                        }
                      </div>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }
      } <!-- Fin pestaña rutas -->

      <!-- ======================================================== -->
      <!-- BANDEJA DE CAPTURAS RECHAZADAS POR PUNTO (ESC-23 / ESC-25) -->
      <!-- ======================================================== -->
      @if (activeTab() === 'rechazos') {
        <div class="space-y-6">
          <!-- Filtros y Resumen -->
          <div class="bg-white border border-slate-200 rounded-xl p-4 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div class="flex flex-col sm:flex-row sm:items-center gap-3">
              <label class="text-xs font-bold text-slate-700">Filtrar por Punto de Servicio:</label>
              <select
                [ngModel]="selectedServicePointFilter()"
                (ngModelChange)="onServicePointFilterChange($event)"
                class="text-xs p-2 border border-slate-300 rounded-lg bg-white min-w-[220px]">
                <option value="">Todos los puntos (General)</option>
                @for (pt of availableLocations(); track pt.id) {
                  <option [value]="pt.id">{{ pt.name }} ({{ pt.code }})</option>
                }
              </select>
            </div>

            <div class="flex items-center gap-3">
              <button
                type="button"
                (click)="loadRejectedCaptures()"
                [disabled]="rejectedLoading()"
                class="inline-flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg bg-slate-100 text-slate-700 hover:bg-slate-200 transition-colors disabled:opacity-50">
                @if (rejectedLoading()) {
                  <span class="inline-block animate-spin h-3 w-3 border-2 border-slate-600 border-t-transparent rounded-full"></span>
                }
                <span>↻ Actualizar</span>
              </button>
            </div>
          </div>

          <!-- Métricas Rápidas -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div class="bg-white border border-slate-200 rounded-xl p-4">
              <span class="text-xs font-semibold text-slate-500 uppercase">Total Capturas Aisladas</span>
              <p class="text-2xl font-bold text-rose-600 mt-1 font-mono">{{ rejectedCaptures().length }}</p>
              <p class="text-[11px] text-slate-400 mt-0.5">Capturas en «Requiere Revisión»</p>
            </div>
            <div class="bg-white border border-slate-200 rounded-xl p-4">
              <span class="text-xs font-semibold text-slate-500 uppercase">Puntos con Incidencias</span>
              <p class="text-2xl font-bold text-amber-600 mt-1 font-mono">{{ uniqueRejectedPointsCount() }}</p>
              <p class="text-[11px] text-slate-400 mt-0.5">Puntos de entrega afectados</p>
            </div>
            <div class="bg-white border border-slate-200 rounded-xl p-4">
              <span class="text-xs font-semibold text-slate-500 uppercase">Unidades Rechazadas</span>
              <p class="text-2xl font-bold text-slate-800 mt-1 font-mono">{{ totalRejectedUnitsCount() }}</p>
              <p class="text-[11px] text-slate-400 mt-0.5">Discrepancias o daños de stock</p>
            </div>
          </div>

          <!-- Estado Cargando -->
          @if (rejectedLoading()) {
            <div class="p-8 text-center text-slate-500 text-sm bg-white border border-slate-200 rounded-xl">
              Consultando capturas rechazadas por punto...
            </div>
          }

          <!-- Estado Vacío -->
          @if (!rejectedLoading() && rejectedCaptures().length === 0) {
            <div class="p-8 text-center bg-white border border-slate-200 rounded-xl space-y-2">
              <div class="text-3xl text-emerald-600">✓</div>
              <p class="text-slate-800 font-bold text-sm">Sin capturas rechazadas pendientes</p>
              <p class="text-slate-500 text-xs">
                No hay elementos en outbox con estado «Requiere Revisión» para los filtros seleccionados.
                Todas las entregas y sincronizaciones de campo se encuentran operativas.
              </p>
            </div>
          }

          <!-- Lista de Capturas Rechazadas -->
          @if (!rejectedLoading() && rejectedCaptures().length > 0) {
            <div class="space-y-4">
              @for (capture of rejectedCaptures(); track capture.receiptId) {
                <div class="bg-white border border-rose-200 rounded-xl p-5 shadow-xs space-y-4">
                  <!-- Cabecera de la captura -->
                  <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-100 pb-3">
                    <div>
                      <div class="flex items-center gap-2">
                        <span class="font-bold text-slate-900 text-sm">{{ capture.servicePointName }}</span>
                        <span class="font-mono text-xs px-2 py-0.5 rounded bg-slate-100 text-slate-600 font-semibold">
                          {{ capture.servicePointCode }}
                        </span>
                      </div>
                      <p class="text-xs text-slate-500 mt-0.5 font-mono">
                        Entrega: <strong class="text-slate-800">{{ capture.deliveryCode }}</strong> · 
                        Registrado por: <strong class="text-slate-800">{{ capture.registeredByName || capture.driverName }}</strong> ·
                        Fecha: {{ capture.receivedAtUtc | date:'medium' }}
                      </p>
                    </div>

                    <div class="flex items-center gap-2">
                      <span class="text-xs px-2.5 py-1 rounded-full font-bold bg-rose-50 text-rose-700 border border-rose-200">
                        ⚠ Requiere Revisión
                      </span>
                    </div>
                  </div>

                  <!-- Motivo de rechazo de negocio -->
                  <div class="bg-rose-50/70 border border-rose-200 rounded-lg p-3 text-xs space-y-1">
                    <div class="flex items-center gap-1.5 font-bold text-rose-900">
                      <span>Causa de Aislamiento:</span>
                      <span class="font-mono text-[11px] bg-rose-100 px-1.5 py-0.5 rounded">{{ capture.reason }}</span>
                    </div>
                    <p class="text-rose-700 text-[11px]">
                      Aislada del flujo normal (ESC-23 / ESC-25) para evitar reintentos continuos y permitir que el resto de la cola continúe sincronizando.
                    </p>
                  </div>

                  <!-- Detalle de líneas -->
                  <div>
                    <h5 class="text-[11px] font-bold text-slate-600 uppercase mb-2">Detalle de Productos y Líneas Rechazadas</h5>
                    <div class="border border-slate-200 rounded-lg overflow-x-auto">
                      <table class="w-full text-left text-xs min-w-[500px]">
                        <thead class="bg-slate-50 text-[11px] text-slate-500 uppercase border-b border-slate-200">
                          <tr>
                            <th class="p-2.5">Producto / SKU</th>
                            <th class="p-2.5 text-center">Cant. Aceptada</th>
                            <th class="p-2.5 text-center">Cant. Rechazada</th>
                            <th class="p-2.5">Motivo Específico</th>
                          </tr>
                        </thead>
                        <tbody class="divide-y divide-slate-100">
                          @for (line of capture.lines; track line.productId) {
                            <tr>
                              <td class="p-2.5 font-medium text-slate-800">
                                {{ line.productName }}
                                <span class="block text-[10px] text-slate-400 font-mono">{{ line.sku }}</span>
                              </td>
                              <td class="p-2.5 text-center font-mono font-semibold text-emerald-700">
                                {{ line.quantityAccepted }} u
                              </td>
                              <td class="p-2.5 text-center font-mono font-bold text-rose-600">
                                {{ line.quantityRejected }} u
                              </td>
                              <td class="p-2.5 text-slate-600 text-[11px]">
                                {{ line.rejectionReason || 'Discrepancia en recepción física' }}
                              </td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>

    <!-- ======================================================== -->
    <!-- MODAL ASISTENTE DE RUTAS POR PEDIDOS (4 PASOS) -->
    <!-- ======================================================== -->
    @if (isWizardOpen()) {
      <div class="fixed inset-0 z-50 bg-slate-900/70 backdrop-blur-sm flex items-center justify-center p-2 sm:p-4">
        <div class="bg-white w-full max-w-7xl h-[94vh] rounded-2xl shadow-2xl flex flex-col overflow-hidden border border-slate-200">
          
          <!-- Header Asistente -->
          <div class="px-6 py-4 bg-slate-900 text-white flex items-center justify-between">
            <div>
              <div class="flex items-center gap-2">
                <h2 class="text-base font-bold">Planificador de Rutas por Pedidos y Abastecimiento</h2>
                <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                  Paso {{ wizardStep() }} de 4
                </span>
              </div>
              <p class="text-xs text-slate-300 mt-0.5">
                @if (wizardStep() === 1) { Selecciona los pedidos de entrega a incluir o crea un nuevo pedido con sus productos. }
                @if (wizardStep() === 2) { Revisa el abastecimiento automático: origen de bodegas más cercanas y distribución de stock. }
                @if (wizardStep() === 3) { Evalúa las opciones de viaje (A: Consolidado vs B: Escalonado) y valida el orden de paradas. }
                @if (wizardStep() === 4) { Asigna el vehículo idóneo según capacidad de peso, volumen y depósito base. }
              </p>
            </div>

            <button type="button" (click)="closeWizard()" class="text-xs font-semibold px-2 py-1 rounded bg-slate-800 text-slate-300 hover:text-white hover:bg-slate-700 transition-colors">Cerrar</button>
          </div>

          <!-- Cuerpo Asistente -->
          <div class="flex-1 grid grid-cols-1 lg:grid-cols-12 overflow-hidden">
            
            <!-- Columna Izquierda: Formulario y Pasos (5 Cols) -->
            <div class="lg:col-span-5 p-4 border-r border-slate-200 flex flex-col gap-3 bg-slate-50/60 overflow-y-auto">
              
              <!-- PASO 1: SELECCIONAR O CREAR PEDIDOS -->
              @if (wizardStep() === 1) {
                <div class="space-y-3">
                  <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-700 uppercase">Pedidos Pendientes de Despacho</span>
                    <button
                      type="button"
                      (click)="showNewOrderForm.set(!showNewOrderForm())"
                      class="text-[11px] font-semibold text-primary-600 hover:underline">
                      {{ showNewOrderForm() ? 'Cancelar nuevo' : '+ Crear Pedido Manual' }}
                    </button>
                  </div>

                  <!-- Formulario Nuevo Pedido Rápido -->
                  @if (showNewOrderForm()) {
                    <div class="p-3 bg-white rounded-xl border border-primary-200 shadow-sm space-y-2 text-xs">
                      <span class="font-bold text-slate-800 block text-[11px] uppercase">Nuevo Pedido</span>
                      
                      <div>
                        <label class="text-[10px] text-slate-500 block">Destino (Punto de Entrega o Bodega)</label>
                        <select [(ngModel)]="newOrderDestId" class="w-full text-xs p-1.5 border rounded">
                          <option value="">Selecciona destino...</option>
                          @for (loc of availableLocations(); track loc.id) {
                            <option [value]="loc.id">{{ loc.name }} ({{ loc.code }})</option>
                          }
                        </select>
                      </div>

                      <div class="grid grid-cols-2 gap-2">
                        <div>
                          <label class="text-[10px] text-slate-500 block">Producto</label>
                          <select [(ngModel)]="newOrderProdId" class="w-full text-xs p-1.5 border rounded">
                            <option value="">Selecciona...</option>
                            @for (p of availableProducts(); track p.id) {
                              <option [value]="p.id">{{ p.name }} ({{ p.sku }})</option>
                            }
                          </select>
                        </div>
                        <div>
                          <label class="text-[10px] text-slate-500 block">Cantidad Requerida</label>
                          <input type="number" [(ngModel)]="newOrderQty" min="1" class="w-full text-xs p-1.5 border rounded font-mono" />
                        </div>
                      </div>

                      <button
                        type="button"
                        (click)="submitNewOrder()"
                        [disabled]="!newOrderDestId || !newOrderProdId || newOrderQty <= 0"
                        class="w-full py-1.5 rounded bg-primary-600 text-white font-bold hover:bg-primary-700 disabled:opacity-50">
                        Guardar Pedido
                      </button>
                    </div>
                  }

                  <!-- Lista de Pedidos Disponibles con Checkbox -->
                  <div class="space-y-2">
                    @for (ord of pendingOrders(); track ord.id) {
                      <div class="p-3 rounded-xl border bg-white shadow-xs flex items-start gap-2 text-xs cursor-pointer"
                        [ngClass]="selectedOrderIds().has(ord.id) ? 'border-primary-600 ring-2 ring-primary-100' : 'border-slate-200'"
                        (click)="toggleOrderSelection(ord.id)">
                        <input
                          type="checkbox"
                          [checked]="selectedOrderIds().has(ord.id)"
                          (click)="$event.stopPropagation()"
                          (change)="toggleOrderSelection(ord.id)"
                          class="mt-0.5 rounded text-primary-600" />
                        <div class="flex-1 min-w-0">
                          <div class="flex items-center justify-between">
                            <span class="font-bold text-slate-900">{{ ord.orderNumber }}</span>
                            <span class="text-[9px] font-mono px-1.5 py-0.5 rounded uppercase font-bold"
                              [ngClass]="ord.priority === 'Critical' ? 'bg-rose-100 text-rose-800' : 'bg-slate-100 text-slate-700'">
                              {{ ord.priority }}
                            </span>
                          </div>
                          <p class="text-[11px] font-semibold text-primary-700 mt-0.5">Destino: {{ ord.destinationName }}</p>
                          <div class="mt-1 space-y-0.5 text-[10px] text-slate-500 font-mono">
                            @for (l of ord.lines; track l.id) {
                              <div>• {{ l.productName }}: <strong>{{ l.requestedQuantity }} u</strong></div>
                            }
                          </div>
                        </div>
                      </div>
                    }
                  </div>
                </div>
              }

              <!-- PASO 2: REVISIÓN DE ABASTECIMIENTO -->
              @if (wizardStep() === 2) {
                <div class="space-y-3">
                  <span class="text-xs font-bold text-slate-700 uppercase">Asignación de Bodegas por Cercanía</span>
                  <p class="text-xs text-slate-500">
                    El sistema verificó el stock FEFO disponible y asignó automáticamente la bodega más próxima:
                  </p>

                  @for (opt of routeOptions(); track opt.optionId) {
                    <div class="p-3 rounded-xl border bg-white space-y-2 text-xs">
                      <span class="font-bold text-slate-800 block text-xs">{{ opt.title }}</span>
                      <p class="text-[11px] text-slate-500">{{ opt.description }}</p>

                      <div class="border border-slate-100 rounded-lg p-2 bg-slate-50 space-y-1">
                        @for (l of opt.sourcingLines; track l.orderId) {
                          <div class="border-b border-slate-200/60 pb-1 last:border-0 last:pb-0">
                            <div class="font-semibold text-slate-800">{{ l.productName }} ({{ l.requestedQuantity }} u)</div>
                            <div class="text-[10px] text-slate-500">Para: {{ l.destinationName }}</div>
                            <div class="mt-1 text-[10px] text-emerald-700 font-mono">
                              @for (a of l.allocations; track a.warehouseId) {
                                <div>Origen {{ a.warehouseCode }}: {{ a.quantity }} u</div>
                              }
                            </div>
                          </div>
                        }
                      </div>
                    </div>
                  }
                </div>
              }

              <!-- PASO 3: OPCIONES A/B & REORDENAR PARADAS -->
              @if (wizardStep() === 3) {
                <div class="space-y-3">
                  <span class="text-xs font-bold text-slate-700 uppercase">Opciones de Ruta & Secuencia de Paradas</span>

                  <div class="grid grid-cols-2 gap-2">
                    @for (opt of routeOptions(); track opt.optionId) {
                      <div
                        (click)="selectedOptionId.set(opt.optionId); onOptionSelected(opt)"
                        class="p-2.5 rounded-xl border cursor-pointer text-xs transition-all"
                        [ngClass]="selectedOptionId() === opt.optionId ? 'border-primary-600 ring-2 ring-primary-100 bg-white' : 'border-slate-200 bg-white/70'">
                        <div class="flex items-center justify-between">
                          <span class="font-bold text-slate-800">Opción {{ opt.optionId }}</span>
                          @if (opt.isRecommended) {
                            <span class="text-[9px] bg-emerald-100 text-emerald-800 px-1.5 py-0.5 rounded font-bold">RECOMENDADO</span>
                          }
                        </div>
                        <p class="text-[10px] text-slate-500 mt-1 line-clamp-2">{{ opt.title }}</p>
                        <div class="text-[10px] font-mono font-bold mt-2 text-slate-700">
                          {{ opt.evaluation.totalDistanceKm }} km | {{ opt.evaluation.totalDurationMinutes }} min
                        </div>
                      </div>
                    }
                  </div>

                  <!-- Lista interactiva de paradas con reordenamiento -->
                  <div class="bg-white p-3 rounded-xl border border-slate-200 space-y-2">
                    <div class="flex items-center justify-between">
                      <span class="text-[11px] font-bold text-slate-700 uppercase">Secuencia Reordenable ({{ editableStops().length }} paradas)</span>
                      <button (click)="restoreOptimalOrder()" class="text-[10px] text-primary-600 hover:underline font-semibold">
                        Restaurar orden óptimo
                      </button>
                    </div>

                    <div class="space-y-1.5 max-h-[220px] overflow-y-auto pr-1">
                      @for (s of editableStops(); track s.sequence; let idx = $index) {
                        <div class="p-2 rounded-lg border flex items-center justify-between text-xs"
                          [ngClass]="s.kind === 'Pickup' ? 'bg-amber-50/70 border-amber-200' : 'bg-slate-50 border-slate-200'">
                          <div class="flex items-center gap-2 min-w-0">
                            <span class="w-5 h-5 rounded-full flex items-center justify-center font-bold text-[10px] text-white"
                              [ngClass]="s.kind === 'Pickup' ? 'bg-amber-600' : 'bg-primary-600'">
                              {{ idx + 1 }}
                            </span>
                            <div class="min-w-0">
                              <p class="font-bold text-slate-800 truncate">{{ s.name }}</p>
                              <p class="text-[9px] font-mono text-slate-500">
                                {{ s.kind === 'Pickup' ? 'Recoger en bodega' : 'Entrega' }}
                              </p>
                            </div>
                          </div>

                          <div class="flex items-center gap-1">
                            @if (idx > 0) {
                              <button (click)="moveStop(idx, -1)" class="p-1 hover:bg-slate-200 rounded font-semibold text-slate-600 text-[10px]" title="Subir parada">▲</button>
                            }
                            @if (idx < editableStops().length - 1) {
                              <button (click)="moveStop(idx, 1)" class="p-1 hover:bg-slate-200 rounded font-semibold text-slate-600 text-[10px]" title="Bajar parada">▼</button>
                            }
                          </div>
                        </div>
                      }
                    </div>

                    @if (currentEvaluation()?.validationErrors?.length) {
                      <div class="p-2 bg-rose-50 border border-rose-200 rounded text-[11px] text-rose-700">
                        @for (err of currentEvaluation()!.validationErrors; track err) {
                          <div>[Aviso] {{ err }}</div>
                        }
                      </div>
                    }
                  </div>
                </div>
              }

              <!-- PASO 4: SELECCIÓN DE VEHÍCULO & CONFIRMACIÓN -->
              @if (wizardStep() === 4) {
                <div class="space-y-3">
                  <span class="text-xs font-bold text-slate-700 uppercase">Vehículos Compatibles con la Carga</span>
                  <p class="text-xs text-slate-500">
                    El sistema calculó el perfil de carga tramo a tramo para descartar vehículos que no soportan el peso o volumen:
                  </p>

                  <div class="space-y-2">
                    @for (vm of availableVehicleMatches(); track vm.vehicleId) {
                      <div
                        (click)="vm.isMatch ? selectedVehicleId.set(vm.vehicleId) : null"
                        class="p-3 rounded-xl border text-xs transition-all"
                        [ngClass]="{
                          'border-primary-600 ring-2 ring-primary-100 bg-white cursor-pointer': vm.isMatch && selectedVehicleId() === vm.vehicleId,
                          'border-slate-200 bg-white hover:border-slate-300 cursor-pointer': vm.isMatch && selectedVehicleId() !== vm.vehicleId,
                          'border-slate-200 bg-slate-100 opacity-60 cursor-not-allowed': !vm.isMatch
                        }">
                        <div class="flex items-center justify-between">
                          <span class="font-bold text-slate-900 text-sm">{{ vm.vehicleCode }}</span>
                          @if (vm.isMatch) {
                            <span class="text-[9px] bg-emerald-100 text-emerald-800 font-bold px-2 py-0.5 rounded">COMPATIBLE</span>
                          } @else {
                            <span class="text-[9px] bg-rose-100 text-rose-800 font-bold px-2 py-0.5 rounded">NO COMPATIBLE</span>
                          }
                        </div>

                        <div class="mt-2 grid grid-cols-2 gap-2 text-[11px] font-mono text-slate-600">
                          <div>Capacidad: <strong>{{ vm.maxWeight }} kg</strong></div>
                          <div>Uso estimado: <strong>{{ vm.weightUtilizationPct }}%</strong></div>
                          <div>Depósito base: <strong>{{ vm.distanceToFirstPickupKm }} km</strong></div>
                          <div>Costo estimado: <strong>USD {{ vm.estimatedCost }}</strong></div>
                        </div>

                        @if (!vm.isMatch) {
                          <p class="text-[10px] text-rose-600 mt-2 font-semibold">
                            Motivo descarte: {{ vm.discardReason }}
                          </p>
                        }
                      </div>
                    }
                  </div>
                </div>
              }

              <!-- Botones de Navegación del Asistente -->
              <div class="mt-auto pt-3 border-t border-slate-200 flex items-center justify-between gap-2">
                @if (wizardStep() > 1) {
                  <button
                    type="button"
                    (click)="stepBack()"
                    class="px-4 py-2 text-xs font-semibold rounded-lg bg-slate-200 text-slate-700 hover:bg-slate-300">
                    ← Anterior
                  </button>
                } @else {
                  <div></div>
                }

                @if (wizardStep() < 4) {
                  <button
                    type="button"
                    (click)="advanceWizardStep()"
                    [disabled]="selectedOrderIds().size === 0 || actionLoading()"
                    class="px-4 py-2 text-xs font-semibold rounded-lg bg-primary-600 text-white hover:bg-primary-700 disabled:opacity-50">
                    Siguiente →
                  </button>
                } @else {
                  <button
                    type="button"
                    (click)="confirmAndSavePlan()"
                    [disabled]="!selectedVehicleId() || actionLoading()"
                    class="px-4 py-2 text-xs font-bold rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm disabled:opacity-50">
                    {{ actionLoading() ? 'Creando Ruta...' : 'Confirmar y Crear Ruta' }}
                  </button>
                }
              </div>
            </div>

            <!-- Columna Derecha: Mapa (7 Cols) -->
            <div class="lg:col-span-7 relative h-full min-h-[450px]">
              <ui-map-view
                #wizardMapView
                [markers]="wizardMarkers()"
                [polylines]="wizardPolylines()"
                [zoom]="8">
              </ui-map-view>
            </div>
            @if (wizardApproximate()) {
              <p class="text-[11px] text-amber-700 mt-1">
                Trazado aproximado (rectas entre paradas): el servicio de rutas por calles no respondió o no está configurado.
              </p>
            }
          </div>
        </div>
      </div>
    }
  `
})
export class RutasPage implements OnInit, OnDestroy {
  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);
  readonly isAuditor = computed(() => {
    const roleFn = (this.session as { role?: () => string | null }).role;
    if (typeof roleFn === 'function') {
      return roleFn() === 'Auditor';
    }
    return !this.session.canWrite();
  });

  @ViewChild('wizardMapView') wizardMapView?: MapViewComponent;

  readonly routePlans = signal<RoutePlanSummaryDto[]>([]);
  readonly selectedPlanId = signal<string | null>(null);
  readonly selectedDetail = signal<RoutePlanDetailDto | null>(null);
  readonly loading = signal(true);
  readonly actionLoading = signal(false);
  readonly agentLoading = signal(false);
  readonly agentRun = signal<AgentRunViewDto | null>(null);
  readonly feedbackMessage = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  readonly mapMarkers = signal<MapMarker[]>([]);
  readonly mapPolylines = signal<MapPolyline[]>([]);
  readonly routeLegend = signal<RouteLegendItem[]>([]);
  readonly drivers = signal<DriverOptionDto[]>([]);
  readonly selectedDriverId = signal('');
  readonly reassignDriverId = signal('');
  readonly routeLegendHasApproximate = computed(() => this.routeLegend().some(i => i.source === 'approximate'));
  private readonly geometries = new Map<string, RouteGeometryDto>();
  // Las rutas que salieron en rectas (OSRM público saturado) se vuelven a pedir unas veces, cada vez más espaciadas.
  private geometryRetries = 0;
  private geometryRetryTimer: ReturnType<typeof setTimeout> | undefined;
  static readonly GEOMETRY_RETRY_DELAYS_MS = [4000, 10000, 20000];

  // #164: Detalle de planificación por parada, producto y conductor
  readonly detailTab = signal<'paradas' | 'productos' | 'conductor'>('paradas');
  readonly expandedStopSeqs = signal<Set<number>>(new Set());

  toggleStopExpansion(seq: number): void {
    const current = new Set(this.expandedStopSeqs());
    if (current.has(seq)) current.delete(seq);
    else current.add(seq);
    this.expandedStopSeqs.set(current);
  }

  expandAllStops(): void {
    const seqs = this.selectedDetail()?.stops.map(s => s.sequence) ?? [];
    this.expandedStopSeqs.set(new Set(seqs));
  }

  collapseAllStops(): void {
    this.expandedStopSeqs.set(new Set());
  }

  readonly aggregatedProducts = computed(() => {
    const detail = this.selectedDetail();
    if (!detail) return [];
    if (detail.productsSummary && detail.productsSummary.length > 0) {
      return detail.productsSummary;
    }
    const map = new Map<string, RouteProductSummaryDto>();
    for (const stop of detail.stops) {
      for (const line of stop.lines ?? []) {
        if (!line.sku) continue;
        const existing = map.get(line.sku);
        if (!existing) {
          map.set(line.sku, {
            sku: line.sku,
            productName: line.productName || line.sku,
            totalPlannedQuantity: Number(line.plannedQuantity) || 0,
            baseUnit: line.baseUnit || 'U',
            stopsCount: 1,
            lots: line.lotCode ? [{ lotCode: line.lotCode, quantity: Number(line.plannedQuantity) || 0 }] : [],
            deliveryStatuses: line.deliveryStatus ? [line.deliveryStatus] : []
          });
        } else {
          existing.totalPlannedQuantity += Number(line.plannedQuantity) || 0;
          if (line.lotCode) {
            const lot = existing.lots.find(l => l.lotCode === line.lotCode);
            if (lot) lot.quantity += Number(line.plannedQuantity) || 0;
            else existing.lots.push({ lotCode: line.lotCode, quantity: Number(line.plannedQuantity) || 0 });
          }
          if (line.deliveryStatus && !existing.deliveryStatuses.includes(line.deliveryStatus)) {
            existing.deliveryStatuses.push(line.deliveryStatus);
          }
        }
      }
    }
    for (const item of map.values()) {
      item.stopsCount = detail.stops.filter(s => (s.lines ?? []).some(l => l.sku === item.sku)).length;
    }
    return [...map.values()].sort((a, b) => a.sku.localeCompare(b.sku));
  });

  readonly totalPlannedUnits = computed(() => {
    return this.aggregatedProducts().reduce((sum, p) => sum + p.totalPlannedQuantity, 0);
  });

  readonly driverAssignedRoutes = computed(() => {
    const detail = this.selectedDetail();
    if (!detail) return [];
    if (detail.driverRoutes && detail.driverRoutes.length > 0) {
      return detail.driverRoutes;
    }
    if (!detail.driverName) return [];
    return this.routePlans()
      .filter(p => p.driverName && p.driverName.includes(detail.driverName!))
      .map(p => ({
        id: p.id,
        routePlanId: p.id,
        code: p.code,
        revision: p.revision,
        status: p.status,
        vehicleCode: p.vehicleCode || '—',
        plannedStartUtc: p.plannedStartUtc,
        totalDistanceKm: p.totalDistanceKm,
        totalDurationMinutes: p.totalDurationMinutes,
        stopsCount: p.stopsCount,
        isCurrent: p.id === detail.id
      }));
  });

  readonly allDriversWorkload = computed(() => {
    const plans = this.routePlans();
    const map = new Map<string, { driverName: string; routesCount: number; totalKm: number; totalStops: number; routes: RoutePlanSummaryDto[] }>();
    for (const p of plans) {
      const name = p.driverName || 'Sin conductor asignado';
      const existing = map.get(name) ?? { driverName: name, routesCount: 0, totalKm: 0, totalStops: 0, routes: [] };
      existing.routesCount++;
      existing.totalKm += p.totalDistanceKm || 0;
      existing.totalStops += p.stopsCount || 0;
      existing.routes.push(p);
      map.set(name, existing);
    }
    return [...map.values()].sort((a, b) => a.driverName.localeCompare(b.driverName));
  });

  // Estado del Asistente de Rutas por Pedidos
  readonly isWizardOpen = signal(false);
  readonly wizardStep = signal<1 | 2 | 3 | 4>(1);
  readonly pendingOrders = signal<DeliveryOrderDto[]>([]);
  readonly selectedOrderIds = signal<Set<string>>(new Set());

  // Formulario rápido nuevo pedido
  readonly showNewOrderForm = signal(false);
  newOrderDestId = '';
  newOrderProdId = '';
  newOrderQty = 10;
  readonly availableLocations = signal<ServicePoint[]>([]);
  readonly availableProducts = signal<any[]>([]);

  // Opciones de Sourcing y Vehículos
  readonly routeOptions = signal<RouteSuggestionOptionDto[]>([]);
  readonly selectedOptionId = signal<string>('A');
  readonly editableStops = signal<RouteCandidateStop[]>([]);
  readonly currentEvaluation = signal<any>(null);
  readonly availableVehicleMatches = signal<any[]>([]);
  readonly selectedVehicleId = signal<string | null>(null);

  readonly wizardMarkers = signal<MapMarker[]>([]);
  readonly wizardPolylines = signal<MapPolyline[]>([]);
  readonly wizardApproximate = signal(false);

  // Pestaña Activa: Rutas o Bandeja de Rechazos
  readonly activeTab = signal<'rutas' | 'rechazos'>('rutas');

  // Estado de la Bandeja de Capturas Rechazadas
  readonly rejectedCaptures = signal<RejectedCaptureDto[]>([]);
  readonly rejectedLoading = signal(false);
  readonly selectedServicePointFilter = signal<string>('');
  readonly uniqueRejectedPointsCount = computed(() => new Set(this.rejectedCaptures().map(c => c.servicePointId)).size);
  readonly totalRejectedUnitsCount = computed(() =>
    this.rejectedCaptures().reduce(
      (acc: number, c) => acc + c.lines.reduce((lacc: number, l) => lacc + (l.quantityRejected || 0), 0),
      0
    )
  );

  ngOnInit(): void {
    if (this.session.canWrite()) this.apiService.getDrivers().subscribe(list => this.drivers.set(list));
    this.loadPlans();
    this.loadPendingOrders();
    this.loadCatalogData();
    this.loadRejectedCaptures();
  }

  loadRejectedCaptures(): void {
    this.rejectedLoading.set(true);
    const filterPoint = this.selectedServicePointFilter() || undefined;
    this.apiService.getRejectedCaptures(filterPoint).subscribe({
      next: (res) => {
        this.rejectedLoading.set(false);
        this.rejectedCaptures.set(res?.items || []);
      },
      error: () => {
        this.rejectedLoading.set(false);
        this.rejectedCaptures.set([]);
      }
    });
  }

  onServicePointFilterChange(pointId: string): void {
    this.selectedServicePointFilter.set(pointId);
    this.loadRejectedCaptures();
  }

  loadPlans(): void {
    this.loading.set(true);
    this.apiService.getRoutePlans().subscribe({
      next: (plans) => {
        this.loading.set(false);
        this.routePlans.set(plans || []);
        this.loadGeometries(plans || []);
        if (plans && plans.length > 0) {
          if (!this.selectedPlanId() || !plans.some(p => p.id === this.selectedPlanId())) {
            this.selectPlan(plans[0].id);
          }
        } else {
          this.selectedPlanId.set(null);
          this.selectedDetail.set(null);
        }
      },
      error: () => this.loading.set(false)
    });
  }

  selectPlan(id: string): void {
    this.selectedPlanId.set(id);
    this.apiService.getRoutePlanById(id).subscribe({
      next: (detail) => {
        this.selectedDetail.set(detail);
        if (detail?.stops) {
          const seqs = detail.stops.map(s => s.sequence);
          this.expandedStopSeqs.set(new Set(seqs));
        }
        if (this.geometries.size > 0) this.renderAllRoutes();
        else this.buildMap(detail);
      },
      error: () => {}
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

  loadPendingOrders(): void {
    this.apiService.getDeliveryOrders('Pending').subscribe({
      next: (orders) => {
        this.pendingOrders.set(orders || []);
        // Seleccionar automáticamente los primeros para comodidad
        if (orders && orders.length > 0 && this.selectedOrderIds().size === 0) {
          this.selectedOrderIds.set(new Set([orders[0].id]));
        }
      },
      error: () => {}
    });
  }

  loadCatalogData(): void {
    this.apiService.getServicePoints().subscribe({
      next: (sps) => {
        this.apiService.getWarehouses().subscribe({
          next: (whs) => this.availableLocations.set([...whs, ...sps]),
          error: () => this.availableLocations.set(sps || [])
        });
      },
      error: () => {}
    });

    this.apiService.getEntityData('products').subscribe({
      next: (prods) => this.availableProducts.set(prods || []),
      error: () => {}
    });
  }

  /** #160: reasignar el conductor de una ruta publicada (por ejemplo, las publicadas antes sin conductor). */
  reassignDriver(id: string): void {
    const accountId = this.reassignDriverId();
    if (!this.session.canWrite() || !accountId) return;
    this.actionLoading.set(true);
    this.apiService.assignRouteDriver(id, accountId).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        this.reassignDriverId.set('');
        this.feedbackMessage.set({ type: 'success', text: `Conductor asignado a ${res?.updatedDeliveries ?? 0} entregas.` });
        this.selectPlan(id);
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.feedbackMessage.set({ type: 'error', text: err?.error?.detail || 'No se pudo asignar el conductor.' });
      }
    });
  }

  publishPlan(id: string): void {
    if (!this.session.canWrite()) return;
    const vehicleCode = this.selectedDetail()?.vehicleCode;
    const driverAccountId = this.selectedDriverId();
    if (!vehicleCode || !driverAccountId) {
      this.feedbackMessage.set({ type: 'error', text: 'Elige el conductor antes de publicar: sin conductor, la ruta no aparece en su teléfono.' });
      return;
    }
    this.actionLoading.set(true);
    this.apiService.publishRoutePlan(id, { vehicleCode, driverAccountId }).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.feedbackMessage.set({
          type: 'success',
          text: 'Ruta publicada exitosamente al conductor.'
        });
        this.selectPlan(id);
        this.loadPlans();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.feedbackMessage.set({
          type: 'error',
          text: err?.error?.detail || err?.error?.message || 'Error al publicar la ruta.'
        });
      }
    });
  }

  // ========================================================
  // FLUJO DE ASISTENTE DE RUTAS POR PEDIDOS
  // ========================================================

  openOrderRoutingModal(): void {
    this.isWizardOpen.set(true);
    this.wizardStep.set(1);
    this.loadPendingOrders();
    setTimeout(() => this.wizardMapView?.invalidateSize(), 200);
  }

  closeWizard(): void {
    this.isWizardOpen.set(false);
  }

  stepBack(): void {
    const cur = this.wizardStep();
    if (cur === 4) this.wizardStep.set(3);
    else if (cur === 3) this.wizardStep.set(2);
    else if (cur === 2) this.wizardStep.set(1);
  }

  toggleOrderSelection(id: string): void {
    const set = new Set(this.selectedOrderIds());
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.selectedOrderIds.set(set);
  }

  submitNewOrder(): void {
    const isWh = this.availableLocations().find(l => l.id === this.newOrderDestId)?.type === 'Bodega';

    this.apiService.createDeliveryOrder({
      destinationServicePointId: !isWh ? this.newOrderDestId : undefined,
      destinationLocationId: isWh ? this.newOrderDestId : undefined,
      lines: [{ productId: this.newOrderProdId, quantity: this.newOrderQty }]
    }).subscribe({
      next: (res) => {
        this.showNewOrderForm.set(false);
        this.loadPendingOrders();
        if (res?.id) {
          const set = new Set(this.selectedOrderIds());
          set.add(res.id);
          this.selectedOrderIds.set(set);
        }
      }
    });
  }

  advanceWizardStep(): void {
    const current = this.wizardStep();
    if (current === 1) {
      this.actionLoading.set(true);
      const orderIds = Array.from(this.selectedOrderIds());

      this.apiService.suggestRoutes(orderIds).subscribe({
        next: (res) => {
          this.actionLoading.set(false);
          const opts = res?.options || [];
          this.routeOptions.set(opts);

          if (opts.length > 0) {
            this.selectedOptionId.set(opts[0].optionId);
            this.onOptionSelected(opts[0]);
          }

          this.wizardStep.set(2);
        },
        error: (err) => {
          this.actionLoading.set(false);
          this.feedbackMessage.set({
            type: 'error',
            text: err?.error?.message || 'Error al calcular opciones de abastecimiento.'
          });
        }
      });
    } else if (current === 2) {
      this.wizardStep.set(3);
    } else if (current === 3) {
      this.wizardStep.set(4);
    }
  }

  onOptionSelected(opt: RouteSuggestionOptionDto): void {
    this.editableStops.set([...opt.stops]);
    this.currentEvaluation.set(opt.evaluation);
    this.availableVehicleMatches.set(opt.vehicleMatches || []);

    const firstMatch = opt.vehicleMatches?.find(v => v.isMatch);
    if (firstMatch) {
      this.selectedVehicleId.set(firstMatch.vehicleId);
    }

    this.updateWizardMap(opt.stops, opt.evaluation.coordinates, opt.evaluation.coordinatesSource);
  }

  moveStop(index: number, direction: -1 | 1): void {
    const stops = [...this.editableStops()];
    const target = index + direction;
    if (target < 0 || target >= stops.length) return;

    const temp = stops[index];
    stops[index] = stops[target];
    stops[target] = temp;

    // Renumerar secuencia
    stops.forEach((s, i) => s.sequence = i + 1);
    this.editableStops.set(stops);

    // Reevaluar secuencia modificada por el usuario
    this.apiService.evaluateRouteSequence(stops, this.selectedVehicleId() || undefined).subscribe(evalRes => {
      this.currentEvaluation.set(evalRes);
      this.updateWizardMap(stops, evalRes.coordinates, evalRes.coordinatesSource);
    });
  }

  restoreOptimalOrder(): void {
    const opt = this.routeOptions().find(o => o.optionId === this.selectedOptionId());
    if (opt) {
      this.onOptionSelected(opt);
    }
  }

  confirmAndSavePlan(): void {
    this.actionLoading.set(true);
    const stops = this.editableStops();
    const evalData = this.currentEvaluation();

    this.apiService.confirmRoutePlan({
      vehicleId: this.selectedVehicleId() || undefined,
      totalDistanceKm: evalData?.totalDistanceKm || 0,
      totalDurationMinutes: evalData?.totalDurationMinutes || 0,
      totalCost: evalData?.totalCost || 0,
      orderIds: Array.from(this.selectedOrderIds()),
      stops,
      coordinates: evalData?.coordinates
    }).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        this.closeWizard();
        this.feedbackMessage.set({
          type: 'success',
          text: `¡Ruta guardada y asignada exitosamente con ${stops.length} paradas!`
        });
        if (res?.id) this.selectedPlanId.set(res.id);
        this.loadPlans();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.feedbackMessage.set({
          type: 'error',
          text: err?.error?.message || 'Error al confirmar la ruta.'
        });
      }
    });
  }

  // ========================================================
  // RUTA DE EMERGENCIA
  // ========================================================

  triggerEmergencyFlow(): void {
    this.actionLoading.set(true);
    this.feedbackMessage.set(null);

    this.apiService.getEmergencyRoutes().subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        const count = res?.orders?.length || 0;
        if (count === 0) {
          this.feedbackMessage.set({
            type: 'success',
            text: res?.message || 'No hay faltantes críticos que requieran ruta de emergencia.'
          });
          return;
        }

        this.feedbackMessage.set({
          type: 'success',
          text: res?.message || `Se generaron ${count} pedidos de emergencia listos para planificar.`
        });

        // Abrir asistente con los pedidos de emergencia cargados
        this.isWizardOpen.set(true);
        this.wizardStep.set(1);
        this.loadPendingOrders();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.feedbackMessage.set({
          type: 'error',
          text: err?.error?.message || 'Error al generar requerimientos de emergencia.'
        });
      }
    });
  }

  // ========================================================
  // AGENTE LOGÍSTICO IA (N8N + DEEPSEEK)
  // ========================================================

  triggerAgentWatch(): void {
    if (this.agentLoading() || this.actionLoading()) return;
    this.agentLoading.set(true);
    this.feedbackMessage.set(null);

    this.apiService.startAgentRun('Watch').subscribe({
      next: (run) => {
        this.agentLoading.set(false);
        this.agentRun.set(run);
        this.loadPlans();
        const mode = run.status === 'Completed' ? 'Agente' : run.status === 'Fallback' ? 'Respuesta determinista' : run.status;
        this.feedbackMessage.set({
          type: run.status === 'Failed' ? 'error' : 'success',
          text: `${mode}: ${run.latencyMs} ms; ${run.decisions.length} decisiones.`
        });
      },
      error: (err) => {
        this.agentLoading.set(false);
        this.feedbackMessage.set({
          type: 'error',
          text: err?.error?.message || 'Error al ejecutar el agente.'
        });
      }
    });
  }

  private updateWizardMap(stops: RouteCandidateStop[], coordinates?: [number, number][], source?: string): void {
    const markers: MapMarker[] = stops.map(s => ({
      id: s.servicePointId || s.locationId || s.code,
      latitude: s.position.lat,
      longitude: s.position.lon,
      title: `#${s.sequence}. ${s.name}`,
      badge: s.kind === 'Pickup' ? 'Bodega (Recogida)' : 'Entrega',
      badgeColor: s.kind === 'Pickup' ? 'amber' : 'blue'
    }));

    this.wizardMarkers.set(markers);

    if (coordinates && coordinates.length > 1) {
      const byRoad = source === 'osrm';
      this.wizardPolylines.set([{
        id: 'wizard-road-route',
        coordinates,
        color: ROUTE_COLORS[0],
        weight: 5,
        dashArray: byRoad ? undefined : '6 6',
        tooltip: byRoad ? 'Recorrido por calles' : 'Trazado aproximado (rectas entre paradas)'
      }]);
      this.wizardApproximate.set(!byRoad);
    } else {
      this.wizardPolylines.set([]);
      this.wizardApproximate.set(false);
    }
  }

  /** Pide al backend el recorrido por calles de cada ruta vigente que aún no tenga. */
  private loadGeometries(plans: RoutePlanSummaryDto[]): void {
    const missing = currentPlans(plans).filter(p => this.geometries.get(p.id)?.source !== 'osrm');
    if (missing.length === 0) {
      if (this.geometries.size > 0) this.renderAllRoutes();
      return;
    }
    forkJoin(missing.map(p => this.apiService.getRoutePlanGeometry(p.id))).subscribe(results => {
      results.forEach((geometry, i) => { if (geometry) this.geometries.set(missing[i].id, geometry); });
      if (this.geometries.size > 0) this.renderAllRoutes();
      this.scheduleGeometryRetry(plans);
    });
  }

  private scheduleGeometryRetry(plans: RoutePlanSummaryDto[]): void {
    clearTimeout(this.geometryRetryTimer);
    const pending = currentPlans(plans).some(p => this.geometries.get(p.id)?.source === 'approximate');
    if (!pending) { this.geometryRetries = 0; return; }
    const delay = RutasPage.GEOMETRY_RETRY_DELAYS_MS[this.geometryRetries];
    if (delay === undefined) return;
    this.geometryRetries++;
    this.geometryRetryTimer = setTimeout(() => this.loadGeometries(this.routePlans()), delay);
  }

  ngOnDestroy(): void {
    clearTimeout(this.geometryRetryTimer);
  }

  /** Todas las rutas vigentes a la vez: un color por vehículo, la seleccionada resaltada y las bodegas marcadas. */
  renderAllRoutes(): void {
    const selected = this.selectedPlanId();
    const polylines: MapPolyline[] = [];
    const markers: MapMarker[] = [];
    const legend: RouteLegendItem[] = [];
    const depots = new Map<string, MapMarker>();

    currentPlans(this.routePlans()).forEach((plan, index) => {
      const geometry = this.geometries.get(plan.id);
      if (!geometry) return;
      const color = ROUTE_COLORS[index % ROUTE_COLORS.length];
      const isSelected = plan.id === selected;
      const dimmed = !!selected && !isSelected;
      const vehicle = geometry.vehicleCode || plan.vehicleCode || plan.code;
      const km = geometry.roadKm ?? plan.totalDistanceKm;

      if (geometry.coordinates.length > 1) {
        polylines.push({
          id: plan.id,
          coordinates: geometry.coordinates,
          color,
          weight: isSelected ? 6 : 4,
          opacity: dimmed ? 0.3 : 0.9,
          dashArray: geometry.source === 'approximate' ? '6 6' : undefined,
          tooltip: `${vehicle} · ${km.toFixed(1)} km ${geometry.source === 'osrm' ? 'por calles' : 'aprox.'} · ${geometry.stops.length} paradas`
        });
      }
      if (geometry.depot) {
        depots.set(geometry.depot.code, {
          id: `depot-${geometry.depot.code}`,
          latitude: geometry.depot.latitude,
          longitude: geometry.depot.longitude,
          title: `Bodega ${geometry.depot.name || geometry.depot.code}`,
          badge: 'Salida y regreso',
          badgeColor: 'amber'
        });
      }
      if (!dimmed) {
        for (const stop of geometry.stops) {
          markers.push({
            id: `${plan.id}-${stop.sequence}`,
            latitude: stop.latitude,
            longitude: stop.longitude,
            title: `${stop.sequence}. ${stop.name || stop.code}`,
            description: vehicle,
            color,
            pinLabel: String(stop.sequence)
          });
        }
      }
      legend.push({ planId: plan.id, vehicleCode: vehicle, color, km, stops: geometry.stops.length, source: geometry.source, selected: isSelected });
    });

    // La ruta seleccionada se dibuja al final para que quede encima.
    polylines.sort((a, b) => Number(a.id === selected) - Number(b.id === selected));
    this.mapPolylines.set(polylines);
    this.mapMarkers.set([...depots.values(), ...markers]);
    this.routeLegend.set(legend);
  }

  private buildMap(detail: RoutePlanDetailDto | null): void {
    if (!detail) {
      this.mapMarkers.set([]);
      this.mapPolylines.set([]);
      return;
    }

    this.apiService.getServicePoints().subscribe(points => {
      this.apiService.getWarehouses().subscribe(warehouses => {
        const allLocations = [...warehouses, ...points];
        const markers: MapMarker[] = [];
        const coords: [number, number][] = [];

        for (const stop of detail.stops) {
          const pt = allLocations.find(p => p.id === stop.servicePointId || p.code === stop.servicePointCode);
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

        const detailCoordinates = (detail as any).coordinates;
        if (detailCoordinates && Array.isArray(detailCoordinates) && detailCoordinates.length > 1) {
          this.mapPolylines.set([{
            id: 'route-line',
            coordinates: detailCoordinates as [number, number][],
            color: '#00A7C4',
            weight: 5
          }]);
        } else if (coords.length > 1) {
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
    });
  }
}
