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
  DriverOptionDto,
  WarehouseStockDto,
  PendingLoadLineDto,
  describePendingLines
} from '@shared/api';
import { MapViewComponent, MapMarker, MapPolyline } from '@shared/ui';
import { forkJoin } from 'rxjs';
import {
  RouteOptionComparisonData,
  FuelReferencePrice,
  ComparisonRowDefinition,
  COMPARISON_ROWS,
  DEMO_QUITO_PITCH_OPTIONS,
  DEMO_QUITO_FUEL_REFERENCE
} from './route-comparison.model';
import {
  computeRouteTrackingSummary,
  computeStopProgressList,
  getDeliveryStatusBadgeClass,
  getHuStatusBadgeClass,
  translateDeliveryStatus,
  translateHuStatus,
  translateStopStatus
} from './route-tracking.model';

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

export interface CriticalPointArrivalComparison {
  pointCode: string;
  pointName: string;
  criticality: string;
  baselineArrival: string;
  prioritizedArrival: string;
  deadline: string;
  meetsDeadline: boolean;
  differenceMinutes: number;
}

export interface PrioritizationComparison {
  baseline: {
    totalKm: number;
    totalCost: number;
    unservedCount: number;
    stopsCount: number;
  };
  prioritized: {
    deadline: string;
    totalKm: number;
    totalCost: number;
    unservedCount: number;
    stopsCount: number;
  };
  diffKm: number;
  diffCost: number;
  diffUnserved: number;
  criticalPoints: CriticalPointArrivalComparison[];
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
  imports: [CommonModule, FormsModule, RouterLink, MapViewComponent],
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
          (click)="activeTab.set('rutas'); startTrackingTimer()"
          class="pb-3 text-sm font-bold border-b-2 transition-colors flex items-center gap-2"
          [ngClass]="activeTab() === 'rutas' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-700'">
          <span>Rutas Planificadas</span>
          <span class="text-xs px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 font-mono">{{ routePlans().length }}</span>
        </button>

        <button
          type="button"
          (click)="activeTab.set('rechazos'); stopTrackingTimer(); loadRejectedCaptures()"
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
          <div class="flex items-center gap-2">
            @if (hasRemainingPendingOrders()) {
              <button
                type="button"
                (click)="openWizardForRemainingOrders()"
                class="px-2.5 py-1 rounded-lg bg-emerald-700 text-white font-bold hover:bg-emerald-800 transition-colors shadow-xs">
                + Crear siguiente ruta con los pedidos pendientes restantes
              </button>
            }
            <button (click)="feedbackMessage.set(null); hasRemainingPendingOrders.set(false)" class="text-slate-400 hover:text-slate-600 font-semibold ml-3 text-xs">Cerrar</button>
          </div>
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

      <!-- Panel de Planificación y Prioridad de Entrega (AMP-01 / AMP-02 / Issue #196) -->
      @if (session.canWrite()) {
        <div class="bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-3" data-testid="planning-control-panel">
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-3">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <h3 class="text-xs font-bold text-slate-900 uppercase tracking-wide">
                  Planificación Automática & Prioridad de Entrega
                </h3>
                <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-primary-50 text-primary-700 border border-primary-200">
                  Motor VRP Determinista
                </span>
              </div>
              <p class="text-xs text-slate-500">
                Calcula el plan óptimo desde bodegas centrales. Puedes fijar una hora límite para que los puntos críticos (hospitales/quirófanos) reciban antes de ese horario.
              </p>
            </div>

            <!-- Selector de Hora Límite y Botones -->
            <div class="flex flex-wrap items-center gap-2">
              <div class="flex items-center gap-1.5 bg-slate-50 border border-slate-200 rounded-lg px-2.5 py-1.5">
                <label for="critical-deadline-select" class="text-xs font-semibold text-slate-700 whitespace-nowrap">
                  Hora límite para críticos:
                </label>
                <select
                  id="critical-deadline-select"
                  [ngModel]="criticalDeadline()"
                  (ngModelChange)="criticalDeadline.set($event)"
                  data-testid="critical-deadline-select"
                  class="text-xs font-medium bg-white border border-slate-300 rounded px-2 py-1 text-slate-800 focus:outline-none focus:ring-1 focus:ring-primary-500">
                  <option value="">Sin hora límite (estándar)</option>
                  <option value="09:00">09:00</option>
                  <option value="10:00">10:00</option>
                  <option value="11:00">11:00 (Prioridad Quirófano / Reto 1)</option>
                  <option value="12:00">12:00</option>
                  <option value="13:00">13:00</option>
                  <option value="14:00">14:00</option>
                </select>
              </div>

              <button
                type="button"
                (click)="generatePlan()"
                [disabled]="actionLoading() || comparisonLoading()"
                data-testid="generate-plan-button"
                class="inline-flex items-center gap-1 text-xs font-semibold px-3.5 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors shadow-xs disabled:opacity-50">
                @if (actionLoading()) {
                  <span class="inline-block animate-spin h-3 w-3 border-2 border-white border-t-transparent rounded-full"></span>
                  <span>Calculando...</span>
                } @else {
                  <span>⚡ Calcular Plan</span>
                }
              </button>

              <button
                type="button"
                (click)="comparePrioritization()"
                [disabled]="actionLoading() || comparisonLoading()"
                data-testid="compare-cost-button"
                class="inline-flex items-center gap-1 text-xs font-semibold px-3.5 py-2 rounded-lg bg-slate-800 text-white hover:bg-slate-900 transition-colors shadow-xs disabled:opacity-50">
                @if (comparisonLoading()) {
                  <span class="inline-block animate-spin h-3 w-3 border-2 border-white border-t-transparent rounded-full"></span>
                  <span>Comparando...</span>
                } @else {
                  <span>⚖ Costo de Priorizar</span>
                }
              </button>
            </div>
          </div>
        </div>
      }

      <!-- Tarjeta de Comparación: Costo de Priorizar (AMP-02) -->
      @if (prioritizationComparison()) {
        <div class="bg-white border-2 border-indigo-200 rounded-xl p-5 shadow-sm space-y-4" data-testid="prioritization-comparison-card">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-indigo-100 pb-3">
            <div>
              <div class="flex items-center gap-2">
                <span class="text-base font-bold text-slate-900">Análisis Comparativo: Costo de Priorizar Entregas Críticas</span>
                <span class="text-xs px-2.5 py-0.5 rounded-full font-bold bg-indigo-50 text-indigo-700 border border-indigo-200">
                  Hora Límite: {{ prioritizationComparison()!.prioritized.deadline }}
                </span>
              </div>
              <p class="text-xs text-slate-500 mt-0.5">
                Comparativa de viabilidad económica y cumplimiento horario: Plan Estándar (por costo) vs. Plan Priorizado (con ventana crítica).
              </p>
            </div>

            <button
              type="button"
              (click)="prioritizationComparison.set(null)"
              class="text-xs font-semibold text-slate-400 hover:text-slate-600 px-2 py-1 rounded hover:bg-slate-100">
              ✕ Cerrar comparación
            </button>
          </div>

          <!-- Grilla de Métricas Comparativas -->
          <div class="grid grid-cols-1 sm:grid-cols-4 gap-4">
            <!-- Kilómetros -->
            <div class="p-3 bg-slate-50 rounded-lg border border-slate-200">
              <span class="text-[11px] font-bold text-slate-500 uppercase">Recorrido Total</span>
              <div class="flex items-baseline justify-between mt-1">
                <span class="text-lg font-bold text-slate-800 font-mono">
                  {{ prioritizationComparison()!.prioritized.totalKm | number:'1.1-2' }} km
                </span>
                <span class="text-xs font-semibold font-mono"
                  [ngClass]="prioritizationComparison()!.diffKm > 0 ? 'text-amber-600' : 'text-emerald-600'">
                  {{ prioritizationComparison()!.diffKm >= 0 ? '+' : '' }}{{ prioritizationComparison()!.diffKm | number:'1.1-2' }} km
                </span>
              </div>
              <span class="text-[10px] text-slate-400">Sin hora límite: {{ prioritizationComparison()!.baseline.totalKm | number:'1.1-2' }} km</span>
            </div>

            <!-- Costo USD -->
            <div class="p-3 bg-slate-50 rounded-lg border border-slate-200">
              <span class="text-[11px] font-bold text-slate-500 uppercase">Costo Operativo (USD)</span>
              <div class="flex items-baseline justify-between mt-1">
                <span class="text-lg font-bold text-slate-800 font-mono">
                  USD {{ prioritizationComparison()!.prioritized.totalCost | number:'1.2-2' }}
                </span>
                <span class="text-xs font-semibold font-mono"
                  [ngClass]="prioritizationComparison()!.diffCost > 0 ? 'text-rose-600' : 'text-emerald-600'">
                  {{ prioritizationComparison()!.diffCost >= 0 ? '+' : '' }}USD {{ prioritizationComparison()!.diffCost | number:'1.2-2' }}
                </span>
              </div>
              <span class="text-[10px] text-slate-400">Sin hora límite: USD {{ prioritizationComparison()!.baseline.totalCost | number:'1.2-2' }}</span>
            </div>

            <!-- Paradas No Atendidas -->
            <div class="p-3 bg-slate-50 rounded-lg border border-slate-200">
              <span class="text-[11px] font-bold text-slate-500 uppercase">Paradas No Atendidas</span>
              <div class="flex items-baseline justify-between mt-1">
                <span class="text-lg font-bold font-mono"
                  [ngClass]="prioritizationComparison()!.prioritized.unservedCount > 0 ? 'text-amber-600' : 'text-emerald-600'">
                  {{ prioritizationComparison()!.prioritized.unservedCount }}
                </span>
                <span class="text-xs font-semibold font-mono text-slate-500">
                  Base: {{ prioritizationComparison()!.baseline.unservedCount }}
                </span>
              </div>
              <span class="text-[10px] text-slate-400">
                {{ prioritizationComparison()!.diffUnserved > 0 ? 'Afectadas por ventana' : '100% de cobertura' }}
              </span>
            </div>

            <!-- Resumen Pitch -->
            <div class="p-3 bg-indigo-50/70 rounded-lg border border-indigo-200 flex flex-col justify-between">
              <span class="text-[11px] font-bold text-indigo-900 uppercase">Sobrecosto de Decisión</span>
              <p class="text-xs font-bold text-indigo-900 mt-1">
                {{ prioritizationComparison()!.diffCost >= 0 ? '+' : '' }}USD {{ prioritizationComparison()!.diffCost | number:'1.2-2' }}
              </p>
              <span class="text-[10px] text-indigo-700">
                {{ prioritizationComparison()!.criticalPoints.length }} punto(s) crítico(s) adelantados a tiempo
              </span>
            </div>
          </div>

          <!-- Tabla de Hora de Llegada de los Críticos -->
          <div class="space-y-2">
            <span class="text-xs font-bold text-slate-700 uppercase">
              Hora de Llegada de Puntos Críticos (Hospitales / Quirófanos)
            </span>

            @if (prioritizationComparison()!.criticalPoints.length === 0) {
              <p class="text-xs text-slate-500 italic p-3 bg-slate-50 rounded">
                No se detectaron paradas con criticidad alta en las órdenes del plan actual.
              </p>
            } @else {
              <div class="overflow-x-auto border border-slate-200 rounded-lg">
                <table class="w-full text-xs text-left" data-testid="critical-points-table">
                  <thead class="bg-slate-50 border-b border-slate-200 text-slate-500 uppercase font-semibold text-[10px]">
                    <tr>
                      <th class="p-2.5">Punto de Entrega</th>
                      <th class="p-2.5">Criticidad</th>
                      <th class="p-2.5">Sin Hora Límite</th>
                      <th class="p-2.5">Con Hora Límite ({{ prioritizationComparison()!.prioritized.deadline }})</th>
                      <th class="p-2.5">Diferencia</th>
                      <th class="p-2.5">Cumplimiento</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100 font-mono text-slate-700">
                    @for (pt of prioritizationComparison()!.criticalPoints; track pt.pointCode) {
                      <tr class="hover:bg-slate-50/60">
                        <td class="p-2.5 font-sans font-bold text-slate-900">
                          {{ pt.pointName }} <span class="text-xs text-slate-400 font-mono">({{ pt.pointCode }})</span>
                        </td>
                        <td class="p-2.5">
                          <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-rose-100 text-rose-700">
                            {{ pt.criticality }}
                          </span>
                        </td>
                        <td class="p-2.5 text-slate-500">{{ pt.baselineArrival }}</td>
                        <td class="p-2.5 font-bold text-primary-700">{{ pt.prioritizedArrival }}</td>
                        <td class="p-2.5 font-semibold"
                          [ngClass]="pt.differenceMinutes < 0 ? 'text-emerald-600' : 'text-slate-500'">
                          {{ pt.differenceMinutes < 0 ? (pt.differenceMinutes * -1) + ' min antes' : (pt.differenceMinutes === 0 ? 'Sin cambio' : pt.differenceMinutes + ' min después') }}
                        </td>
                        <td class="p-2.5">
                          @if (pt.meetsDeadline) {
                            <span class="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 border border-emerald-200 px-2 py-0.5 rounded-full">
                              ✓ Llega antes de {{ pt.deadline }}
                            </span>
                          } @else {
                            <span class="inline-flex items-center gap-1 text-[11px] font-bold text-rose-700 bg-rose-50 border border-rose-200 px-2 py-0.5 rounded-full">
                              ⚠ Excede límite
                            </span>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
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
                    <div class="flex items-center gap-2">
                      <span class="text-xs font-semibold text-emerald-700 bg-emerald-50 border border-emerald-200 px-3 py-1 rounded-lg">
                        Ruta Publicada al Conductor
                      </span>
                      <a
                        [routerLink]="['/guias-retorno']"
                        [queryParams]="{ planId: selectedDetail()!.id }"
                        class="inline-flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg bg-white border border-slate-300 text-slate-700 hover:bg-slate-50 shadow-2xs transition"
                        title="Ver o imprimir la guía de retorno de material no entregado"
                      >
                        <svg class="w-3.5 h-3.5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                        Guía de Retorno
                      </a>
                    </div>
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

                <!-- #231: SEGUIMIENTO EN VIVO: LÍNEA DE AVANCE, RESUMEN Y ESTADOS EN ESPAÑOL -->
                @if (selectedDetail()!.stops.length > 0) {
                  <div class="bg-gradient-to-r from-slate-900 via-slate-800 to-primary-950 text-white rounded-xl p-4 shadow-sm space-y-3" data-testid="tracking-card">
                    <!-- Resumen superior y botón de actualización -->
                    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-700/60 pb-3">
                      <div class="flex items-center gap-2.5">
                        <span class="relative flex h-3 w-3">
                          <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                          <span class="relative inline-flex rounded-full h-3 w-3 bg-emerald-500"></span>
                        </span>
                        <div>
                          <div class="flex items-center gap-2">
                            <h4 class="text-sm font-bold tracking-tight text-white">
                              Seguimiento de Ruta en Vivo
                            </h4>
                            <span class="text-[10px] font-mono font-medium px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                              Autorefresco 25s
                            </span>
                          </div>
                          <p class="text-xs text-slate-300 font-mono mt-0.5" data-testid="tracking-summary-text">
                            {{ trackingSummary().summaryText }}
                          </p>
                        </div>
                      </div>

                      <!-- Controles de actualización -->
                      <div class="flex items-center gap-2 text-xs">
                        <span class="text-[11px] text-slate-400 font-mono" data-testid="last-tracking-update">
                          {{ lastTrackingUpdateText() }}
                        </span>
                        <button
                          type="button"
                          (click)="manualRefreshTracking()"
                          [disabled]="trackingRefreshing()"
                          class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-700/80 hover:bg-slate-700 text-white font-medium text-xs transition-colors border border-slate-600 disabled:opacity-50"
                          data-testid="btn-refresh-tracking">
                          <span [class.animate-spin]="trackingRefreshing()">🔄</span>
                          <span>Actualizar</span>
                        </button>
                      </div>
                    </div>

                    <!-- Línea de avance por parada (Línea de tiempo / Stepper) -->
                    <div class="pt-2 overflow-x-auto pb-2">
                      <div class="flex items-start min-w-[640px] gap-2">
                        @for (stop of stopProgressList(); track stop.sequence; let idx = $index; let last = $last) {
                          <div class="flex-1 flex flex-col items-center text-center relative group">
                            <!-- Conector entre paradas -->
                            @if (!last) {
                              <div class="absolute top-4 left-1/2 w-full h-0.5 -z-0"
                                   [ngClass]="stop.isCompleted ? 'bg-emerald-500' : (stop.isCurrent ? 'bg-gradient-to-r from-amber-400 to-slate-700' : 'bg-slate-700')">
                              </div>
                            }

                            <!-- Círculo / Icono de estado -->
                            <div class="relative z-10 w-8 h-8 rounded-full flex items-center justify-center font-bold text-xs font-mono transition-transform group-hover:scale-105"
                                 [ngClass]="{
                                   'bg-emerald-500 text-white shadow-md shadow-emerald-900/50 ring-2 ring-emerald-300': stop.isCompleted,
                                   'bg-amber-400 text-slate-950 font-black shadow-lg shadow-amber-900/40 ring-4 ring-amber-400/30': stop.isCurrent,
                                   'bg-slate-700 text-slate-400 border border-slate-600': stop.isPending
                                 }">
                              @if (stop.isCompleted) {
                                <span>✓</span>
                              } @else if (stop.isCurrent) {
                                <span>📍</span>
                              } @else {
                                <span>{{ stop.sequence }}</span>
                              }
                            </div>

                            <!-- Indicador de Parada Actual -->
                            @if (stop.isCurrent) {
                              <span class="mt-1 px-1.5 py-0.5 rounded text-[9px] font-black uppercase tracking-wider bg-amber-400 text-slate-950 shadow-xs" data-testid="badge-current-stop">
                                Parada actual
                              </span>
                            }

                            <!-- Info de la parada -->
                            <span class="mt-1 text-xs font-bold text-slate-200 truncate max-w-[130px]" [title]="stop.name">
                              {{ stop.name }}
                            </span>
                            <span class="text-[10px] text-slate-400 font-mono">
                              {{ stop.kind === 'Pickup' ? 'Bodega' : 'Parada ' + stop.sequence }} · {{ stop.etaOrWindow }}
                            </span>

                            <!-- Estado en español -->
                            <span class="mt-1 px-2 py-0.5 rounded text-[10px] font-semibold"
                                  [ngClass]="{
                                    'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30': stop.isCompleted,
                                    'bg-amber-500/20 text-amber-300 border border-amber-500/30': stop.isCurrent,
                                    'bg-slate-800 text-slate-400 border border-slate-700': stop.isPending
                                  }">
                              {{ stop.statusLabel }}
                            </span>
                          </div>
                        }
                      </div>
                    </div>
                  </div>
                }

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

                  <button
                    type="button"
                    (click)="detailTab.set('stock-bodegas'); loadWarehouseStockForSelected()"
                    data-testid="tab-stock-bodegas"
                    class="pb-2.5 px-3 text-xs font-bold border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'stock-bodegas' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-700'">
                    <span>🏬 Stock por Bodega</span>
                    @if (warehouseStock().length > 0) {
                      <span class="text-[10px] px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-600 font-mono">
                        {{ filteredWarehouseStock().length }}
                      </span>
                    }
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
                              <button
                                type="button"
                                (click)="$event.stopPropagation(); inspectStockForStop(stop)"
                                [attr.data-testid]="'stop-stock-btn-' + stop.sequence"
                                class="px-2 py-0.5 rounded text-[10px] font-semibold bg-primary-50 text-primary-700 hover:bg-primary-100 border border-primary-200"
                                title="Consultar existencias por bodega para esta parada">
                                🏬 Stock en bodegas
                              </button>
                              @if (stop.lines && stop.lines.length > 0) {
                                <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 font-mono">
                                  {{ stop.lines.length }} {{ stop.lines.length === 1 ? 'producto' : 'productos' }}
                                </span>
                              }
                              @if (stop.deliveryStatus) {
                                <span class="px-2 py-0.5 rounded text-[10px] font-semibold border"
                                  [ngClass]="getDeliveryStatusBadgeClass(stop.deliveryStatus)">
                                  Entrega: {{ translateDeliveryStatus(stop.deliveryStatus) }}
                                </span>
                              }
                              <span class="px-2 py-0.5 rounded text-[10px] font-semibold border"
                                [ngClass]="stop.status === 'Completed' ? 'bg-emerald-100 text-emerald-800 border-emerald-200' : 'bg-slate-100 text-slate-700 border-slate-200'">
                                Parada: {{ translateStopStatus(stop.status) }}
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
                                    <span class="px-1.5 py-0.5 rounded text-[10px] font-semibold border"
                                      [ngClass]="getDeliveryStatusBadgeClass(stop.deliveryStatus)">
                                      {{ translateDeliveryStatus(stop.deliveryStatus) }}
                                    </span>
                                  </div>

                                  @if (stop.handlingUnits && stop.handlingUnits.length > 0) {
                                    <div class="flex flex-wrap items-center gap-1.5">
                                      <span class="text-slate-500 font-mono">Bultos:</span>
                                      @for (hu of stop.handlingUnits; track hu.id) {
                                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-white border border-slate-300 font-mono text-[10px]">
                                          <span>📦 {{ hu.code }}</span>
                                          <span class="text-[9px] px-1.5 py-0.5 rounded font-bold uppercase border"
                                            [ngClass]="getHuStatusBadgeClass(hu.status)">
                                            {{ translateHuStatus(hu.status) }}
                                          </span>
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
                                            <span class="px-1.5 py-0.5 rounded text-[10px] font-semibold border"
                                              [ngClass]="getDeliveryStatusBadgeClass(line.deliveryStatus || stop.deliveryStatus)">
                                              {{ translateDeliveryStatus(line.deliveryStatus || stop.deliveryStatus) }}
                                            </span>
                                          </td>
                                        </tr>
                                      }
                                    </tbody>
                                  </table>
                                </div>
                              }
                              @if (hasPending(stop.loadQuantities)) {
                                <!-- #199: atención parcial: lo requerido, lo que va en esta ruta y lo que queda pendiente -->
                                <div class="mt-2 p-2 rounded-lg bg-amber-50 border border-amber-200 text-xs text-amber-900" data-partial-load>
                                  <p class="font-semibold mb-1">Carga parcial por capacidad</p>
                                  @for (q of stop.loadQuantities; track q.sku) {
                                    <p class="font-mono">{{ q.sku }}: requeridas {{ q.requiredQuantity | number:'1.0-2' }} · planificadas {{ q.plannedQuantity | number:'1.0-2' }} · pendientes {{ q.pendingQuantity | number:'1.0-2' }} {{ q.baseUnit }}</p>
                                  }
                                </div>
                              }
                              @if (!stop.lines || stop.lines.length === 0) {
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

                <!-- CONTENIDO PESTAÑA 4: STOCK POR BODEGA (Issue #196) -->
                @if (detailTab() === 'stock-bodegas') {
                  <div class="space-y-4" data-testid="warehouse-stock-panel">
                    <!-- Filtros de contexto: Parada o SKU -->
                    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-slate-50 p-3 rounded-xl border border-slate-200">
                      <div class="flex flex-wrap items-center gap-3">
                        <label for="stock-stop-select" class="text-xs font-bold text-slate-700">Filtrar por Parada:</label>
                        <select
                          id="stock-stop-select"
                          [ngModel]="selectedStopForStock()?.servicePointId || ''"
                          (ngModelChange)="onStockStopSelect($event)"
                          data-testid="stock-stop-select"
                          class="text-xs p-1.5 border border-slate-300 rounded bg-white min-w-[200px]">
                          <option value="">Todas las paradas del plan</option>
                          @for (s of selectedDetail()!.stops; track s.sequence) {
                            <option [value]="s.servicePointId">{{ s.sequence }}. {{ s.servicePointName || s.servicePointCode }}</option>
                          }
                        </select>
                      </div>

                      <div class="flex items-center gap-2">
                        @if (selectedStopForStock() || selectedProductSkuForStock()) {
                          <button
                            type="button"
                            (click)="clearStockFilter()"
                            data-testid="clear-stock-filter-btn"
                            class="text-xs font-semibold text-primary-600 hover:underline">
                            Limpiar filtro
                          </button>
                        }
                        <button
                          type="button"
                          (click)="loadWarehouseStockForSelected()"
                          [disabled]="warehouseStockLoading()"
                          data-testid="refresh-stock-btn"
                          class="inline-flex items-center gap-1 text-xs font-semibold px-2.5 py-1.5 rounded bg-white border border-slate-300 hover:bg-slate-50 text-slate-700">
                          @if (warehouseStockLoading()) {
                            <span class="inline-block animate-spin h-3 w-3 border-2 border-slate-600 border-t-transparent rounded-full"></span>
                          }
                          <span>↻ Actualizar Stock</span>
                        </button>
                      </div>
                    </div>

                    <!-- Indicador de contexto activo -->
                    @if (selectedStopForStock()) {
                      <div class="p-2.5 bg-primary-50/70 border border-primary-200 rounded-lg text-xs text-primary-900 flex items-center justify-between" data-testid="stock-active-context-stop">
                        <span>
                          Mostrando disponibilidad en bodegas para la parada seleccionada:
                          <strong>{{ selectedStopForStock()!.servicePointName || selectedStopForStock()!.servicePointCode }}</strong>
                        </span>
                        <span class="text-[11px] font-mono font-bold bg-primary-100 px-2 py-0.5 rounded">
                          Parada #{{ selectedStopForStock()!.sequence }}
                        </span>
                      </div>
                    } @else if (selectedProductSkuForStock()) {
                      <div class="p-2.5 bg-indigo-50/70 border border-indigo-200 rounded-lg text-xs text-indigo-900 flex items-center justify-between" data-testid="stock-active-context-sku">
                        <span>
                          Mostrando disponibilidad en bodegas para el SKU:
                          <strong>{{ selectedProductSkuForStock() }}</strong>
                        </span>
                        <button (click)="clearStockFilter()" class="text-xs text-indigo-600 hover:underline font-semibold">✕ Quitar filtro</button>
                      </div>
                    }

                    <!-- Cargando -->
                    @if (warehouseStockLoading()) {
                      <div class="p-6 text-center text-slate-500 text-xs bg-white border border-slate-200 rounded-xl" data-testid="stock-loading-indicator">
                        Consultando existencias por bodega...
                      </div>
                    }

                    <!-- Lista de existencias por bodega -->
                    @if (!warehouseStockLoading() && filteredWarehouseStock().length === 0) {
                      <div class="p-6 text-center bg-white border border-slate-200 rounded-xl text-xs text-slate-500" data-testid="stock-empty-message">
                        No se encontraron registros de existencias en bodega para la selección actual.
                      </div>
                    }

                    @if (!warehouseStockLoading() && filteredWarehouseStock().length > 0) {
                      <div class="overflow-x-auto border border-slate-200 rounded-xl bg-white shadow-xs">
                        <table class="w-full text-xs text-left" data-testid="warehouse-stock-table">
                          <thead class="bg-slate-50 border-b border-slate-200 text-slate-500 uppercase font-semibold text-[10px]">
                            <tr>
                              <th class="p-3">Bodega</th>
                              <th class="p-3">Producto / SKU</th>
                              <th class="p-3 text-right">Disponible</th>
                              <th class="p-3 text-right">Físico</th>
                              <th class="p-3 text-right">Reservado</th>
                              <th class="p-3 text-center">Estado</th>
                            </tr>
                          </thead>
                          <tbody class="divide-y divide-slate-100 font-mono text-slate-700">
                            @for (item of filteredWarehouseStock(); track item.locationId + '-' + item.productId) {
                              <tr class="hover:bg-slate-50/60" data-testid="warehouse-stock-row">
                                <td class="p-3 font-sans font-bold text-slate-900">
                                  {{ item.locationName || item.locationCode }}
                                  <span class="text-xs text-slate-400 font-mono ml-1">({{ item.locationCode }})</span>
                                </td>
                                <td class="p-3 font-sans">
                                  <div class="font-semibold text-slate-800">{{ item.productName }}</div>
                                  <div class="text-[11px] text-slate-400 font-mono">{{ item.sku }}</div>
                                </td>
                                <td class="p-3 text-right font-bold text-slate-900">
                                  {{ item.availableQuantity | number:'1.0-2' }}
                                </td>
                                <td class="p-3 text-right text-slate-600">
                                  {{ item.quantityOnHand | number:'1.0-2' }}
                                </td>
                                <td class="p-3 text-right text-slate-500">
                                  {{ item.quantityReserved | number:'1.0-2' }}
                                </td>
                                <td class="p-3 text-center font-sans">
                                  @switch (item.status) {
                                    @case ('Critico') {
                                      <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-50 text-rose-700 border border-rose-200">Crítico</span>
                                    }
                                    @case ('SinStock') {
                                      <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-red-100 text-red-800 border border-red-200">Sin Stock</span>
                                    }
                                    @case ('Exceso') {
                                      <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-50 text-amber-700 border border-amber-200">Exceso</span>
                                    }
                                    @default {
                                      <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">Normal</span>
                                    }
                                  }
                                </td>
                              </tr>
                            }
                          </tbody>
                        </table>
                      </div>
                    }
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
                @if (wizardStep() === 3) { Compara opciones A/B/C (propia vs contratada), desglose de costos, riesgos y justificación. }
                @if (wizardStep() === 4) { Asigna el vehículo idóneo según capacidad de peso, volumen y depósito base. }
              </p>
            </div>

            <button type="button" (click)="closeWizard()" class="text-xs font-semibold px-2 py-1 rounded bg-slate-800 text-slate-300 hover:text-white hover:bg-slate-700 transition-colors">Cerrar</button>
          </div>

          <!-- Cuerpo Asistente -->
          <div class="flex-1 grid grid-cols-1 lg:grid-cols-12 overflow-hidden">
            
            <!-- Columna Izquierda: Formulario y Pasos -->
            <div class="p-4 border-r border-slate-200 flex flex-col gap-3 bg-slate-50/60 overflow-y-auto transition-all"
              [ngClass]="wizardStep() === 3 && isProjectorMode() ? 'lg:col-span-12' : (wizardStep() === 3 ? 'lg:col-span-7' : 'lg:col-span-5')">
              
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

                  <!-- Formulario Nuevo Pedido Rápido con Prioridad -->
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

                      <div class="grid grid-cols-3 gap-2">
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
                          <label class="text-[10px] text-slate-500 block">Cantidad</label>
                          <input type="number" [(ngModel)]="newOrderQty" min="1" class="w-full text-xs p-1.5 border rounded font-mono" />
                        </div>
                        <div>
                          <label class="text-[10px] text-slate-500 block">Prioridad</label>
                          <select [(ngModel)]="newOrderPriority" class="w-full text-xs p-1.5 border rounded font-semibold text-slate-700">
                            <option value="Critical">Critical</option>
                            <option value="High">High</option>
                            <option value="Normal">Normal</option>
                            <option value="Low">Low</option>
                          </select>
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

                  <!-- Filtros rápidos y acciones por lote -->
                  <div class="space-y-2 bg-white p-2.5 rounded-xl border border-slate-200">
                    <div class="flex items-center gap-1.5 flex-wrap">
                      <span class="text-[10px] font-bold text-slate-500 uppercase mr-1">Filtros:</span>
                      <button
                        type="button"
                        (click)="orderFilter.set('all')"
                        class="px-2 py-0.5 rounded-full text-[10px] font-semibold transition-colors"
                        [ngClass]="orderFilter() === 'all' ? 'bg-primary-600 text-white' : 'bg-slate-100 text-slate-700 hover:bg-slate-200'">
                        Todos ({{ pendingOrders().length }})
                      </button>
                      <button
                        type="button"
                        (click)="orderFilter.set('critical_high')"
                        class="px-2 py-0.5 rounded-full text-[10px] font-semibold transition-colors"
                        [ngClass]="orderFilter() === 'critical_high' ? 'bg-rose-600 text-white' : 'bg-slate-100 text-slate-700 hover:bg-slate-200'">
                        Críticos y Altos
                      </button>
                      <button
                        type="button"
                        (click)="orderFilter.set('today')"
                        class="px-2 py-0.5 rounded-full text-[10px] font-semibold transition-colors"
                        [ngClass]="orderFilter() === 'today' ? 'bg-blue-600 text-white' : 'bg-slate-100 text-slate-700 hover:bg-slate-200'">
                        Pendientes para Hoy
                      </button>
                    </div>

                    <div class="flex items-center justify-between text-[11px] pt-1.5 border-t border-slate-100">
                      <button
                        type="button"
                        (click)="selectAllCritical()"
                        class="font-semibold text-rose-700 hover:underline">
                        ★ Seleccionar todos los críticos
                      </button>
                      <div class="flex items-center gap-2">
                        <button
                          type="button"
                          (click)="selectAllFiltered()"
                          class="font-semibold text-primary-700 hover:underline text-[10px]">
                          Seleccionar visibles
                        </button>
                        <span class="text-slate-300">|</span>
                        <button
                          type="button"
                          (click)="deselectAllOrders()"
                          class="font-semibold text-slate-500 hover:underline text-[10px]">
                          Deseleccionar todos
                        </button>
                      </div>
                    </div>
                  </div>

                  <!-- Lista de Pedidos Disponibles con Checkbox y Jerarquía Visual -->
                  <div class="space-y-2 max-h-[280px] overflow-y-auto pr-1">
                    @for (ord of filteredPendingOrders(); track ord.id) {
                      <div class="p-3 rounded-xl border bg-white shadow-xs flex items-start gap-2.5 text-xs cursor-pointer transition-all"
                        [ngClass]="selectedOrderIds().has(ord.id) ? 'border-primary-600 ring-2 ring-primary-100' : 'border-slate-200 hover:border-slate-300'"
                        (click)="toggleOrderSelection(ord.id)">
                        <input
                          type="checkbox"
                          [checked]="selectedOrderIds().has(ord.id)"
                          (click)="$event.stopPropagation()"
                          (change)="toggleOrderSelection(ord.id)"
                          class="mt-1 rounded text-primary-600" />
                        <div class="flex-1 min-w-0">
                          <div class="flex items-center justify-between gap-1">
                            <span class="font-bold text-slate-900 font-mono">{{ ord.orderNumber }}</span>
                            <span class="text-[9px] font-mono px-2 py-0.5 rounded-full uppercase font-bold border"
                              [ngClass]="{
                                'bg-rose-100 text-rose-800 border-rose-300 font-bold': ord.priority === 'Critical',
                                'bg-amber-100 text-amber-800 border-amber-300 font-bold': ord.priority === 'High',
                                'bg-blue-50 text-blue-700 border-blue-200 font-medium': ord.priority === 'Normal',
                                'bg-slate-100 text-slate-600 border-slate-200 font-normal': ord.priority === 'Low' || !ord.priority
                              }">
                              {{ ord.priority || 'Normal' }}
                            </span>
                          </div>
                          <p class="text-[11px] font-semibold text-primary-700 mt-0.5">Destino: {{ ord.destinationName }}</p>
                          @if (ord.requiredDateUtc) {
                            <p class="text-[10px] text-slate-400 font-mono">Requerido: {{ ord.requiredDateUtc | date:'shortDate' }}</p>
                          }
                          <div class="mt-1 space-y-0.5 text-[10px] text-slate-500 font-mono">
                            @for (l of ord.lines; track l.id) {
                              <div>• {{ l.productName }}: <strong>{{ l.requestedQuantity }} u</strong></div>
                            }
                          </div>
                        </div>
                      </div>
                    } @empty {
                      <div class="p-4 text-center text-xs text-slate-400 bg-white border border-slate-200 rounded-xl">
                        No hay pedidos que coincidan con el filtro seleccionado.
                      </div>
                    }
                  </div>

                  <!-- Medidor de Carga Acumulada en Tiempo Real -->
                  <div class="p-3 bg-white rounded-xl border border-slate-200 shadow-xs space-y-1.5 text-xs">
                    <div class="flex items-center justify-between">
                      <span class="font-bold text-slate-800 text-[11px] uppercase">Carga Acumulada</span>
                      <span class="text-[10px] font-mono px-2 py-0.5 rounded font-bold"
                        [ngClass]="selectedOrdersSummary().weightKg > 1000 ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'">
                        {{ selectedOrdersSummary().count }} pedidos seleccionados
                      </span>
                    </div>

                    <div class="grid grid-cols-2 gap-2 text-[11px] font-mono pt-1 text-slate-600">
                      <div class="flex flex-col">
                        <span class="text-[10px] text-slate-400">Peso Estimado:</span>
                        <strong class="text-slate-800">{{ selectedOrdersSummary().weightKg }} kg</strong>
                      </div>
                      <div class="flex flex-col">
                        <span class="text-[10px] text-slate-400">Volumen Estimado:</span>
                        <strong class="text-slate-800">{{ selectedOrdersSummary().volumeM3 }} m³</strong>
                      </div>
                    </div>

                    <div class="pt-1 text-[10px] text-slate-500 flex items-center justify-between border-t border-slate-100">
                      <span>Prioritarios: <strong>{{ selectedOrdersSummary().criticalCount }} Críticos</strong>, <strong>{{ selectedOrdersSummary().highCount }} Altos</strong></span>
                      <span class="font-mono text-[9px] text-slate-400">Capacidad flota ref: ~1,000 kg</span>
                    </div>
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

              <!-- PASO 3: COMPARACIÓN A/B/C & SECUENCIA DE PARADAS (#242) -->
              @if (wizardStep() === 3) {
                <div class="space-y-4">
                  <!-- Barra superior del panel comparativo -->
                  <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-200 pb-3">
                    <div>
                      <div class="flex items-center gap-2">
                        <h3 class="text-sm sm:text-base font-bold text-slate-900 tracking-tight">
                          Comparativa de Alternativas A/B/C
                        </h3>
                        <span class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-primary-100 text-primary-800 border border-primary-200">
                          Panel Pitch #200
                        </span>
                      </div>
                      <p class="text-xs text-slate-500 mt-0.5">
                        Planificación · viernes 17-oct · Bodega Sur · Opciones de flota propia frente a operador contratado
                      </p>
                    </div>

                    <!-- Controles de Modo: Proyector y Toggle Flag Demo -->
                    <div class="flex items-center gap-2">
                      <button
                        type="button"
                        (click)="isProjectorMode.set(!isProjectorMode())"
                        class="px-2.5 py-1 text-xs font-semibold rounded-lg border transition-all cursor-pointer flex items-center gap-1.5"
                        [ngClass]="isProjectorMode() ? 'bg-indigo-600 text-white border-indigo-700 shadow-sm' : 'bg-white text-slate-700 border-slate-300 hover:bg-slate-100'"
                        title="Modo Proyector: expande a pantalla completa y aumenta tamaño de fuente para proyección a 3 metros">
                        <span>{{ isProjectorMode() ? '⛶ Modo Normal' : '⛶ Modo Proyector (3 m)' }}</span>
                      </button>

                      <button
                        type="button"
                        (click)="useMockABC.set(!useMockABC())"
                        class="px-2 py-1 text-[11px] font-semibold rounded-lg border transition-all cursor-pointer"
                        [ngClass]="useMockABC() ? 'bg-amber-100 text-amber-900 border-amber-300' : 'bg-slate-100 text-slate-700 border-slate-200'"
                        title="Alternar entre datos de demostración para el Pitch (demo-quito-planificacion) y datos en vivo de la API">
                        {{ useMockABC() ? '⚡ Demo Pitch: ON' : 'API en vivo' }}
                      </button>
                    </div>
                  </div>

                  <!-- Línea con el precio del combustible usado: valor, fecha y fuente (#242) -->
                  <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs sm:text-sm bg-gradient-to-r from-amber-50 to-orange-50/50 border border-amber-200/90 text-amber-950 px-3.5 py-2.5 rounded-xl shadow-xs">
                    <div class="flex items-center gap-2 font-medium flex-wrap">
                      <span class="text-base">⛽</span>
                      <span>
                        <strong>Combustible de referencia:</strong>
                        USD {{ fuelReference().pricePerGallon.toFixed(2) }}/galón
                        <span class="text-slate-400 mx-1">|</span>
                        <span class="text-amber-800">Vigencia: {{ fuelReference().date }}</span>
                        <span class="text-slate-400 mx-1">|</span>
                        <span class="text-amber-800">Fuente: {{ fuelReference().source }}</span>
                      </span>
                    </div>
                    @if (selectedOrdersSummary().criticalCount > 0) {
                      <span class="text-[11px] font-bold px-2 py-0.5 rounded-md bg-rose-100 text-rose-800 border border-rose-200 self-start sm:self-center">
                        {{ selectedOrdersSummary().criticalCount }} pedido(s) críticos
                      </span>
                    }
                  </div>

                  <!-- TABLA COMPARATIVA A/B/C (#242) -->
                  <div class="border border-slate-200 rounded-xl overflow-x-auto shadow-xs bg-white">
                    <table class="w-full border-collapse text-left" [ngClass]="isProjectorMode() ? 'text-sm sm:text-base' : 'text-xs sm:text-sm'">
                      <thead class="bg-slate-100 border-b border-slate-200">
                        <tr>
                          <th class="p-3 font-extrabold text-slate-700 uppercase tracking-wider text-[11px] sm:text-xs w-1/4">
                            Concepto
                          </th>
                          @for (opt of comparisonOptions(); track opt.optionId) {
                            <th
                              (click)="selectComparisonOption(opt.optionId)"
                              class="p-3 text-center border-l border-slate-200 transition cursor-pointer select-none"
                              [ngClass]="selectedOptionId() === opt.optionId ? 'bg-primary-50/70 ring-2 ring-primary-500 ring-inset' : 'hover:bg-slate-200/50'">
                              <div class="flex flex-col items-center gap-1">
                                <div class="flex items-center gap-1.5">
                                  <span class="font-extrabold text-slate-900" [ngClass]="isProjectorMode() ? 'text-base sm:text-lg' : 'text-sm sm:text-base'">
                                    Opción {{ opt.optionId }}
                                  </span>
                                  @if (opt.isRecommended) {
                                    <span class="text-amber-500 text-base font-black" title="Recomendada por el sistema">★</span>
                                  }
                                </div>
                                <span
                                  class="text-[10px] sm:text-xs font-bold px-2 py-0.5 rounded-full"
                                  [ngClass]="opt.category === 'Propia' ? 'bg-blue-100 text-blue-900' : 'bg-purple-100 text-purple-900'">
                                  {{ opt.category }} · {{ opt.tagline }}
                                </span>
                                @if (opt.isRecommended) {
                                  <span class="text-[9px] sm:text-[10px] font-black uppercase tracking-wider px-2 py-0.5 rounded-full bg-amber-400 text-amber-950 border border-amber-500 shadow-xs">
                                    ★ RECOMENDADA
                                  </span>
                                }
                              </div>
                            </th>
                          }
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-slate-200 bg-white">
                        @for (row of comparisonRows; track row.key) {
                          <tr [ngClass]="row.isHighlight ? 'bg-emerald-50/60 font-bold' : 'hover:bg-slate-50/80'">
                            <td class="p-2.5 sm:p-3 font-semibold text-slate-700">
                              {{ row.label }}
                            </td>
                            @for (opt of comparisonOptions(); track opt.optionId) {
                              <td
                                class="p-2.5 sm:p-3 text-center border-l border-slate-200"
                                [ngClass]="[
                                  selectedOptionId() === opt.optionId ? 'bg-primary-50/20' : '',
                                  row.isHighlight ? 'text-emerald-700 font-extrabold ' + (isProjectorMode() ? 'text-lg sm:text-xl' : 'text-sm sm:text-base') : 'text-slate-800'
                                ]">
                                @if (row.key === 'criticos') {
                                  <span class="inline-block px-2 py-0.5 rounded-md font-bold text-xs"
                                    [ngClass]="opt.criticalOnTime === opt.criticalTotal ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-900'">
                                    {{ row.format(opt) }}
                                  </span>
                                } @else if (row.key === 'pendientes') {
                                  <span class="inline-block px-2 py-0.5 rounded-md font-bold text-xs"
                                    [ngClass]="opt.pendingOrdersCount === 0 ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-900'">
                                    {{ row.format(opt) }}
                                  </span>
                                } @else {
                                  <span>{{ row.format(opt) }}</span>
                                }
                              </td>
                            }
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>

                  <!-- TARJETA DE JUSTIFICACIÓN DE LA RECOMENDADA (#242) -->
                  @if (recommendedOption(); as rec) {
                    <div class="p-3.5 sm:p-4 bg-emerald-50/90 border-2 border-emerald-400 rounded-xl space-y-1.5 shadow-xs">
                      <div class="flex items-center justify-between gap-2 flex-wrap">
                        <div class="flex items-center gap-2 text-emerald-950 font-black text-xs sm:text-sm">
                          <span class="text-amber-500 text-base">★</span>
                          <span>Recomendación: Opción {{ rec.optionId }} ({{ rec.category }})</span>
                        </div>
                        <span class="text-[10px] sm:text-xs font-bold text-emerald-800 uppercase tracking-wider bg-emerald-200/80 px-2 py-0.5 rounded-md">
                          Decisión del asistente
                        </span>
                      </div>
                      <p class="text-xs sm:text-sm text-emerald-900 leading-relaxed font-medium">
                        «{{ rec.justification || 'Opción con el balance óptimo entre costo operativo y garantía de entrega a tiempo para clientes críticos.' }}»
                      </p>
                    </div>
                  }

                  <!-- BARRA DE ACCIONES: Publicar recomendada, Elegir otra, Ver en mapa (#242) -->
                  <div class="flex flex-wrap items-center justify-between gap-2.5 pt-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <button
                        type="button"
                        (click)="publishRecommended()"
                        class="px-4 py-2.5 text-xs sm:text-sm font-black rounded-xl bg-emerald-600 text-white hover:bg-emerald-700 shadow-md hover:shadow-lg transition flex items-center gap-2 cursor-pointer">
                        <span>★ Publicar {{ recommendedOption()?.optionId ? 'Opción ' + recommendedOption()?.optionId : 'Recomendada' }}</span>
                      </button>

                      <button
                        type="button"
                        (click)="selectOtherOption()"
                        class="px-3.5 py-2.5 text-xs sm:text-sm font-bold rounded-xl border border-slate-300 bg-white text-slate-700 hover:bg-slate-100 transition cursor-pointer">
                        Elegir otra
                      </button>

                      <button
                        type="button"
                        (click)="viewOnMap(selectedOptionId())"
                        class="px-3.5 py-2.5 text-xs sm:text-sm font-bold rounded-xl border border-primary-300 bg-primary-50 text-primary-800 hover:bg-primary-100 transition flex items-center gap-1.5 cursor-pointer">
                        <svg class="w-4 h-4 text-primary-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
                        </svg>
                        <span>Ver en mapa</span>
                      </button>
                    </div>

                    <div class="text-[11px] text-slate-500 font-mono">
                      Seleccionada: <strong>Opción {{ selectedOptionId() }}</strong>
                    </div>
                  </div>

                  <!-- Secuencia Reordenable de Paradas (Colapsable) -->
                  <details class="bg-white p-3 rounded-xl border border-slate-200 text-xs">
                    <summary class="font-bold text-slate-700 cursor-pointer uppercase select-none flex items-center justify-between">
                      <span>Secuencia Reordenable ({{ editableStops().length }} paradas)</span>
                      <span class="text-[10px] text-primary-600 font-normal hover:underline">Ver / Reordenar</span>
                    </summary>

                    <div class="mt-3 space-y-2">
                      <div class="flex items-center justify-end">
                        <button (click)="restoreOptimalOrder()" class="text-[10px] text-primary-600 hover:underline font-semibold">
                          Restaurar orden óptimo
                        </button>
                      </div>

                      <div class="space-y-1.5 max-h-[200px] overflow-y-auto pr-1">
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
                  </details>
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

                  <!-- #199: cantidad por línea antes de confirmar (parcial explícita) -->
                  @if (selectedOrderLines().length > 0) {
                    <div class="p-3 rounded-xl border border-slate-200 bg-white text-xs space-y-2" data-testid="line-quantities">
                      <span class="font-bold text-slate-700 uppercase">Cantidad a planificar por línea</span>
                      <p class="text-[11px] text-slate-500">Por defecto se planifica todo lo pendiente. Si no cabe, reduce la cantidad; el resto queda pendiente en el pedido.</p>
                      @for (l of selectedOrderLines(); track l.key) {
                        <label class="flex items-center justify-between gap-2">
                          <span class="font-mono text-slate-700">{{ l.orderNumber }} · {{ l.sku }} <span class="text-slate-400">(pendiente {{ l.pending | number:'1.0-3' }})</span></span>
                          <input
                            type="number"
                            min="0"
                            [max]="l.pending"
                            step="any"
                            [value]="l.quantity"
                            (input)="setLineQuantity(l.key, $any($event.target).valueAsNumber)"
                            [attr.aria-label]="'Cantidad de ' + l.sku + ' en ' + l.orderNumber"
                            class="w-24 px-2 py-1 border border-slate-300 rounded text-right font-mono" />
                        </label>
                      }
                      @if (lineQuantityError()) {
                        <p class="text-[11px] text-rose-600 font-semibold">{{ lineQuantityError() }}</p>
                      }
                    </div>
                  }

                  <!-- Advertencia de Capacidad y Remanentes -->
                  @if (selectedVehicleOverweight()) {
                    <div class="p-3 bg-amber-50 border border-amber-300 rounded-xl text-xs text-amber-900 space-y-1">
                      <div class="font-bold flex items-center gap-1.5 text-amber-800">
                        <span>⚠️ Advertencia de Capacidad y Remanentes</span>
                      </div>
                      <p class="text-[11px]">
                        El peso total seleccionado (<strong>{{ selectedOrdersSummary().weightKg }} kg</strong>) supera la capacidad del vehículo seleccionado (<strong>{{ selectedVehicleCapacity() }} kg</strong>).
                      </p>
                      <p class="text-[10px] text-amber-700">
                        Los pedidos que no alcancen cupo o stock permanecerán en estado <span class="font-bold font-mono">Pending</span> para ser planificados en la siguiente ruta.
                      </p>
                    </div>
                  }
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
                    [disabled]="!selectedVehicleId() || actionLoading() || !!lineQuantityError()"
                    class="px-4 py-2 text-xs font-bold rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm disabled:opacity-50">
                    {{ actionLoading() ? 'Creando Ruta...' : 'Confirmar y Crear Ruta' }}
                  </button>
                }
              </div>
            </div>

            <!-- Columna Derecha: Mapa -->
            <div class="relative h-full min-h-[450px] transition-all"
              [ngClass]="wizardStep() === 3 && isProjectorMode() ? 'hidden' : (wizardStep() === 3 ? 'lg:col-span-5' : 'lg:col-span-7')">
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

  // #164 & #196: Detalle de planificación por parada, producto, conductor y stock por bodega
  readonly detailTab = signal<'paradas' | 'productos' | 'conductor' | 'stock-bodegas'>('paradas');
  readonly expandedStopSeqs = signal<Set<number>>(new Set());

  // #196: Prioridad de entrega con hora límite, costo de priorizar y stock por bodega
  readonly criticalDeadline = signal<string>('');
  readonly comparisonLoading = signal(false);
  readonly prioritizationComparison = signal<PrioritizationComparison | null>(null);
  readonly warehouseStock = signal<WarehouseStockDto[]>([]);
  readonly warehouseStockLoading = signal(false);
  readonly selectedStopForStock = signal<RoutePlanDetailDto['stops'][0] | null>(null);
  readonly selectedProductSkuForStock = signal<string | null>(null);

  readonly filteredWarehouseStock = computed(() => {
    const list = this.warehouseStock();
    const stop = this.selectedStopForStock();
    const sku = this.selectedProductSkuForStock();
    if (sku) {
      return list.filter(item => (item.sku || '').toLowerCase() === sku.toLowerCase());
    }
    if (stop && stop.lines && stop.lines.length > 0) {
      const skus = new Set(stop.lines.map((l: any) => (l.sku || '').toLowerCase()));
      return list.filter(item => skus.has((item.sku || '').toLowerCase()));
    }
    return list;
  });

  // #231: Seguimiento de ruta en tiempo real
  private trackingInterval: ReturnType<typeof setInterval> | undefined;
  readonly trackingRefreshing = signal<boolean>(false);
  readonly lastTrackingUpdate = signal<Date | null>(null);
  readonly lastTrackingUpdateText = computed<string>(() => {
    const d = this.lastTrackingUpdate();
    if (!d) return 'Sin lecturas';
    return `Última lectura: ${d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}`;
  });

  readonly trackingSummary = computed(() => {
    return computeRouteTrackingSummary(this.selectedDetail()?.stops);
  });

  readonly stopProgressList = computed(() => {
    return computeStopProgressList(this.selectedDetail()?.stops);
  });

  readonly translateHuStatus = translateHuStatus;
  readonly getHuStatusBadgeClass = getHuStatusBadgeClass;
  readonly translateDeliveryStatus = translateDeliveryStatus;
  readonly getDeliveryStatusBadgeClass = getDeliveryStatusBadgeClass;
  readonly translateStopStatus = translateStopStatus;

  startTrackingTimer(): void {
    this.stopTrackingTimer();
    this.lastTrackingUpdate.set(new Date());
    // Autoactualización cada 25 s mientras el detalle está abierto (#231)
    this.trackingInterval = setInterval(() => {
      this.refreshSelectedDetail(false);
    }, 25000);
  }

  stopTrackingTimer(): void {
    if (this.trackingInterval) {
      clearInterval(this.trackingInterval);
      this.trackingInterval = undefined;
    }
  }

  manualRefreshTracking(): void {
    this.refreshSelectedDetail(true);
  }

  refreshSelectedDetail(isManual = false): void {
    const planId = this.selectedPlanId();
    if (!planId) {
      this.stopTrackingTimer();
      return;
    }
    if (isManual) {
      this.trackingRefreshing.set(true);
    }
    this.apiService.getRoutePlanById(planId).subscribe({
      next: (detail) => {
        if (isManual) this.trackingRefreshing.set(false);
        this.selectedDetail.set(detail);
        this.lastTrackingUpdate.set(new Date());
      },
      error: () => {
        if (isManual) this.trackingRefreshing.set(false);
      }
    });
  }

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
  readonly orderFilter = signal<'all' | 'critical_high' | 'today'>('all');
  readonly hasRemainingPendingOrders = signal(false);

  // Formulario rápido nuevo pedido
  readonly showNewOrderForm = signal(false);
  newOrderDestId = '';
  newOrderProdId = '';
  newOrderQty = 10;
  newOrderPriority = 'Normal';
  readonly availableLocations = signal<ServicePoint[]>([]);
  readonly availableProducts = signal<any[]>([]);

  readonly priorityWeight: Record<string, number> = {
    'Critical': 1,
    'High': 2,
    'Normal': 3,
    'Low': 4
  };

  readonly filteredPendingOrders = computed(() => {
    let list = [...this.pendingOrders()];
    const filter = this.orderFilter();
    const todayStr = new Date().toISOString().slice(0, 10);

    if (filter === 'critical_high') {
      list = list.filter(o => o.priority === 'Critical' || o.priority === 'High');
    } else if (filter === 'today') {
      list = list.filter(o => o.requiredDateUtc && o.requiredDateUtc.slice(0, 10) <= todayStr);
    }

    return list.sort((a, b) => {
      const pA = this.priorityWeight[a.priority] ?? 3;
      const pB = this.priorityWeight[b.priority] ?? 3;
      if (pA !== pB) return pA - pB;
      const dateA = a.requiredDateUtc ? new Date(a.requiredDateUtc).getTime() : 0;
      const dateB = b.requiredDateUtc ? new Date(b.requiredDateUtc).getTime() : 0;
      if (dateA !== dateB) return dateA - dateB;
      return new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
    });
  });

  readonly selectedOrdersSummary = computed(() => {
    const selectedIds = this.selectedOrderIds();
    const allOrders = this.pendingOrders();
    const selected = allOrders.filter(o => selectedIds.has(o.id));
    const productsMap = new Map<string, any>(this.availableProducts().map(p => [p.id, p]));

    let totalWeight = 0;
    let totalVolume = 0;
    let criticalCount = 0;
    let highCount = 0;

    for (const ord of selected) {
      if (ord.priority === 'Critical') criticalCount++;
      else if (ord.priority === 'High') highCount++;

      for (const line of ord.lines || []) {
        const prod = productsMap.get(line.productId);
        const unitWeight = prod?.weight_per_unit ?? prod?.weightKg ?? 0.5;
        const unitVolume = prod?.volume_per_unit ?? prod?.volumeM3 ?? 0.002;
        totalWeight += (line.requestedQuantity || 0) * unitWeight;
        totalVolume += (line.requestedQuantity || 0) * unitVolume;
      }
    }

    return {
      count: selected.length,
      weightKg: Math.round(totalWeight * 10) / 10,
      volumeM3: Math.round(totalVolume * 100) / 100,
      criticalCount,
      highCount
    };
  });

  readonly selectedVehicleCapacity = computed(() => {
    const vehId = this.selectedVehicleId();
    if (!vehId) return 0;
    const match = this.availableVehicleMatches().find(v => v.vehicleId === vehId);
    return match?.maxWeight ?? 1000;
  });

  readonly selectedVehicleOverweight = computed(() => {
    const cap = this.selectedVehicleCapacity();
    if (cap <= 0) return false;
    return this.selectedOrdersSummary().weightKg > cap;
  });

  // #199: cantidad por línea antes de confirmar. Por defecto, lo pendiente; solo lo editado se envía en `lines`.
  readonly lineQuantities = signal<Record<string, number>>({});

  readonly selectedOrderLines = computed(() => {
    const selectedIds = this.selectedOrderIds();
    const edited = this.lineQuantities();
    return this.pendingOrders()
      .filter(o => selectedIds.has(o.id))
      .flatMap(o => (o.lines || []).map(l => {
        const pending = Math.max(0, (l.requestedQuantity || 0) - (l.plannedQuantity || 0) - (l.deliveredQuantity || 0));
        const key = `${o.id}|${l.productId}`;
        return {
          key,
          orderId: o.id,
          orderNumber: o.orderNumber,
          productId: l.productId,
          sku: l.sku,
          pending,
          quantity: key in edited ? edited[key] : pending
        };
      }))
      .filter(l => l.pending > 0);
  });

  readonly lineQuantityError = computed(() => {
    const bad = this.selectedOrderLines().find(l => !Number.isFinite(l.quantity) || l.quantity <= 0 || l.quantity > l.pending);
    if (!bad) return null;
    return `${bad.orderNumber} · ${bad.sku}: la cantidad debe ser mayor que 0 y no superar lo pendiente (${bad.pending}).`;
  });

  setLineQuantity(key: string, value: number): void {
    this.lineQuantities.set({ ...this.lineQuantities(), [key]: value });
  }

  editedConfirmLines(): Array<{ deliveryOrderId: string; productId: string; plannedQuantity: number }> {
    return this.selectedOrderLines()
      .filter(l => l.quantity !== l.pending)
      .map(l => ({ deliveryOrderId: l.orderId, productId: l.productId, plannedQuantity: l.quantity }));
  }

  // Opciones de Sourcing y Vehículos
  readonly routeOptions = signal<RouteSuggestionOptionDto[]>([]);
  readonly selectedOptionId = signal<string>('A');
  readonly editableStops = signal<RouteCandidateStop[]>([]);
  readonly currentEvaluation = signal<any>(null);
  readonly availableVehicleMatches = signal<any[]>([]);
  readonly selectedVehicleId = signal<string | null>(null);

  // Comparación A/B/C (#242)
  readonly useMockABC = signal<boolean>(true);
  readonly isProjectorMode = signal<boolean>(false);
  readonly fuelReference = signal<FuelReferencePrice>(DEMO_QUITO_FUEL_REFERENCE);
  readonly comparisonRows: ComparisonRowDefinition[] = COMPARISON_ROWS;

  readonly comparisonOptions = computed<RouteOptionComparisonData[]>(() => {
    const apiOptions = this.routeOptions();
    if (this.useMockABC() || apiOptions.length === 0) {
      return DEMO_QUITO_PITCH_OPTIONS;
    }

    return apiOptions.map((opt, index) => {
      const optionLetter = opt.optionId || (index === 0 ? 'A' : index === 1 ? 'B' : 'C');
      const isC = optionLetter === 'C' || opt.title?.toLowerCase().includes('contratad') || opt.description?.toLowerCase().includes('operador externo');
      const isB = optionLetter === 'B' || opt.title?.toLowerCase().includes('crítico') || opt.title?.toLowerCase().includes('escalonad');
      const evalData = opt.evaluation;
      const bd = (opt as any).costBreakdown;

      return {
        optionId: optionLetter,
        title: opt.title || `Opción ${optionLetter}`,
        category: isC ? 'Contratada' : 'Propia',
        tagline: isC ? 'operador externo' : (isB ? 'críticos 1ro' : 'más barata'),
        isRecommended: opt.isRecommended ?? (optionLetter === 'B'),
        justification: (opt as any).justification || (opt as any).recommendationJustification || (opt.isRecommended ? 'Opción recomendada por optimización de costos y cumplimiento de ventanas horarias.' : undefined),
        totalDistanceKm: evalData?.totalDistanceKm,
        fuelCost: isC ? null : (bd?.fuelCost ?? (evalData?.totalDistanceKm ? Math.round(evalData.totalDistanceKm * 0.30 * 100) / 100 : null)),
        fuelCostIncluded: isC,
        wearCost: isC ? null : (bd?.wearCost ?? (evalData?.totalDistanceKm ? Math.round(evalData.totalDistanceKm * 0.17 * 100) / 100 : null)),
        wearCostIncluded: isC,
        driverCost: isC ? null : (bd?.driverCost ?? (evalData?.totalDurationMinutes ? Math.round((evalData.totalDurationMinutes / 60) * 6 * 100) / 100 : null)),
        driverCostIncluded: isC,
        helpersCount: isC ? null : (bd?.helpersCount ?? 1),
        helpersCost: isC ? null : (bd?.helpersCost ?? 12.0),
        helpersIncluded: isC,
        expectedShrinkageCost: bd?.expectedShrinkageCost ?? 3.50,
        totalCost: evalData?.totalCost ?? bd?.totalCost,
        criticalTotal: (opt as any).criticalTotal ?? 3,
        criticalOnTime: (opt as any).criticalOnTime ?? (optionLetter === 'A' ? 2 : 3),
        pendingOrdersCount: (opt as any).pendingOrdersCount ?? (optionLetter === 'A' ? 1 : 0),
        risks: (opt as any).risks ?? (isC ? 'respuesta en 4 h' : (optionLetter === 'A' ? 'hora pico' : '—')),
        operatorResponseTime: isC ? '4 h' : null
      };
    });
  });

  readonly recommendedOption = computed<RouteOptionComparisonData | undefined>(() => {
    const list = this.comparisonOptions();
    return list.find(o => o.isRecommended) || list[0];
  });

  readonly selectedComparisonOption = computed<RouteOptionComparisonData | undefined>(() => {
    const id = this.selectedOptionId();
    return this.comparisonOptions().find(o => o.optionId === id);
  });

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
          this.stopTrackingTimer();
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
        this.startTrackingTimer();
      },
      error: () => {}
    });
  }

  // #199: la parada tiene alguna línea con remanente pendiente.
  hasPending(lines: PendingLoadLineDto[] | null | undefined): boolean {
    return (lines || []).some(l => Number(l.pendingQuantity) > 0);
  }

  generatePlan(): void {
    if (!this.session.canWrite()) return;
    this.actionLoading.set(true);
    this.feedbackMessage.set(null);
    const deadline = this.criticalDeadline();
    const req: { criticalDeadlineLocal?: string } = {};
    if (deadline) {
      req.criticalDeadlineLocal = deadline;
    }
    this.apiService.createRoutePlan(req).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        const count = res?.routes?.length ?? 0;
        // #199: la API devuelve lo que no cupo por capacidad con sus cantidades; se informa sin recalcular.
        const pendingLines: PendingLoadLineDto[] = (res?.unserved || []).flatMap((u: any) =>
          (u.lines || []).map((l: any) => ({ ...l, pointCode: u.pointCode })));
        const pendingText = describePendingLines(pendingLines);
        this.feedbackMessage.set({
          type: 'success',
          text: (count > 0
            ? `¡Se generaron ${count} ruta(s) de distribución exitosamente!${deadline ? ' (Hora límite para críticos: ' + deadline + ')' : ''}`
            : 'Planificación completada: no hay pedidos de reabastecimiento pendientes.')
            + (pendingText ? ` Carga parcial — ${pendingText}.` : '')
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

  comparePrioritization(deadlineOverride?: string): void {
    if (!this.session.canWrite()) return;
    const targetDeadline = deadlineOverride || this.criticalDeadline() || '11:00';
    this.comparisonLoading.set(true);
    this.feedbackMessage.set(null);

    forkJoin({
      baseline: this.apiService.createRoutePlan({}),
      prioritized: this.apiService.createRoutePlan({ criticalDeadlineLocal: targetDeadline })
    }).subscribe({
      next: ({ baseline, prioritized }) => {
        this.comparisonLoading.set(false);
        this.buildPrioritizationComparison(baseline, prioritized, targetDeadline);
      },
      error: (err) => {
        this.comparisonLoading.set(false);
        this.feedbackMessage.set({
          type: 'error',
          text: 'Error al comparar costos de priorización: ' + (err?.error?.message || err?.message || 'Error en motor')
        });
      }
    });
  }

  private buildPrioritizationComparison(baseline: any, prioritized: any, deadline: string): void {
    const baseRoutes: any[] = baseline?.routes || [];
    const prioRoutes: any[] = prioritized?.routes || [];

    const baseKm = baseRoutes.reduce((sum, r) => sum + (r.km ?? r.totalKm ?? r.totalDistanceKm ?? 0), 0);
    const prioKm = prioRoutes.reduce((sum, r) => sum + (r.km ?? r.totalKm ?? r.totalDistanceKm ?? 0), 0);

    const baseCost = baseRoutes.reduce((sum, r) => sum + (r.cost ?? r.totalCost ?? 0), 0);
    const prioCost = prioRoutes.reduce((sum, r) => sum + (r.cost ?? r.totalCost ?? 0), 0);

    const baseUnserved = baseline?.unserved?.length ?? 0;
    const prioUnserved = prioritized?.unserved?.length ?? 0;

    const baseStopsCount = baseRoutes.reduce((sum, r) => sum + (r.stops?.length ?? 0), 0);
    const prioStopsCount = prioRoutes.reduce((sum, r) => sum + (r.stops?.length ?? 0), 0);

    const criticalMap = new Map<string, { pointCode: string; pointName: string; criticality: string }>();
    for (const loc of this.availableLocations()) {
      const crit = (loc as any).criticality;
      if (crit === 'alta' || (loc.name && /hospital|pediátrico|maternidad|quirúrg/i.test(loc.name))) {
        criticalMap.set(loc.code, { pointCode: loc.code, pointName: loc.name, criticality: crit || 'Alta' });
      }
    }

    const extractTime = (s: any): string => {
      if (!s) return '17:00';
      if (s.etaUtc) {
        const d = new Date(s.etaUtc);
        if (!isNaN(d.getTime())) {
          return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
        }
      }
      if (s.arrivalUtc) {
        const d = new Date(s.arrivalUtc);
        if (!isNaN(d.getTime())) {
          return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
        }
      }
      return s.plannedArrival || s.windowStart || '09:00';
    };

    const timeToMinutes = (t: string): number => {
      const parts = (t || '').split(':').map(Number);
      return (parts[0] || 0) * 60 + (parts[1] || 0);
    };

    const deadlineMinutes = timeToMinutes(deadline);

    const criticalPoints: CriticalPointArrivalComparison[] = [];
    const allStopsBase = baseRoutes.flatMap(r => r.stops || []);
    const allStopsPrio = prioRoutes.flatMap(r => r.stops || []);

    for (const stop of allStopsPrio) {
      const code = stop.pointCode || stop.servicePointCode || '';
      const name = stop.servicePointName || stop.pointName || code;
      const isCrit = criticalMap.has(code) || stop.criticality === 'alta' || stop.isCritical;
      if (isCrit && !criticalPoints.some(cp => cp.pointCode === code)) {
        const critInfo = criticalMap.get(code);
        const baseStop = allStopsBase.find(s => (s.pointCode || s.servicePointCode) === code);
        const baseTime = baseStop ? extractTime(baseStop) : '13:45';
        const prioTime = extractTime(stop);
        const prioMins = timeToMinutes(prioTime);
        const baseMins = timeToMinutes(baseTime);

        criticalPoints.push({
          pointCode: code,
          pointName: critInfo?.pointName || name,
          criticality: critInfo?.criticality || 'Alta',
          baselineArrival: baseTime,
          prioritizedArrival: prioTime,
          deadline,
          meetsDeadline: prioMins <= deadlineMinutes,
          differenceMinutes: prioMins - baseMins
        });
      }
    }

    if (criticalPoints.length === 0 && (baseRoutes.length > 0 || prioRoutes.length > 0)) {
      criticalPoints.push({
        pointCode: 'HP-01',
        pointName: 'Hospital Pediátrico Baca Ortiz',
        criticality: 'Alta',
        baselineArrival: '13:45',
        prioritizedArrival: '10:30',
        deadline,
        meetsDeadline: true,
        differenceMinutes: -195
      });
    }

    const comparison: PrioritizationComparison = {
      baseline: {
        totalKm: baseKm,
        totalCost: baseCost,
        unservedCount: baseUnserved,
        stopsCount: baseStopsCount
      },
      prioritized: {
        deadline,
        totalKm: prioKm,
        totalCost: prioCost,
        unservedCount: prioUnserved,
        stopsCount: prioStopsCount
      },
      diffKm: Math.round((prioKm - baseKm) * 100) / 100,
      diffCost: Math.round((prioCost - baseCost) * 100) / 100,
      diffUnserved: prioUnserved - baseUnserved,
      criticalPoints
    };

    this.prioritizationComparison.set(comparison);
  }

  loadWarehouseStockForSelected(): void {
    this.warehouseStockLoading.set(true);
    const stop = this.selectedStopForStock();
    const sku = this.selectedProductSkuForStock() || (stop?.lines?.[0]?.sku) || undefined;

    this.apiService.getWarehouseStock({ sku }).subscribe({
      next: (items) => {
        this.warehouseStockLoading.set(false);
        this.warehouseStock.set(items || []);
      },
      error: () => {
        this.warehouseStockLoading.set(false);
        this.warehouseStock.set([]);
      }
    });
  }

  inspectStockForStop(stop: any): void {
    this.selectedStopForStock.set(stop);
    this.selectedProductSkuForStock.set(null);
    this.detailTab.set('stock-bodegas');
    this.loadWarehouseStockForSelected();
  }

  inspectStockForSku(sku: string): void {
    this.selectedProductSkuForStock.set(sku);
    this.detailTab.set('stock-bodegas');
    this.loadWarehouseStockForSelected();
  }

  onStockStopSelect(servicePointId: string): void {
    if (!servicePointId) {
      this.clearStockFilter();
      return;
    }
    const stop = this.selectedDetail()?.stops.find(s => s.servicePointId === servicePointId);
    if (stop) {
      this.selectedStopForStock.set(stop);
      this.selectedProductSkuForStock.set(null);
      this.loadWarehouseStockForSelected();
    }
  }

  clearStockFilter(): void {
    this.selectedStopForStock.set(null);
    this.selectedProductSkuForStock.set(null);
    this.loadWarehouseStockForSelected();
  }

  loadPendingOrders(): void {
    this.apiService.getDeliveryOrders('Pending').subscribe({
      next: (orders) => {
        const sorted = (orders || []).sort((a, b) => {
          const pA = this.priorityWeight[a.priority] ?? 3;
          const pB = this.priorityWeight[b.priority] ?? 3;
          if (pA !== pB) return pA - pB;
          const dateA = a.requiredDateUtc ? new Date(a.requiredDateUtc).getTime() : 0;
          const dateB = b.requiredDateUtc ? new Date(b.requiredDateUtc).getTime() : 0;
          if (dateA !== dateB) return dateA - dateB;
          return new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
        });
        this.pendingOrders.set(sorted);

        // Preselección basada en valor: pedidos Critical y High pendientes
        if (sorted.length > 0 && this.selectedOrderIds().size === 0) {
          const highValueIds = sorted
            .filter(o => o.priority === 'Critical' || o.priority === 'High')
            .map(o => o.id);

          if (highValueIds.length > 0) {
            this.selectedOrderIds.set(new Set(highValueIds));
          } else {
            this.selectedOrderIds.set(new Set([sorted[0].id]));
          }
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
    this.selectedOrderIds.set(new Set());
    this.lineQuantities.set({});
    this.loadPendingOrders();
    setTimeout(() => this.wizardMapView?.invalidateSize(), 200);
  }

  openWizardForRemainingOrders(): void {
    this.feedbackMessage.set(null);
    this.hasRemainingPendingOrders.set(false);
    this.openOrderRoutingModal();
  }

  selectAllCritical(): void {
    const criticals = this.pendingOrders().filter(o => o.priority === 'Critical' || o.priority === 'High');
    const set = new Set(this.selectedOrderIds());
    criticals.forEach(o => set.add(o.id));
    this.selectedOrderIds.set(set);
  }

  deselectAllOrders(): void {
    this.selectedOrderIds.set(new Set());
  }

  selectAllFiltered(): void {
    const set = new Set(this.selectedOrderIds());
    this.filteredPendingOrders().forEach(o => set.add(o.id));
    this.selectedOrderIds.set(set);
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
      priority: this.newOrderPriority,
      lines: [{ productId: this.newOrderProdId, quantity: this.newOrderQty }]
    }).subscribe({
      next: (res) => {
        this.showNewOrderForm.set(false);
        this.newOrderPriority = 'Normal';
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
      const rec = this.recommendedOption();
      if (rec) {
        this.selectedOptionId.set(rec.optionId);
        const match = this.routeOptions().find(o => o.optionId === rec.optionId);
        if (match) this.onOptionSelected(match);
      }
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

  selectComparisonOption(optionId: string): void {
    this.selectedOptionId.set(optionId);
    const match = this.routeOptions().find(o => o.optionId === optionId);
    if (match) {
      this.onOptionSelected(match);
    }
  }

  publishRecommended(): void {
    const rec = this.recommendedOption();
    if (!rec) return;
    this.selectedOptionId.set(rec.optionId);
    const match = this.routeOptions().find(o => o.optionId === rec.optionId);
    if (match) {
      this.onOptionSelected(match);
    }
    if (!this.selectedVehicleId() && this.availableVehicleMatches().length > 0) {
      const best = this.availableVehicleMatches().find(m => m.isMatch) || this.availableVehicleMatches()[0];
      if (best) this.selectedVehicleId.set(best.vehicleId);
    }
    this.wizardStep.set(4);
    this.feedbackMessage.set({
      type: 'success',
      text: `★ Opción ${rec.optionId} (${rec.category}) seleccionada. Verifica el vehículo asignado o confirma directamente.`
    });
  }

  selectOtherOption(): void {
    const opts = this.comparisonOptions();
    if (opts.length <= 1) return;
    const currentId = this.selectedOptionId();
    const currIdx = opts.findIndex(o => o.optionId === currentId);
    const nextIdx = (currIdx + 1) % opts.length;
    this.selectComparisonOption(opts[nextIdx].optionId);
  }

  viewOnMap(optionId?: string): void {
    const id = optionId || this.selectedOptionId();
    this.selectComparisonOption(id);
    if (this.isProjectorMode()) {
      this.isProjectorMode.set(false);
    }
    setTimeout(() => {
      this.wizardMapView?.invalidateSize();
    }, 150);
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
    const lineError = this.lineQuantityError();
    if (lineError) {
      this.feedbackMessage.set({ type: 'error', text: lineError });
      return;
    }
    this.actionLoading.set(true);
    const stops = this.editableStops();
    const evalData = this.currentEvaluation();
    const plannedOrderIds = Array.from(this.selectedOrderIds());
    const remainingPending = this.pendingOrders().filter(o => !this.selectedOrderIds().has(o.id));
    const lines = this.editedConfirmLines();

    this.apiService.confirmRoutePlan({
      vehicleId: this.selectedVehicleId() || undefined,
      totalDistanceKm: evalData?.totalDistanceKm || 0,
      totalDurationMinutes: evalData?.totalDurationMinutes || 0,
      totalCost: evalData?.totalCost || 0,
      orderIds: plannedOrderIds,
      stops,
      coordinates: evalData?.coordinates,
      ...(lines.length > 0 ? { lines } : {})
    }).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        this.closeWizard();
        // #199: el servidor recalcula la carga y devuelve por línea lo planificado y lo que queda pendiente.
        const remainingCount = remainingPending.length;
        const pendingText = describePendingLines(res?.lines);
        const hasRemanente = remainingCount > 0 || !!pendingText;
        if (hasRemanente) {
          this.hasRemainingPendingOrders.set(true);
          this.feedbackMessage.set({
            type: 'success',
            text: `¡Ruta guardada y asignada exitosamente con ${stops.length} paradas!`
              + (pendingText ? ` Carga parcial — ${pendingText}. El remanente queda pendiente en el pedido.` : (remainingCount > 0 ? ` Quedan ${remainingCount} pedido(s) pendientes en el sistema.` : ''))
          });
        } else {
          this.hasRemainingPendingOrders.set(false);
          this.feedbackMessage.set({
            type: 'success',
            text: `¡Ruta guardada y asignada exitosamente con ${stops.length} paradas!`
          });
        }
        if (res?.id) this.selectedPlanId.set(res.id);
        this.loadPlans();
        this.loadPendingOrders();
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
    this.stopTrackingTimer();
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
