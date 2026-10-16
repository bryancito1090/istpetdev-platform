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
            <h1 class="text-[22px] font-semibold text-[#1A2C44] tracking-tight">
              Planificación de Rutas y Distribución
            </h1>
            @if (isAuditor()) {
              <span class="text-[11px] font-medium px-2 py-0.5 rounded-md bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]">
                Auditoría (Solo Lectura)
              </span>
            }
          </div>
          <p class="text-[13px] text-[#3C4F69] mt-0.5">
            @if (isAuditor()) {
              Auditoría y fiscalización de itinerarios: verificación de ventanas de atención, tiempos estimados de llegada y kilometraje de la red.
            } @else {
              Genera rutas de despacho hospitalario con abastecimiento automático desde los almacenes más cercanos y asignación de vehículos de transporte.
            }
          </p>
        </div>

        @if (session.canWrite()) {
          <div class="flex items-center gap-2">
            <!-- Botón Asistente IA de Rutas -->
            <button
              type="button"
              (click)="triggerAgentWatch()"
              [disabled]="agentLoading() || actionLoading()"
              class="inline-flex items-center gap-1.5 text-[13px] font-medium px-3.5 py-1.5 rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs disabled:opacity-50">
              @if (agentLoading()) {
                <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-[#635BFF] border-t-transparent rounded-full"></span>
                <span>Consultando asistente…</span>
              } @else {
                <span>Consultar Asistente de Rutas</span>
              }
            </button>

            <!-- Botón de Ruta Urgente -->
            <button
              type="button"
              (click)="triggerEmergencyFlow()"
              [disabled]="agentLoading() || actionLoading()"
              class="inline-flex items-center text-[13px] font-medium px-3.5 py-1.5 rounded-md bg-rose-50 border border-rose-200 text-rose-700 hover:bg-rose-100 transition-colors shadow-2xs disabled:opacity-50">
              <span>Proponer Ruta de Urgencia</span>
            </button>

            <!-- Botón Nueva Ruta por Pedidos -->
            <button
              type="button"
              (click)="openOrderRoutingModal()"
              [disabled]="agentLoading() || actionLoading()"
              class="inline-flex items-center text-[13px] font-medium px-4 py-1.5 rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-2xs disabled:opacity-50">
              <span>+ Nueva Ruta por Pedidos</span>
            </button>
          </div>
        }
      </div>

      <!-- Selector de Pestañas: Rutas / Bandeja de Rechazos -->
      <div class="flex items-center gap-6 border-b border-[#E3E8EE]">
        <button
          type="button"
          (click)="activeTab.set('rutas')"
          class="pb-3 text-[13px] font-medium border-b-2 transition-colors flex items-center gap-2"
          [ngClass]="activeTab() === 'rutas' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#697386] hover:text-[#30313D]'">
          <span>Rutas Planificadas</span>
          <span class="text-[11px] px-1.5 py-0.2 rounded bg-[#F8FAFC] border border-[#E3E8EE] text-[#697386] font-mono">{{ routePlans().length }}</span>
        </button>

        <button
          type="button"
          (click)="activeTab.set('rechazos'); loadRejectedCaptures()"
          class="pb-3 text-[13px] font-medium border-b-2 transition-colors flex items-center gap-2"
          [ngClass]="activeTab() === 'rechazos' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#697386] hover:text-[#30313D]'">
          <span>Entregas con Novedad</span>
          @if (rejectedCaptures().length > 0) {
            <span class="text-[11px] px-1.5 py-0.2 rounded bg-rose-50 border border-rose-200 text-rose-700 font-medium font-mono">{{ rejectedCaptures().length }}</span>
          }
        </button>
      </div>

      @if (activeTab() === 'rutas') {
        <!-- Banner de retroalimentación -->
        @if (feedbackMessage()) {
        <div 
          class="p-3.5 rounded-lg text-[13px] font-medium flex items-center justify-between transition-all"
          [ngClass]="feedbackMessage()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-rose-50 text-rose-800 border border-rose-200'">
          <div class="flex items-center gap-2">
            <span class="font-semibold">[{{ feedbackMessage()!.type === 'success' ? 'Completado' : 'Aviso' }}]</span>
            <span>{{ feedbackMessage()!.text }}</span>
          </div>
          <button (click)="feedbackMessage.set(null)" class="text-[#697386] hover:text-[#1A2C44] font-medium ml-3 text-[12px]">Cerrar</button>
        </div>
      }

      <!-- Tarjeta del Asistente IA -->
      @if (agentRun()) {
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-[#F1F3F5] pb-3">
            <div class="flex items-center gap-2.5">
              <span class="px-2 py-1 bg-[#635BFF] text-white rounded-md text-[11px] font-semibold leading-none">Asistente</span>
              <div>
                <h3 class="text-[14px] font-semibold text-[#1A2C44]">Propuesta de Asignación Logística</h3>
                <p class="text-[12px] text-[#697386]">
                  Ejecución {{ agentRun()!.runId.substring(0, 8) }} &bull; Modelo: {{ agentRun()!.model || 'Estándar' }} &bull; Tiempo de respuesta: {{ agentRun()!.latencyMs }} ms
                </p>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span
                class="text-[11px] px-2 py-0.5 rounded-md font-medium border"
                [ngClass]="agentRun()!.status === 'Completed' ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-amber-50 text-amber-700 border-amber-200'">
                {{ agentRun()!.status === 'Completed' ? 'Completado' : agentRun()!.status === 'Fallback' ? 'Cálculo de respaldo' : agentRun()!.status === 'Running' ? 'En ejecución' : 'No completado' }}
              </span>
              <button (click)="agentRun.set(null)" class="text-[#8792A2] hover:text-[#1A2C44] text-[13px] ml-2">✕</button>
            </div>
          </div>

          <!-- Decisiones generadas por el Agente -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            @for (decision of agentRun()!.decisions; track decision.agentDecisionId) {
              <div class="bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg p-3.5 space-y-2 text-[13px]">
                <div class="flex items-center justify-between">
                  <span class="font-semibold text-[#1A2C44]">Vehículo: {{ decision.vehicleCode }}</span>
                  <span class="text-[11px] font-medium text-[#697386] bg-white px-2 py-0.5 rounded border border-[#E3E8EE]">
                    Fuente: {{ decision.source === 'Llm' ? 'Asistente IA' : 'Regla de negocio' }}
                  </span>
                </div>
                <p class="text-[#3C4F69] leading-relaxed">{{ decision.explanation }}</p>
                @if (decision.driverMessage) {
                  <div class="bg-white border border-[#E3E8EE] rounded-md p-2 text-[12px] text-[#30313D]">
                    <span class="font-medium text-[#1A2C44]">Instrucción al conductor:</span> "{{ decision.driverMessage }}"
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }

      <!-- Panel de Planificación y Prioridad de Entrega -->
      @if (session.canWrite()) {
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-4 shadow-2xs space-y-3" data-testid="planning-control-panel">
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-3">
            <div class="space-y-0.5">
              <div class="flex items-center gap-2">
                <h3 class="text-[14px] font-semibold text-[#1A2C44]">
                  Planificación Automática y Prioridad de Entrega
                </h3>
                <span class="text-[11px] font-medium px-2 py-0.5 rounded bg-[#F8FAFC] text-[#30313D] border border-[#E3E8EE]">
                  Optimización de Rutas
                </span>
              </div>
              <p class="text-[13px] text-[#3C4F69]">
                Calcula la ruta más eficiente desde los almacenes. Puedes fijar una hora límite para que los hospitales o clínicas con mayor prioridad reciban antes de ese horario.
              </p>
            </div>

            <!-- Selector de Hora Límite y Botones -->
            <div class="flex flex-wrap items-center gap-2">
              <div class="flex items-center gap-1.5 bg-[#F8FAFC] border border-[#E3E8EE] rounded-md px-2.5 py-1">
                <label for="critical-deadline-select" class="text-[12px] font-medium text-[#30313D] whitespace-nowrap">
                  Hora límite prioritaria:
                </label>
                <select
                  id="critical-deadline-select"
                  [ngModel]="criticalDeadline()"
                  (ngModelChange)="criticalDeadline.set($event)"
                  data-testid="critical-deadline-select"
                  class="text-[12px] font-medium bg-white border border-[#E3E8EE] rounded px-2 py-1 text-[#1A2C44] focus:outline-none focus:border-[#635BFF]">
                  <option value="">Sin hora límite (estándar)</option>
                  <option value="09:00">09:00</option>
                  <option value="10:00">10:00</option>
                  <option value="11:00">11:00 (Prioridad Hospitalaria)</option>
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
                class="inline-flex items-center gap-1 text-[13px] font-medium px-3.5 py-1.5 rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-2xs disabled:opacity-50">
                @if (actionLoading()) {
                  <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-white border-t-transparent rounded-full"></span>
                  <span>Calculando...</span>
                } @else {
                  <span>Calcular Plan de Rutas</span>
                }
              </button>

              <button
                type="button"
                (click)="comparePrioritization()"
                [disabled]="actionLoading() || comparisonLoading()"
                data-testid="compare-cost-button"
                class="inline-flex items-center gap-1 text-[13px] font-medium px-3.5 py-1.5 rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs disabled:opacity-50">
                @if (comparisonLoading()) {
                  <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-[#635BFF] border-t-transparent rounded-full"></span>
                  <span>Comparando...</span>
                } @else {
                  <span>Comparar Impacto de Prioridad</span>
                }
              </button>
            </div>
          </div>
        </div>
      }

      <!-- Tarjeta de Comparación: Costo de Priorizar -->
      @if (prioritizationComparison()) {
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs space-y-4" data-testid="prioritization-comparison-card">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-[#F1F3F5] pb-3">
            <div>
              <div class="flex items-center gap-2">
                <span class="text-[15px] font-semibold text-[#1A2C44]">Análisis Comparativo: Impacto de Priorizar Entregas Críticas</span>
                <span class="text-[11px] px-2 py-0.5 rounded-md font-medium bg-[#F8FAFC] text-[#30313D] border border-[#E3E8EE]">
                  Hora Límite: {{ prioritizationComparison()!.prioritized.deadline }}
                </span>
              </div>
              <p class="text-[13px] text-[#3C4F69] mt-0.5">
                Comparativa de viabilidad económica y cumplimiento horario: Plan Estándar (por costo) vs. Plan Priorizado (con ventana crítica).
              </p>
            </div>

            <button
              type="button"
              (click)="prioritizationComparison.set(null)"
              class="text-[12px] font-medium text-[#697386] hover:text-[#1A2C44] px-2 py-1 rounded hover:bg-[#F8FAFC]">
              ✕ Cerrar
            </button>
          </div>

          <!-- Grilla de Métricas Comparativas -->
          <div class="grid grid-cols-1 sm:grid-cols-4 gap-4">
            <!-- Kilómetros -->
            <div class="p-3.5 bg-[#F8FAFC] rounded-lg border border-[#E3E8EE]">
              <span class="text-[11px] font-semibold text-[#697386] uppercase">Recorrido Total</span>
              <div class="flex items-baseline justify-between mt-1">
                <span class="text-[18px] font-bold text-[#1A2C44] font-mono">
                  {{ prioritizationComparison()!.prioritized.totalKm | number:'1.1-2' }} km
                </span>
                <span class="text-[12px] font-semibold font-mono"
                  [ngClass]="prioritizationComparison()!.diffKm > 0 ? 'text-amber-600' : 'text-emerald-600'">
                  {{ prioritizationComparison()!.diffKm >= 0 ? '+' : '' }}{{ prioritizationComparison()!.diffKm | number:'1.1-2' }} km
                </span>
              </div>
              <span class="text-[11px] text-[#8792A2]">Sin hora límite: {{ prioritizationComparison()!.baseline.totalKm | number:'1.1-2' }} km</span>
            </div>

            <!-- Costo USD -->
            <div class="p-3.5 bg-[#F8FAFC] rounded-lg border border-[#E3E8EE]">
              <span class="text-[11px] font-semibold text-[#697386] uppercase">Costo Estimado</span>
              <div class="flex items-baseline justify-between mt-1">
                <span class="text-[18px] font-bold text-[#1A2C44] font-mono">
                  USD {{ prioritizationComparison()!.prioritized.totalCost | number:'1.2-2' }}
                </span>
                <span class="text-[12px] font-semibold font-mono"
                  [ngClass]="prioritizationComparison()!.diffCost > 0 ? 'text-rose-600' : 'text-emerald-600'">
                  {{ prioritizationComparison()!.diffCost >= 0 ? '+' : '' }}USD {{ prioritizationComparison()!.diffCost | number:'1.2-2' }}
                </span>
              </div>
              <span class="text-[11px] text-[#8792A2]">Sin hora límite: USD {{ prioritizationComparison()!.baseline.totalCost | number:'1.2-2' }}</span>
            </div>

            <!-- Paradas No Atendidas -->
            <div class="p-3.5 bg-[#F8FAFC] rounded-lg border border-[#E3E8EE]">
              <span class="text-[11px] font-semibold text-[#697386] uppercase">Paradas No Asignadas</span>
              <div class="flex items-baseline justify-between mt-1">
                <span class="text-[18px] font-bold font-mono"
                  [ngClass]="prioritizationComparison()!.prioritized.unservedCount > 0 ? 'text-amber-600' : 'text-emerald-600'">
                  {{ prioritizationComparison()!.prioritized.unservedCount }}
                </span>
                <span class="text-[12px] font-medium font-mono text-[#697386]">
                  Base: {{ prioritizationComparison()!.baseline.unservedCount }}
                </span>
              </div>
              <span class="text-[11px] text-[#8792A2]">
                {{ prioritizationComparison()!.diffUnserved > 0 ? 'Afectadas por ventana' : '100% de cobertura' }}
              </span>
            </div>

            <!-- Resumen Decisión -->
            <div class="p-3.5 bg-white rounded-lg border border-[#E3E8EE] flex flex-col justify-between">
              <span class="text-[11px] font-semibold text-[#697386] uppercase">Diferencial de Costo</span>
              <p class="text-[16px] font-bold text-[#1A2C44] mt-1">
                {{ prioritizationComparison()!.diffCost >= 0 ? '+' : '' }}USD {{ prioritizationComparison()!.diffCost | number:'1.2-2' }}
              </p>
              <span class="text-[11px] text-[#3C4F69]">
                {{ prioritizationComparison()!.criticalPoints.length }} punto(s) prioritario(s) adelantados
              </span>
            </div>
          </div>

          <!-- Tabla de Hora de Llegada de los Críticos -->
          <div class="space-y-2">
            <span class="text-[12px] font-semibold text-[#1A2C44] uppercase tracking-wider">
              Llegada Estimada a Centros Prioritarios (Hospitales / Quirófanos)
            </span>

            @if (prioritizationComparison()!.criticalPoints.length === 0) {
              <p class="text-[12px] text-[#697386] italic p-3 bg-[#F8FAFC] rounded border border-[#E3E8EE]">
                No se detectaron paradas con criticidad alta en las órdenes del plan actual.
              </p>
            } @else {
              <div class="overflow-x-auto border border-[#E3E8EE] rounded-lg shadow-2xs bg-white">
                <table class="w-full text-xs text-left" data-testid="critical-points-table">
                  <thead class="bg-[#F8FAFC] border-b border-[#E3E8EE] text-[#697386] uppercase font-semibold text-[11px]">
                    <tr>
                      <th class="p-2.5">Centro de Salud</th>
                      <th class="p-2.5">Prioridad</th>
                      <th class="p-2.5">Sin Hora Límite</th>
                      <th class="p-2.5">Con Hora Límite ({{ prioritizationComparison()!.prioritized.deadline }})</th>
                      <th class="p-2.5">Diferencia</th>
                      <th class="p-2.5">Cumplimiento</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-[#F1F3F5] font-mono text-[#30313D]">
                    @for (pt of prioritizationComparison()!.criticalPoints; track pt.pointCode) {
                      <tr class="hover:bg-[#F8FAFC]/50">
                        <td class="p-2.5 font-sans font-medium text-[#1A2C44]">
                          {{ pt.pointName }} <span class="text-[11px] text-[#8792A2] font-mono">({{ pt.pointCode }})</span>
                        </td>
                        <td class="p-2.5 font-sans">
                          <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-rose-50 text-rose-700 border border-rose-200">
                            {{ pt.criticality }}
                          </span>
                        </td>
                        <td class="p-2.5 text-[#697386]">{{ pt.baselineArrival }}</td>
                        <td class="p-2.5 font-bold text-[#635BFF]">{{ pt.prioritizedArrival }}</td>
                        <td class="p-2.5 font-semibold"
                          [ngClass]="pt.differenceMinutes < 0 ? 'text-emerald-600' : 'text-[#697386]'">
                          {{ pt.differenceMinutes < 0 ? (pt.differenceMinutes * -1) + ' min antes' : (pt.differenceMinutes === 0 ? 'Sin cambio' : pt.differenceMinutes + ' min después') }}
                        </td>
                        <td class="p-2.5 font-sans">
                          @if (pt.meetsDeadline) {
                            <span class="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-700 bg-emerald-50 border border-emerald-200 px-2 py-0.5 rounded-full">
                              Llega antes de {{ pt.deadline }}
                            </span>
                          } @else {
                            <span class="inline-flex items-center gap-1 text-[11px] font-medium text-rose-700 bg-rose-50 border border-rose-200 px-2 py-0.5 rounded-full">
                              Excede horario
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
        <div class="p-8 text-center text-[#697386] text-[13px] bg-white border border-[#E3E8EE] rounded-lg">
          Cargando rutas de distribución...
        </div>
      }

      <!-- Estado vacío -->
      @if (!loading() && routePlans().length === 0) {
        <div class="p-8 text-center bg-white border border-[#E3E8EE] rounded-lg space-y-3">
          <p class="text-[#3C4F69] text-[13px]">No hay rutas planificadas todavía.</p>
          <div class="flex justify-center gap-3">
            @if (session.canWrite()) {
              <button
                type="button"
                (click)="openOrderRoutingModal()"
                class="inline-flex items-center text-[13px] font-medium px-4 py-1.5 rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] shadow-2xs">
                Crear primera ruta por pedidos
              </button>
              <button
                type="button"
                (click)="triggerEmergencyFlow()"
                class="inline-flex items-center text-[13px] font-medium px-4 py-1.5 rounded-md bg-rose-50 border border-rose-200 text-rose-700 hover:bg-rose-100">
                Evaluar requerimientos de urgencia
              </button>
            }
            <a 
              routerLink="/alertas" 
              class="inline-flex items-center text-[13px] font-medium px-3.5 py-1.5 rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors">
              Ver alertas de stock
            </a>
          </div>
        </div>
      }

      <!-- Listado y detalle de rutas -->
      @if (!loading() && routePlans().length > 0) {
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- Columna izquierda: Lista de rutas -->
          <div class="space-y-3">
            <h2 class="text-[12px] font-semibold text-[#697386] uppercase tracking-wider">
              Rutas registradas ({{ routePlans().length }})
            </h2>
            <div class="space-y-2">
              @for (plan of routePlans(); track plan.id) {
                <div 
                  (click)="selectPlan(plan.id)"
                  class="p-3.5 rounded-lg border cursor-pointer transition-all bg-white"
                  [ngClass]="selectedPlanId() === plan.id ? 'border-[#635BFF] ring-1 ring-[#635BFF] bg-[#F8FAFC]/60' : 'border-[#E3E8EE] hover:border-[#8792A2]'">
                  <div class="flex items-center justify-between">
                    <span class="font-semibold text-[13px] text-[#1A2C44]">{{ plan.code }}</span>
                    <span
                      class="text-[10px] font-mono px-2 py-0.5 rounded font-medium"
                      [ngClass]="{
                        'bg-emerald-50 text-emerald-700 border border-emerald-200': plan.status === 'Published',
                        'bg-amber-50 text-amber-700 border border-amber-200': plan.status === 'Proposed',
                        'bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]': plan.status !== 'Published' && plan.status !== 'Proposed'
                      }">
                      {{ plan.status === 'Published' ? 'Publicada' : plan.status === 'Proposed' ? 'Propuesta' : plan.status }}
                    </span>
                  </div>
                  <div class="text-[12px] text-[#697386] mt-2 flex justify-between font-mono">
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
              <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 space-y-4 shadow-2xs">
                <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-[#F1F3F5] pb-4">
                  <div>
                    <h3 class="font-semibold text-[#1A2C44] text-[16px]">{{ selectedDetail()!.code }}</h3>
                    <div class="flex flex-wrap gap-x-4 gap-y-1 text-[12px] text-[#3C4F69] font-mono mt-1">
                      <span><strong>Distancia:</strong> {{ selectedDetail()!.totalDistanceKm }} km</span>
                      <span><strong>Duración:</strong> {{ selectedDetail()!.totalDurationMinutes }} min</span>
                      <span><strong>Vehículo:</strong> {{ selectedDetail()!.vehicleCode || 'C-01' }}</span>
                      <span><strong>Salida:</strong> {{ selectedDetail()!.plannedStartUtc | date:'shortTime' }}</span>
                      @if (selectedDetail()!.plannedEndUtc) {
                        <span><strong>Llegada est.:</strong> {{ selectedDetail()!.plannedEndUtc | date:'shortTime' }}</span>
                      }
                    </div>
                  </div>

                  @if (selectedDetail()!.status !== 'Published') {
                    @if (session.canWrite()) {
                      <div class="flex items-center gap-2">
                        <label class="text-[12px] text-[#3C4F69] flex items-center gap-2">
                          Conductor:
                          <select class="border border-[#E3E8EE] rounded-md px-2 py-1 text-[12px] bg-white text-[#1A2C44] focus:outline-none focus:border-[#635BFF]" aria-label="Conductor de la ruta"
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
                          class="inline-flex items-center text-[12px] font-medium px-3.5 py-1.5 rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-2xs disabled:opacity-50">
                          {{ actionLoading() ? 'Publicando...' : 'Publicar a Conductor' }}
                        </button>
                      </div>
                      @if (drivers().length === 0) {
                        <span class="text-[11px] text-amber-700">No hay conductores activos registrados en el sistema.</span>
                      }
                    } @else {
                      <span class="text-[11px] font-medium text-amber-700 bg-amber-50 border border-amber-200 px-2.5 py-1 rounded-md">
                        Pendiente de publicación
                      </span>
                    }
                  } @else {
                    <span class="text-[11px] font-medium text-emerald-700 bg-emerald-50 border border-emerald-200 px-2.5 py-1 rounded-md">
                      Ruta Publicada al Conductor
                    </span>
                  }
                </div>

                <!-- Datos de asignación de la ruta -->
                <div class="flex flex-wrap items-center gap-x-4 gap-y-2 text-[12px] text-[#3C4F69]" data-route-assignment>
                  <span><span class="text-[#8792A2]">Almacén origen:</span> <strong class="text-[#1A2C44]">{{ selectedDetail()!.originWarehouseName || 'Sin almacén' }}</strong></span>
                  <span><span class="text-[#8792A2]">Vehículo:</span> <strong class="text-[#1A2C44]">{{ selectedDetail()!.vehicleCode || '—' }}</strong></span>
                  @if (selectedDetail()!.status === 'Published') {
                    <span>
                      <span class="text-[#8792A2]">Conductor:</span>
                      <strong [class.text-rose-700]="!selectedDetail()!.driverName" class="text-[#1A2C44]">{{ selectedDetail()!.driverName || 'Sin conductor asignado' }}</strong>
                    </span>
                    @if ((selectedDetail()!.unassignedDeliveries ?? 0) > 0) {
                      <span class="text-rose-700 font-medium">{{ selectedDetail()!.unassignedDeliveries }} entregas sin conductor.</span>
                    }
                    @if (session.canWrite()) {
                      <span class="flex items-center gap-2">
                        <select class="border border-[#E3E8EE] rounded-md px-2 py-1 text-[12px] bg-white text-[#1A2C44]" aria-label="Reasignar conductor"
                          [ngModel]="reassignDriverId()" (ngModelChange)="reassignDriverId.set($event)">
                          <option value="">Cambiar conductor…</option>
                          @for (driver of drivers(); track driver.accountId) {
                            <option [value]="driver.accountId">{{ driver.displayName }}</option>
                          }
                        </select>
                        <button type="button" class="text-[12px] font-medium text-[#635BFF] hover:underline disabled:opacity-50"
                          [disabled]="!reassignDriverId() || actionLoading()" (click)="reassignDriver(selectedDetail()!.id)">
                          Asignar
                        </button>
                      </span>
                    }
                  }
                </div>

                <!-- Mapa interactivo de la ruta -->
                @if (mapMarkers().length > 0) {
                  <div class="h-72 rounded-lg overflow-hidden border border-[#E3E8EE] shadow-2xs">
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
                        class="inline-flex items-center gap-2 text-[11px] px-2.5 py-1 rounded-md border bg-white hover:bg-[#F8FAFC] transition-colors"
                        [class.border-[#635BFF]]="item.selected" [class.border-[#E3E8EE]]="!item.selected">
                        <span class="inline-block w-2.5 h-2.5 rounded-full" [style.background-color]="item.color"></span>
                        <span class="font-medium text-[#1A2C44]">{{ item.vehicleCode }}</span>
                        <span class="text-[#697386]">{{ item.km | number:'1.0-1' }} km {{ item.source === 'osrm' ? 'por red vial' : 'aprox.' }} &bull; {{ item.stops }} paradas</span>
                      </button>
                    }
                  </div>
                  @if (routeLegendHasApproximate()) {
                    <p class="text-[11px] text-amber-700">
                      Las líneas punteadas son aproximadas (trazado recto entre paradas).
                    </p>
                  }
                }

                <!-- Pestañas de Detalle de la Planificación -->
                <div class="border-b border-[#E3E8EE] flex flex-wrap gap-2 pt-2">
                  <button
                    type="button"
                    (click)="detailTab.set('paradas')"
                    class="pb-2.5 px-3 text-[12px] font-medium border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'paradas' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#697386] hover:text-[#30313D]'">
                    <span>Itinerario y Paradas</span>
                    <span class="text-[10px] px-1.5 py-0.2 rounded bg-[#F8FAFC] border border-[#E3E8EE] text-[#697386] font-mono">
                      {{ selectedDetail()!.stops.length }}
                    </span>
                  </button>

                  <button
                    type="button"
                    (click)="detailTab.set('productos')"
                    class="pb-2.5 px-3 text-[12px] font-medium border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'productos' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#697386] hover:text-[#30313D]'">
                    <span>Por Producto (SKUs)</span>
                    @if (aggregatedProducts().length > 0) {
                      <span class="text-[10px] px-1.5 py-0.2 rounded bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] font-medium font-mono">
                        {{ aggregatedProducts().length }}
                      </span>
                    }
                  </button>

                  <button
                    type="button"
                    (click)="detailTab.set('conductor')"
                    class="pb-2.5 px-3 text-[12px] font-medium border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'conductor' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#697386] hover:text-[#30313D]'">
                    <span>Por Conductor</span>
                    <span class="text-[10px] px-1.5 py-0.2 rounded font-mono"
                      [ngClass]="selectedDetail()!.driverName ? 'bg-emerald-50 text-emerald-700 font-medium' : 'bg-amber-50 text-amber-700'">
                      {{ selectedDetail()!.driverName ? 'Asignado' : 'Pendiente' }}
                    </span>
                  </button>

                  <button
                    type="button"
                    (click)="detailTab.set('stock-bodegas'); loadWarehouseStockForSelected()"
                    data-testid="tab-stock-bodegas"
                    class="pb-2.5 px-3 text-[12px] font-medium border-b-2 transition-colors flex items-center gap-1.5"
                    [ngClass]="detailTab() === 'stock-bodegas' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#697386] hover:text-[#30313D]'">
                    <span>Stock por Almacén</span>
                    @if (warehouseStock().length > 0) {
                      <span class="text-[10px] px-1.5 py-0.2 rounded bg-[#F8FAFC] border border-[#E3E8EE] text-[#697386] font-mono">
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
                        <h4 class="text-[12px] font-semibold text-[#1A2C44] uppercase tracking-wider">
                          Paradas, Productos, Lotes y Bultos
                        </h4>
                        <span class="text-[11px] text-[#697386] font-mono">
                          {{ selectedDetail()!.stops.length }} paradas programadas &bull; {{ totalPlannedUnits() | number:'1.0-2' }} unidades totales
                        </span>
                      </div>
                      <div class="flex items-center gap-2">
                        <button
                          type="button"
                          (click)="expandAllStops()"
                          class="text-[11px] font-medium text-[#635BFF] hover:underline">
                          Expandir todo
                        </button>
                        <span class="text-[#E3E8EE]">|</span>
                        <button
                          type="button"
                          (click)="collapseAllStops()"
                          class="text-[11px] font-medium text-[#697386] hover:text-[#1A2C44] hover:underline">
                          Colapsar todo
                        </button>
                      </div>
                    </div>

                    <div class="space-y-3">
                      @for (stop of selectedDetail()!.stops; track stop.sequence) {
                        <div class="border border-[#E3E8EE] rounded-lg overflow-hidden bg-white shadow-2xs transition-all">
                          <!-- Cabecera de la parada (clic para colapsar/expandir) -->
                          <div
                            (click)="toggleStopExpansion(stop.sequence)"
                            class="p-3 bg-[#F8FAFC] hover:bg-slate-100/70 cursor-pointer flex flex-col sm:flex-row sm:items-center justify-between gap-2 select-none border-b border-[#E3E8EE]">
                            <div class="flex items-center gap-2.5">
                              <span class="w-6 h-6 rounded-full flex items-center justify-center text-[11px] font-semibold font-mono"
                                [ngClass]="stop.sequence === 1 ? 'bg-amber-100 text-amber-900 border border-amber-200' : 'bg-slate-200 text-slate-800'">
                                {{ stop.sequence }}
                              </span>
                              <div>
                                <div class="flex items-center gap-2">
                                  <span class="px-2 py-0.5 rounded text-[10px] font-medium"
                                    [ngClass]="stop.sequence === 1 ? 'bg-amber-50 text-amber-800 border border-amber-200' : 'bg-sky-50 text-sky-800 border border-sky-200'">
                                    {{ stop.sequence === 1 ? 'RECOGIDA' : 'ENTREGA' }}
                                  </span>
                                  <span class="font-semibold text-[13px] text-[#1A2C44]">
                                    {{ stop.servicePointName || stop.servicePointCode }}
                                  </span>
                                  <span class="text-[11px] font-mono text-[#8792A2]">({{ stop.servicePointCode }})</span>
                                </div>
                                <div class="flex flex-wrap gap-x-3 text-[11px] text-[#3C4F69] font-mono mt-0.5">
                                  <span><strong>Ventana:</strong> {{ stop.windowStart || '08:00' }} - {{ stop.windowEnd || '17:00' }}</span>
                                  <span><strong>Llegada estimada:</strong> <strong class="text-[#635BFF]">{{ stop.etaUtc ? (stop.etaUtc | date:'shortTime') : '08:30' }}</strong></span>
                                  <span><strong>Atención:</strong> {{ stop.serviceMinutes || 15 }} min</span>
                                </div>
                              </div>
                            </div>

                            <div class="flex items-center gap-2 self-end sm:self-center">
                              <button
                                type="button"
                                (click)="$event.stopPropagation(); inspectStockForStop(stop)"
                                [attr.data-testid]="'stop-stock-btn-' + stop.sequence"
                                class="px-2 py-0.5 rounded text-[10px] font-medium bg-white text-[#30313D] hover:bg-[#F8FAFC] border border-[#E3E8EE] shadow-2xs transition-colors"
                                title="Consultar existencias por almacén para esta parada">
                                Stock en almacenes
                              </button>
                              @if (stop.lines && stop.lines.length > 0) {
                                <span class="px-2 py-0.5 rounded-md text-[10px] font-medium bg-[#F8FAFC] text-[#30313D] border border-[#E3E8EE] font-mono">
                                  {{ stop.lines.length }} {{ stop.lines.length === 1 ? 'producto' : 'productos' }}
                                </span>
                              }
                              @if (stop.deliveryStatus) {
                                <span class="px-2 py-0.5 rounded text-[10px] font-medium"
                                  [ngClass]="stop.deliveryStatus === 'Delivered' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : (stop.deliveryStatus === 'Proposed' ? 'bg-amber-50 text-amber-800 border border-amber-200' : 'bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]')">
                                  Entrega: {{ stop.deliveryStatus }}
                                </span>
                              }
                              <span class="px-2 py-0.5 rounded text-[10px] font-medium"
                                [ngClass]="stop.status === 'Completed' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]'">
                                Parada: {{ stop.status }}
                              </span>
                              <span class="text-xs text-[#8792A2] font-mono">
                                {{ expandedStopSeqs().has(stop.sequence) ? '▲' : '▼' }}
                              </span>
                            </div>
                          </div>

                          <!-- Contenido expandido: Detalle de productos, lotes y bultos de la parada -->
                          @if (expandedStopSeqs().has(stop.sequence)) {
                            <div class="p-3 bg-white space-y-3">
                              <!-- Metadatos de la entrega y bultos -->
                              @if (stop.deliveryCode || (stop.handlingUnits && stop.handlingUnits.length > 0)) {
                                <div class="flex flex-wrap items-center justify-between gap-2 p-2 rounded-md bg-[#F8FAFC] border border-[#E3E8EE] text-[11px]">
                                  <div class="flex items-center gap-2">
                                    <span class="text-[#697386] font-mono">Entrega:</span>
                                    <strong class="font-mono text-[#1A2C44]">{{ stop.deliveryCode || 'DEL-...' }}</strong>
                                    <span class="px-1.5 py-0.5 rounded text-[10px] font-medium bg-white border border-[#E3E8EE] text-[#30313D]">
                                      {{ stop.deliveryStatus || 'Planificada' }}
                                    </span>
                                  </div>

                                  @if (stop.handlingUnits && stop.handlingUnits.length > 0) {
                                    <div class="flex flex-wrap items-center gap-1.5">
                                      <span class="text-[#697386] font-mono">Bultos:</span>
                                      @for (hu of stop.handlingUnits; track hu.id) {
                                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-white border border-[#E3E8EE] font-mono text-[10px] text-[#30313D]">
                                          <span>{{ hu.code }}</span>
                                          <span class="text-[9px] px-1 rounded bg-[#F8FAFC] text-[#697386] font-medium uppercase">{{ hu.status }}</span>
                                        </span>
                                      }
                                    </div>
                                  }
                                </div>
                              }

                              <!-- Tabla de productos y lotes de la parada -->
                              @if (stop.lines && stop.lines.length > 0) {
                                @if (stop.kind === 'Pickup') {
                                  <p class="text-[12px] font-medium text-amber-800 mb-1" data-pickup-load>Cargar en este almacén (se entrega en las paradas siguientes):</p>
                                }
                                <div class="overflow-x-auto border border-[#E3E8EE] rounded-lg">
                                  <table class="w-full text-left text-xs min-w-[500px]">
                                    <thead class="bg-[#F8FAFC] text-[10px] text-[#697386] uppercase border-b border-[#E3E8EE] font-mono">
                                      <tr>
                                        <th class="p-2">SKU</th>
                                        <th class="p-2">Producto</th>
                                        <th class="p-2">Lote Asignado</th>
                                        <th class="p-2 text-right">Cantidad Planificada</th>
                                        <th class="p-2">Bulto</th>
                                        <th class="p-2 text-right">Estado</th>
                                      </tr>
                                    </thead>
                                    <tbody class="divide-y divide-[#F1F3F5]">
                                      @for (line of stop.lines; track line.productId + (line.lotId || '') + line.sku) {
                                        <tr class="hover:bg-[#F8FAFC]/50">
                                          <td class="p-2 font-mono font-medium text-[#1A2C44]">{{ line.sku }}</td>
                                          <td class="p-2 text-[#30313D] font-medium">{{ line.productName }}</td>
                                          <td class="p-2">
                                            @if (line.lotCode) {
                                              <span class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-amber-50 text-amber-800 border border-amber-200 font-mono text-[10px]">
                                                <span>{{ line.lotCode }}</span>
                                                @if (line.lotExpiresOn) {
                                                  <span class="text-[9px] text-amber-600">({{ line.lotExpiresOn }})</span>
                                                }
                                              </span>
                                            } @else {
                                              <span class="text-[#8792A2] font-mono text-[11px]">— Asignación al publicar —</span>
                                            }
                                          </td>
                                          <td class="p-2 text-right font-mono font-bold text-[#1A2C44]">
                                            {{ line.plannedQuantity | number:'1.0-2' }} <span class="text-[#8792A2] font-normal text-[10px]">{{ line.baseUnit }}</span>
                                          </td>
                                          <td class="p-2">
                                            @if (line.handlingUnitCode) {
                                              <span class="px-1.5 py-0.5 rounded bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] font-mono text-[10px]">
                                                {{ line.handlingUnitCode }}
                                              </span>
                                            } @else if (stop.handlingUnits && stop.handlingUnits.length > 0) {
                                              <span class="px-1.5 py-0.5 rounded bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] font-mono text-[10px]">
                                                {{ stop.handlingUnits[0].code }}
                                              </span>
                                            } @else {
                                              <span class="text-[#8792A2] font-mono text-[10px]">Sin consolidar</span>
                                            }
                                          </td>
                                          <td class="p-2 text-right">
                                            <span class="px-1.5 py-0.5 rounded text-[10px] font-medium"
                                              [ngClass]="line.deliveryStatus === 'Delivered' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]'">
                                              {{ line.deliveryStatus || stop.deliveryStatus || 'Planificada' }}
                                            </span>
                                          </td>
                                        </tr>
                                      }
                                    </tbody>
                                  </table>
                                </div>
                              }
                              @if (hasPending(stop.loadQuantities)) {
                                <div class="mt-2 p-2.5 rounded-lg bg-amber-50 border border-amber-200 text-xs text-amber-900" data-partial-load>
                                  <p class="font-semibold mb-1">Carga parcial por capacidad de vehículo</p>
                                  @for (q of stop.loadQuantities; track q.sku) {
                                    <p class="font-mono text-[11px]">{{ q.sku }}: requeridas {{ q.requiredQuantity | number:'1.0-2' }} &bull; planificadas {{ q.plannedQuantity | number:'1.0-2' }} &bull; pendientes {{ q.pendingQuantity | number:'1.0-2' }} {{ q.baseUnit }}</p>
                                  }
                                </div>
                              }
                              @if (!stop.lines || stop.lines.length === 0) {
                                <div class="p-3 text-center text-xs text-[#697386] bg-[#F8FAFC] rounded-lg border border-[#E3E8EE]">
                                  @if (stop.kind === 'Pickup') {
                                    <span>Almacén de recogida: sin productos para cargar en este punto.</span>
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

                <!-- CONTENIDO PESTAÑA 2: VISTA AGREGADA POR PRODUCTO -->
                @if (detailTab() === 'productos') {
                  <div class="space-y-4">
                    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                      <div>
                        <h4 class="text-[12px] font-semibold text-[#1A2C44] uppercase tracking-wider">
                          Resumen Agregado de Carga por Producto (SKU)
                        </h4>
                        <p class="text-[12px] text-[#3C4F69]">
                          Total consolidado de insumos que salen del almacén en este vehículo y ruta.
                        </p>
                      </div>
                      <div class="flex items-center gap-3 text-xs font-mono">
                        <span class="px-2 py-1 rounded bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D]">
                          <strong>{{ aggregatedProducts().length }}</strong> SKUs distintos
                        </span>
                        <span class="px-2 py-1 rounded bg-[#F8FAFC] text-[#1A2C44] border border-[#E3E8EE] font-semibold">
                          {{ totalPlannedUnits() | number:'1.0-2' }} unidades totales
                        </span>
                      </div>
                    </div>

                    @if (aggregatedProducts().length > 0) {
                      <div class="border border-[#E3E8EE] rounded-lg overflow-x-auto bg-white shadow-2xs">
                        <table class="w-full text-left text-xs min-w-[600px]">
                          <thead class="bg-[#F8FAFC] text-[11px] text-[#697386] uppercase border-b border-[#E3E8EE] font-semibold">
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
                          <tbody class="divide-y divide-[#F1F3F5]">
                            @for (item of aggregatedProducts(); track item.sku) {
                              <tr class="hover:bg-[#F8FAFC]/50">
                                <td class="p-3 font-mono font-medium text-[#1A2C44]">{{ item.sku }}</td>
                                <td class="p-3 font-medium text-[#30313D]">{{ item.productName }}</td>
                                <td class="p-3 text-right font-mono font-bold text-[#635BFF] text-[13px]">
                                  {{ item.totalPlannedQuantity | number:'1.0-2' }}
                                </td>
                                <td class="p-3 font-mono text-[#697386]">{{ item.baseUnit }}</td>
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
                                    <span class="text-[#8792A2] font-mono text-[10px]">Asignación automática</span>
                                  }
                                </td>
                                <td class="p-3 text-center font-mono">
                                  <span class="px-2 py-0.5 rounded-full bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] font-medium text-[10px]">
                                    {{ item.stopsCount }} {{ item.stopsCount === 1 ? 'parada' : 'paradas' }}
                                  </span>
                                </td>
                                <td class="p-3 text-right">
                                  @for (st of item.deliveryStatuses; track st) {
                                    <span class="px-1.5 py-0.5 rounded text-[10px] font-medium"
                                      [ngClass]="st === 'Delivered' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]'">
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
                      <div class="p-8 text-center text-xs text-[#697386] bg-white border border-[#E3E8EE] rounded-lg">
                        No hay productos planificados en esta ruta aún.
                      </div>
                    }
                  </div>
                }

                <!-- CONTENIDO PESTAÑA 3: VISTA POR CONDUCTOR -->
                @if (detailTab() === 'conductor') {
                  <div class="space-y-4">
                    <!-- Ficha del conductor de esta ruta -->
                    <div class="bg-[#F8FAFC] border border-[#E3E8EE] rounded-lg p-4 space-y-3">
                      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                        <div class="flex items-center gap-3">
                          <div class="w-9 h-9 rounded-full bg-[#E3E8EE] text-[#1A2C44] flex items-center justify-center font-bold text-xs">
                            C
                          </div>
                          <div>
                            <span class="text-[10px] uppercase font-semibold text-[#697386]">Conductor Asignado a Esta Ruta</span>
                            <h4 class="font-semibold text-[#1A2C44] text-[14px]">
                              {{ selectedDetail()!.driverName || 'Sin conductor asignado' }}
                            </h4>
                          </div>
                        </div>

                        <div class="flex items-center gap-2">
                          <span class="px-2.5 py-1 rounded-md text-[11px] font-medium"
                            [ngClass]="selectedDetail()!.driverName ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-rose-50 text-rose-800 border border-rose-200'">
                            {{ selectedDetail()!.driverName ? 'Asignado' : 'Pendiente de Asignación' }}
                          </span>
                        </div>
                      </div>

                      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-2 border-t border-[#E3E8EE] text-xs">
                        <div>
                          <span class="text-[#8792A2] block text-[10px] uppercase font-medium">Vehículo</span>
                          <strong class="font-mono text-[#1A2C44]">{{ selectedDetail()!.vehicleCode || '—' }}</strong>
                        </div>
                        <div>
                          <span class="text-[#8792A2] block text-[10px] uppercase font-medium">Almacén Origen</span>
                          <strong class="text-[#1A2C44]">{{ selectedDetail()!.originWarehouseName || 'Central' }}</strong>
                        </div>
                        <div>
                          <span class="text-[#8792A2] block text-[10px] uppercase font-medium">Entregas Totales</span>
                          <strong class="font-mono text-[#1A2C44]">{{ selectedDetail()!.deliveriesCount || selectedDetail()!.stops.length }}</strong>
                        </div>
                        <div>
                          <span class="text-[#8792A2] block text-[10px] uppercase font-medium">Entregas sin Conductor</span>
                          <strong [class.text-rose-700]="(selectedDetail()!.unassignedDeliveries ?? 0) > 0" class="font-mono text-[#1A2C44]">
                            {{ selectedDetail()!.unassignedDeliveries ?? 0 }}
                          </strong>
                        </div>
                      </div>
                    </div>

                    <!-- Rutas que lleva este conductor -->
                    <div class="space-y-2">
                      <h4 class="text-[12px] font-semibold text-[#1A2C44] uppercase tracking-wider">
                        Rutas a Cargo de Este Conductor ({{ driverAssignedRoutes().length }})
                      </h4>

                      @if (driverAssignedRoutes().length > 0) {
                        <div class="border border-[#E3E8EE] rounded-lg overflow-x-auto bg-white shadow-2xs">
                          <table class="w-full text-left text-xs min-w-[550px]">
                            <thead class="bg-[#F8FAFC] text-[11px] text-[#697386] uppercase border-b border-[#E3E8EE] font-semibold">
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
                            <tbody class="divide-y divide-[#F1F3F5]">
                              @for (r of driverAssignedRoutes(); track r.id) {
                                <tr class="hover:bg-[#F8FAFC]/60 cursor-pointer"
                                  [class.bg-[#F8FAFC]]="r.isCurrent"
                                  (click)="selectPlan(r.id)">
                                  <td class="p-2.5 font-mono font-medium text-[#1A2C44] flex items-center gap-1.5">
                                    <span>{{ r.code }}</span>
                                    @if (r.isCurrent) {
                                      <span class="px-1.5 py-0.2 rounded text-[9px] bg-[#635BFF] text-white font-medium">Actual</span>
                                    }
                                  </td>
                                  <td class="p-2.5 font-mono text-[#30313D]">{{ r.vehicleCode }}</td>
                                  <td class="p-2.5 text-[#697386] font-mono text-[11px]">{{ r.plannedStartUtc | date:'short' }}</td>
                                  <td class="p-2.5 text-right font-mono text-[#30313D]">{{ r.totalDistanceKm | number:'1.0-1' }} km</td>
                                  <td class="p-2.5 text-right font-mono text-[#30313D]">{{ r.totalDurationMinutes | number:'1.0-0' }} min</td>
                                  <td class="p-2.5 text-center font-mono text-[#30313D]">{{ r.stopsCount }}</td>
                                  <td class="p-2.5 text-right">
                                    <span class="px-2 py-0.5 rounded text-[10px] font-medium"
                                      [ngClass]="r.status === 'Published' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]'">
                                      {{ r.status === 'Published' ? 'Publicada' : r.status }}
                                    </span>
                                  </td>
                                </tr>
                              }
                            </tbody>
                          </table>
                        </div>
                      } @else {
                        <div class="p-4 bg-white border border-[#E3E8EE] rounded-lg text-xs text-[#697386] text-center">
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
                      <h4 class="text-[12px] font-semibold text-[#1A2C44] uppercase tracking-wider">
                        Distribución de Rutas por Conductor en la Flota
                      </h4>
                      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                        @for (dw of allDriversWorkload(); track dw.driverName) {
                          <div class="p-3 bg-white border border-[#E3E8EE] rounded-lg space-y-2 shadow-2xs">
                            <div class="flex items-center justify-between">
                              <strong class="text-xs text-[#1A2C44]">{{ dw.driverName }}</strong>
                              <span class="px-2 py-0.5 rounded-md text-[10px] font-medium bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] font-mono">
                                {{ dw.routesCount }} {{ dw.routesCount === 1 ? 'ruta' : 'rutas' }}
                              </span>
                            </div>
                            <div class="flex items-center justify-between text-[11px] text-[#697386] font-mono">
                              <span>{{ dw.totalStops }} paradas</span>
                              <span>{{ dw.totalKm | number:'1.0-1' }} km</span>
                            </div>
                            <div class="flex flex-wrap gap-1 pt-1 border-t border-[#F1F3F5]">
                              @for (r of dw.routes; track r.id) {
                                <button
                                  type="button"
                                  (click)="selectPlan(r.id)"
                                  class="text-[10px] font-mono px-2 py-0.5 rounded-md transition-colors"
                                  [ngClass]="r.id === selectedDetail()!.id ? 'bg-[#635BFF] text-white' : 'bg-[#F8FAFC] border border-[#E3E8EE] text-[#30313D] hover:bg-slate-100'">
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

                <!-- CONTENIDO PESTAÑA 4: STOCK POR ALMACÉN -->
                @if (detailTab() === 'stock-bodegas') {
                  <div class="space-y-4" data-testid="warehouse-stock-panel">
                    <!-- Filtros de contexto: Parada o SKU -->
                    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-[#F8FAFC] p-3 rounded-lg border border-[#E3E8EE]">
                      <div class="flex flex-wrap items-center gap-3">
                        <label for="stock-stop-select" class="text-[12px] font-medium text-[#30313D]">Filtrar por Parada:</label>
                        <select
                          id="stock-stop-select"
                          [ngModel]="selectedStopForStock()?.servicePointId || ''"
                          (ngModelChange)="onStockStopSelect($event)"
                          data-testid="stock-stop-select"
                          class="text-xs p-1.5 border border-[#E3E8EE] rounded-md bg-white text-[#1A2C44] min-w-[200px] focus:outline-none focus:border-[#635BFF]">
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
                            class="text-xs font-medium text-[#635BFF] hover:underline">
                            Limpiar filtro
                          </button>
                        }
                        <button
                          type="button"
                          (click)="loadWarehouseStockForSelected()"
                          [disabled]="warehouseStockLoading()"
                          data-testid="refresh-stock-btn"
                          class="inline-flex items-center gap-1 text-[12px] font-medium px-2.5 py-1 rounded-md bg-white border border-[#E3E8EE] hover:bg-[#F8FAFC] text-[#30313D] transition-colors shadow-2xs">
                          @if (warehouseStockLoading()) {
                            <span class="inline-block animate-spin h-3 w-3 border-2 border-[#635BFF] border-t-transparent rounded-full"></span>
                          }
                          <span>Actualizar Stock</span>
                        </button>
                      </div>
                    </div>

                    <!-- Indicador de contexto activo -->
                    @if (selectedStopForStock()) {
                      <div class="p-2.5 bg-[#F8FAFC] border border-[#E3E8EE] rounded-md text-xs text-[#1A2C44] flex items-center justify-between" data-testid="stock-active-context-stop">
                        <span>
                          Mostrando disponibilidad en almacenes para la parada:
                          <strong>{{ selectedStopForStock()!.servicePointName || selectedStopForStock()!.servicePointCode }}</strong>
                        </span>
                        <span class="text-[11px] font-mono font-medium bg-white border border-[#E3E8EE] px-2 py-0.5 rounded">
                          Parada #{{ selectedStopForStock()!.sequence }}
                        </span>
                      </div>
                    } @else if (selectedProductSkuForStock()) {
                      <div class="p-2.5 bg-[#F8FAFC] border border-[#E3E8EE] rounded-md text-xs text-[#1A2C44] flex items-center justify-between" data-testid="stock-active-context-sku">
                        <span>
                          Mostrando disponibilidad en almacenes para el SKU:
                          <strong>{{ selectedProductSkuForStock() }}</strong>
                        </span>
                        <button (click)="clearStockFilter()" class="text-xs text-[#635BFF] hover:underline font-medium">✕ Quitar filtro</button>
                      </div>
                    }

                    <!-- Cargando -->
                    @if (warehouseStockLoading()) {
                      <div class="p-6 text-center text-[#697386] text-xs bg-white border border-[#E3E8EE] rounded-lg" data-testid="stock-loading-indicator">
                        Consultando existencias por almacén...
                      </div>
                    }

                    <!-- Lista de existencias por almacén -->
                    @if (!warehouseStockLoading() && filteredWarehouseStock().length === 0) {
                      <div class="p-6 text-center bg-white border border-[#E3E8EE] rounded-lg text-xs text-[#697386]" data-testid="stock-empty-message">
                        No se encontraron registros de existencias en almacén para la selección actual.
                      </div>
                    }

                    @if (!warehouseStockLoading() && filteredWarehouseStock().length > 0) {
                      <div class="overflow-x-auto border border-[#E3E8EE] rounded-lg bg-white shadow-2xs">
                        <table class="w-full text-xs text-left" data-testid="warehouse-stock-table">
                          <thead class="bg-[#F8FAFC] border-b border-[#E3E8EE] text-[#697386] uppercase font-semibold text-[11px]">
                            <tr>
                              <th class="p-3">Almacén</th>
                              <th class="p-3">Producto / SKU</th>
                              <th class="p-3 text-right">Disponible</th>
                              <th class="p-3 text-right">Físico</th>
                              <th class="p-3 text-right">Reservado</th>
                              <th class="p-3 text-center">Estado</th>
                            </tr>
                          </thead>
                          <tbody class="divide-y divide-[#F1F3F5] font-mono text-[#30313D]">
                            @for (item of filteredWarehouseStock(); track item.locationId + '-' + item.productId) {
                              <tr class="hover:bg-[#F8FAFC]/50" data-testid="warehouse-stock-row">
                                <td class="p-3 font-sans font-medium text-[#1A2C44]">
                                  {{ item.locationName || item.locationCode }}
                                  <span class="text-xs text-[#8792A2] font-mono ml-1">({{ item.locationCode }})</span>
                                </td>
                                <td class="p-3 font-sans">
                                  <div class="font-medium text-[#1A2C44]">{{ item.productName }}</div>
                                  <div class="text-[11px] text-[#8792A2] font-mono">{{ item.sku }}</div>
                                </td>
                                <td class="p-3 text-right font-bold text-[#1A2C44]">
                                  {{ item.availableQuantity | number:'1.0-2' }}
                                </td>
                                <td class="p-3 text-right text-[#697386]">
                                  {{ item.quantityOnHand | number:'1.0-2' }}
                                </td>
                                <td class="p-3 text-right text-[#8792A2]">
                                  {{ item.quantityReserved | number:'1.0-2' }}
                                </td>
                                <td class="p-3 text-center font-sans">
                                  @switch (item.status) {
                                    @case ('Critico') {
                                      <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-rose-50 text-rose-700 border border-rose-200">Crítico</span>
                                    }
                                    @case ('SinStock') {
                                      <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-rose-100 text-rose-800 border border-rose-200">Sin Stock</span>
                                    }
                                    @case ('Exceso') {
                                      <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-amber-50 text-amber-700 border border-amber-200">Exceso</span>
                                    }
                                    @default {
                                      <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">Normal</span>
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
      <!-- BANDEJA DE ENTREGAS CON NOVEDAD POR CENTRO DE SALUD -->
      <!-- ======================================================== -->
      @if (activeTab() === 'rechazos') {
        <div class="space-y-4">
          <!-- Filtros y Barra de Acciones -->
          <div class="bg-white border border-[#E3E8EE] rounded-lg p-3.5 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div class="flex flex-col sm:flex-row sm:items-center gap-2.5">
              <label class="text-[13px] font-medium text-[#30313D]">Centro de salud o destino:</label>
              <select
                [ngModel]="selectedServicePointFilter()"
                (ngModelChange)="onServicePointFilterChange($event)"
                class="text-[13px] h-9 px-3 border border-[#E3E8EE] rounded-md bg-white text-[#1A2C44] min-w-[240px] focus:outline-none focus:border-[#635BFF] focus:ring-1 focus:ring-[#635BFF]">
                <option value="">Todos los centros (Vista general)</option>
                @for (pt of availableLocations(); track pt.id) {
                  <option [value]="pt.id">{{ pt.name }} ({{ pt.code }})</option>
                }
              </select>
            </div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                (click)="loadRejectedCaptures()"
                [disabled]="rejectedLoading()"
                class="inline-flex items-center gap-1.5 h-9 px-3 text-[13px] font-medium rounded-md bg-[#F8FAFC] text-[#30313D] border border-[#E3E8EE] hover:bg-[#F1F4F8] transition-colors disabled:opacity-50">
                @if (rejectedLoading()) {
                  <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-[#635BFF] border-t-transparent rounded-full"></span>
                }
                <span>Actualizar registros</span>
              </button>
            </div>
          </div>

          <!-- Métricas Resumen -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div class="bg-white border border-[#E3E8EE] rounded-lg p-3.5 shadow-xs">
              <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Entregas con Observación</span>
              <p class="text-2xl font-bold text-rose-600 mt-1">{{ rejectedCaptures().length }}</p>
              <p class="text-[12px] text-[#8792A2] mt-0.5">Pendientes de revisión de inventario</p>
            </div>
            <div class="bg-white border border-[#E3E8EE] rounded-lg p-3.5 shadow-xs">
              <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Centros Afectados</span>
              <p class="text-2xl font-bold text-amber-600 mt-1">{{ uniqueRejectedPointsCount() }}</p>
              <p class="text-[12px] text-[#8792A2] mt-0.5">Centros con novedades en recepción</p>
            </div>
            <div class="bg-white border border-[#E3E8EE] rounded-lg p-3.5 shadow-xs">
              <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Productos no Recibidos</span>
              <p class="text-2xl font-bold text-[#1A2C44] mt-1">{{ totalRejectedUnitsCount() }}</p>
              <p class="text-[12px] text-[#8792A2] mt-0.5">Unidades devueltas o con daño físico</p>
            </div>
          </div>

          <!-- Estado Cargando -->
          @if (rejectedLoading()) {
            <div class="p-8 text-center text-[#8792A2] text-[13px] bg-white border border-[#E3E8EE] rounded-lg shadow-xs">
              Consultando registros de novedades de entrega...
            </div>
          }

          <!-- Estado Vacío -->
          @if (!rejectedLoading() && rejectedCaptures().length === 0) {
            <div class="p-8 text-center bg-white border border-[#E3E8EE] rounded-lg shadow-xs space-y-2">
              <div class="w-10 h-10 mx-auto rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center font-bold text-lg">✓</div>
              <p class="text-[#1A2C44] font-semibold text-sm">Sin entregas observadas pendientes</p>
              <p class="text-[#8792A2] text-[13px] max-w-md mx-auto">
                No hay novedades ni rechazos reportados para el filtro actual. Todas las entregas registradas se completaron conformes.
              </p>
            </div>
          }

          <!-- Lista de Entregas con Novedad -->
          @if (!rejectedLoading() && rejectedCaptures().length > 0) {
            <div class="space-y-3">
              @for (capture of rejectedCaptures(); track capture.receiptId) {
                <div class="bg-white border border-[#E3E8EE] rounded-lg p-4 shadow-xs space-y-3">
                  <!-- Cabecera de la entrega -->
                  <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-[#E3E8EE] pb-3">
                    <div>
                      <div class="flex items-center gap-2">
                        <span class="font-semibold text-[#1A2C44] text-[14px]">{{ capture.servicePointName }}</span>
                        <span class="text-[11px] px-2 py-0.5 rounded bg-[#F8FAFC] text-[#4F5B66] border border-[#E3E8EE] font-medium">
                          {{ capture.servicePointCode }}
                        </span>
                      </div>
                      <p class="text-[12px] text-[#8792A2] mt-0.5">
                        Entrega: <strong class="text-[#30313D]">{{ capture.deliveryCode }}</strong> · 
                        Registrado por: <strong class="text-[#30313D]">{{ capture.registeredByName || capture.driverName }}</strong> · 
                        Fecha: {{ capture.receivedAtUtc | date:'medium' }}
                      </p>
                    </div>

                    <div class="flex items-center gap-2">
                      <span class="text-[11px] px-2.5 py-1 rounded-md font-medium bg-rose-50 text-rose-700 border border-rose-200">
                        Requiere revisión
                      </span>
                    </div>
                  </div>

                  <!-- Motivo de la novedad -->
                  <div class="bg-[#F8FAFC] border border-[#E3E8EE] rounded-md p-3 text-[12px] space-y-1">
                    <div class="flex items-center gap-1.5 font-semibold text-[#1A2C44]">
                      <span>Motivo reportado:</span>
                      <span class="text-[11px] bg-white border border-[#E3E8EE] px-1.5 py-0.5 rounded text-[#30313D]">{{ capture.reason }}</span>
                    </div>
                    <p class="text-[#8792A2] text-[11px]">
                      Registro aislado temporalmente para permitir que los despachos activos continúen sin interrupciones operativas.
                    </p>
                  </div>

                  <!-- Detalle de productos -->
                  <div>
                    <h5 class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider mb-2">Detalle de productos y cantidades</h5>
                    <div class="border border-[#E3E8EE] rounded-md overflow-x-auto">
                      <table class="w-full text-left text-[13px] min-w-[500px]">
                        <thead class="bg-[#F8FAFC] text-[11px] font-medium text-[#8792A2] uppercase tracking-wider border-b border-[#E3E8EE]">
                          <tr>
                            <th class="p-2.5">Producto</th>
                            <th class="p-2.5 text-center">Recibido Conforme</th>
                            <th class="p-2.5 text-center">No Recibido / Rechazado</th>
                            <th class="p-2.5">Observación</th>
                          </tr>
                        </thead>
                        <tbody class="divide-y divide-[#E3E8EE]">
                          @for (line of capture.lines; track line.productId) {
                            <tr>
                              <td class="p-2.5 font-medium text-[#1A2C44]">
                                {{ line.productName }}
                                <span class="block text-[11px] text-[#8792A2] font-mono">{{ line.sku }}</span>
                              </td>
                              <td class="p-2.5 text-center font-medium text-emerald-700">
                                {{ line.quantityAccepted }} u
                              </td>
                              <td class="p-2.5 text-center font-bold text-rose-600">
                                {{ line.quantityRejected }} u
                              </td>
                              <td class="p-2.5 text-[#4F5B66] text-[12px]">
                                {{ line.rejectionReason || 'Diferencia en recepción física' }}
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
      <div class="fixed inset-0 z-50 bg-[#0A2540]/60 backdrop-blur-xs flex items-center justify-center p-2 sm:p-4">
        <div class="bg-white w-full max-w-7xl h-[92vh] rounded-xl shadow-xl flex flex-col overflow-hidden border border-[#E3E8EE]">
          
          <!-- Header Asistente -->
          <div class="px-5 py-3.5 bg-[#1A2C44] text-white flex items-center justify-between border-b border-slate-700">
            <div>
              <div class="flex items-center gap-2.5">
                <h2 class="text-sm font-semibold text-white">Asistente de Planificación de Rutas y Distribución</h2>
                <span class="text-[11px] font-medium px-2 py-0.5 rounded-full bg-white/10 text-emerald-300 border border-white/20">
                  Paso {{ wizardStep() }} de 4
                </span>
              </div>
              <p class="text-[12px] text-slate-300 mt-0.5">
                @if (wizardStep() === 1) { Selecciona los pedidos a incluir o registra un nuevo pedido de medicamentos. }
                @if (wizardStep() === 2) { Revisa la asignación de almacenes con inventario disponible más cercanos a cada centro de salud. }
                @if (wizardStep() === 3) { Evalúa las alternativas de recorrido y organiza el orden de las paradas. }
                @if (wizardStep() === 4) { Asigna el vehículo disponible según capacidad de carga de peso y volumen. }
              </p>
            </div>

            <button
              type="button"
              (click)="closeWizard()"
              class="text-xs font-medium px-2.5 py-1.5 rounded-md bg-white/10 text-slate-200 hover:text-white hover:bg-white/20 transition-colors">
              Cerrar
            </button>
          </div>

          <!-- Cuerpo Asistente -->
          <div class="flex-1 grid grid-cols-1 lg:grid-cols-12 overflow-hidden">
            
            <!-- Columna Izquierda: Formulario y Pasos (5 Cols) -->
            <div class="lg:col-span-5 p-4 border-r border-[#E3E8EE] flex flex-col gap-3 bg-[#F8FAFC] overflow-y-auto">
              
              <!-- PASO 1: SELECCIONAR O CREAR PEDIDOS -->
              @if (wizardStep() === 1) {
                <div class="space-y-3">
                  <div class="flex items-center justify-between">
                    <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Pedidos Pendientes de Entrega</span>
                    <button
                      type="button"
                      (click)="showNewOrderForm.set(!showNewOrderForm())"
                      class="text-[12px] font-medium text-[#635BFF] hover:underline">
                      {{ showNewOrderForm() ? 'Cancelar nuevo' : '+ Registrar Solicitud Manual' }}
                    </button>
                  </div>

                  <!-- Formulario Nuevo Pedido Rápido -->
                  @if (showNewOrderForm()) {
                    <div class="p-3.5 bg-white rounded-lg border border-[#E3E8EE] shadow-xs space-y-2.5 text-[12px]">
                      <span class="font-semibold text-[#1A2C44] block text-[12px]">Nueva Solicitud de Entrega</span>
                      
                      <div>
                        <label class="text-[11px] text-[#4F5B66] block mb-1">Centro de Salud Destino</label>
                        <select
                          [(ngModel)]="newOrderDestId"
                          class="w-full text-[12px] h-8 px-2 border border-[#E3E8EE] rounded-md bg-white text-[#1A2C44] focus:outline-none focus:border-[#635BFF]">
                          <option value="">Selecciona destino...</option>
                          @for (loc of availableLocations(); track loc.id) {
                            <option [value]="loc.id">{{ loc.name }} ({{ loc.code }})</option>
                          }
                        </select>
                      </div>

                      <div class="grid grid-cols-2 gap-2">
                        <div>
                          <label class="text-[11px] text-[#4F5B66] block mb-1">Producto</label>
                          <select
                            [(ngModel)]="newOrderProdId"
                            class="w-full text-[12px] h-8 px-2 border border-[#E3E8EE] rounded-md bg-white text-[#1A2C44] focus:outline-none focus:border-[#635BFF]">
                            <option value="">Selecciona...</option>
                            @for (p of availableProducts(); track p.id) {
                              <option [value]="p.id">{{ p.name }} ({{ p.sku }})</option>
                            }
                          </select>
                        </div>
                        <div>
                          <label class="text-[11px] text-[#4F5B66] block mb-1">Cantidad Solicitada</label>
                          <input
                            type="number"
                            [(ngModel)]="newOrderQty"
                            min="1"
                            class="w-full text-[12px] h-8 px-2 border border-[#E3E8EE] rounded-md bg-white text-[#1A2C44] font-medium focus:outline-none focus:border-[#635BFF]" />
                        </div>
                      </div>

                      <button
                        type="button"
                        (click)="submitNewOrder()"
                        [disabled]="!newOrderDestId || !newOrderProdId || newOrderQty <= 0"
                        class="w-full h-8 rounded-md bg-[#635BFF] text-white font-medium hover:bg-[#5248E0] transition-colors disabled:opacity-50">
                        Guardar Solicitud
                      </button>
                    </div>
                  }

                  <!-- Lista de Pedidos Disponibles con Checkbox -->
                  <div class="space-y-2">
                    @for (ord of pendingOrders(); track ord.id) {
                      <div class="p-3 rounded-lg border bg-white shadow-xs flex items-start gap-2.5 text-[12px] cursor-pointer transition-all"
                        [ngClass]="selectedOrderIds().has(ord.id) ? 'border-[#635BFF] ring-2 ring-[#635BFF]/10' : 'border-[#E3E8EE] hover:border-slate-300'"
                        (click)="toggleOrderSelection(ord.id)">
                        <input
                          type="checkbox"
                          [checked]="selectedOrderIds().has(ord.id)"
                          (click)="$event.stopPropagation()"
                          (change)="toggleOrderSelection(ord.id)"
                          class="mt-0.5 rounded text-[#635BFF] focus:ring-[#635BFF]" />
                        <div class="flex-1 min-w-0">
                          <div class="flex items-center justify-between">
                            <span class="font-semibold text-[#1A2C44]">{{ ord.orderNumber }}</span>
                            <span class="text-[10px] px-1.5 py-0.5 rounded font-medium"
                              [ngClass]="ord.priority === 'Critical' ? 'bg-rose-50 text-rose-700 border border-rose-200' : 'bg-[#F8FAFC] text-[#4F5B66] border border-[#E3E8EE]'">
                              {{ ord.priority === 'Critical' ? 'Prioritario' : 'Ordinario' }}
                            </span>
                          </div>
                          <p class="text-[12px] font-medium text-[#635BFF] mt-0.5">Destino: {{ ord.destinationName }}</p>
                          <div class="mt-1 space-y-0.5 text-[11px] text-[#4F5B66]">
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
                  <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Asignación de Almacenes por Proximidad</span>
                  <p class="text-[12px] text-[#4F5B66]">
                    El sistema verificó el stock con caducidad más próxima (FEFO) y seleccionó automáticamente el almacén disponible más cercano a cada destino:
                  </p>

                  @for (opt of routeOptions(); track opt.optionId) {
                    <div class="p-3.5 rounded-lg border border-[#E3E8EE] bg-white shadow-xs space-y-2 text-[12px]">
                      <span class="font-semibold text-[#1A2C44] block text-[13px]">{{ opt.title }}</span>
                      <p class="text-[12px] text-[#8792A2]">{{ opt.description }}</p>

                      <div class="border border-[#E3E8EE] rounded-md p-2.5 bg-[#F8FAFC] space-y-1.5">
                        @for (l of opt.sourcingLines; track l.orderId) {
                          <div class="border-b border-[#E3E8EE] pb-1.5 last:border-0 last:pb-0">
                            <div class="font-medium text-[#1A2C44]">{{ l.productName }} ({{ l.requestedQuantity }} u)</div>
                            <div class="text-[11px] text-[#8792A2]">Destino: {{ l.destinationName }}</div>
                            <div class="mt-1 text-[11px] text-emerald-700 font-medium">
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

              <!-- PASO 3: OPCIONES DE RUTA & REORDENAR PARADAS -->
              @if (wizardStep() === 3) {
                <div class="space-y-3">
                  <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Alternativas de Recorrido y Paradas</span>

                  <div class="grid grid-cols-2 gap-2">
                    @for (opt of routeOptions(); track opt.optionId) {
                      <div
                        (click)="selectedOptionId.set(opt.optionId); onOptionSelected(opt)"
                        class="p-3 rounded-lg border cursor-pointer text-[12px] transition-all"
                        [ngClass]="selectedOptionId() === opt.optionId ? 'border-[#635BFF] ring-2 ring-[#635BFF]/10 bg-white' : 'border-[#E3E8EE] bg-white hover:border-slate-300'">
                        <div class="flex items-center justify-between">
                          <span class="font-semibold text-[#1A2C44]">Opción {{ opt.optionId }}</span>
                          @if (opt.isRecommended) {
                            <span class="text-[10px] bg-emerald-50 text-emerald-700 border border-emerald-200 px-1.5 py-0.5 rounded font-medium">Recomendado</span>
                          }
                        </div>
                        <p class="text-[11px] text-[#8792A2] mt-1 line-clamp-2">{{ opt.title }}</p>
                        <div class="text-[11px] font-medium mt-2 text-[#30313D]">
                          {{ opt.evaluation.totalDistanceKm }} km · {{ opt.evaluation.totalDurationMinutes }} min aprox.
                        </div>
                      </div>
                    }
                  </div>

                  <!-- Lista interactiva de paradas con reordenamiento -->
                  <div class="bg-white p-3.5 rounded-lg border border-[#E3E8EE] shadow-xs space-y-2">
                    <div class="flex items-center justify-between">
                      <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Orden de Paradas ({{ editableStops().length }})</span>
                      <button (click)="restoreOptimalOrder()" class="text-[11px] text-[#635BFF] hover:underline font-medium">
                        Restaurar orden sugerido
                      </button>
                    </div>

                    <div class="space-y-1.5 max-h-[220px] overflow-y-auto pr-1">
                      @for (s of editableStops(); track s.sequence; let idx = $index) {
                        <div class="p-2 rounded-md border flex items-center justify-between text-[12px]"
                          [ngClass]="s.kind === 'Pickup' ? 'bg-amber-50/60 border-amber-200' : 'bg-[#F8FAFC] border-[#E3E8EE]'">
                          <div class="flex items-center gap-2 min-w-0">
                            <span class="w-5 h-5 rounded-full flex items-center justify-center font-bold text-[10px] text-white"
                              [ngClass]="s.kind === 'Pickup' ? 'bg-amber-600' : 'bg-[#635BFF]'">
                              {{ idx + 1 }}
                            </span>
                            <div class="min-w-0">
                              <p class="font-medium text-[#1A2C44] truncate">{{ s.name }}</p>
                              <p class="text-[11px] text-[#8792A2]">
                                {{ s.kind === 'Pickup' ? 'Recogida en almacén' : 'Entrega a centro de salud' }}
                              </p>
                            </div>
                          </div>

                          <div class="flex items-center gap-1">
                            @if (idx > 0) {
                              <button (click)="moveStop(idx, -1)" class="p-1 hover:bg-slate-200 rounded font-semibold text-[#4F5B66] text-[10px]" title="Subir parada">▲</button>
                            }
                            @if (idx < editableStops().length - 1) {
                              <button (click)="moveStop(idx, 1)" class="p-1 hover:bg-slate-200 rounded font-semibold text-[#4F5B66] text-[10px]" title="Bajar parada">▼</button>
                            }
                          </div>
                        </div>
                      }
                    </div>

                    @if (currentEvaluation()?.validationErrors?.length) {
                      <div class="p-2.5 bg-rose-50 border border-rose-200 rounded-md text-[11px] text-rose-700 space-y-0.5">
                        @for (err of currentEvaluation()!.validationErrors; track err) {
                          <div>Observación: {{ err }}</div>
                        }
                      </div>
                    }
                  </div>
                </div>
              }

              <!-- PASO 4: SELECCIÓN DE VEHÍCULO & CONFIRMACIÓN -->
              @if (wizardStep() === 4) {
                <div class="space-y-3">
                  <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Vehículos de Transporte Disponibles</span>
                  <p class="text-[12px] text-[#4F5B66]">
                    Se evaluó el perfil de carga en cada tramo para descartar vehículos que no cumplan con el peso o volumen requerido:
                  </p>

                  <div class="space-y-2">
                    @for (vm of availableVehicleMatches(); track vm.vehicleId) {
                      <div
                        (click)="vm.isMatch ? selectedVehicleId.set(vm.vehicleId) : null"
                        class="p-3.5 rounded-lg border text-[12px] transition-all"
                        [ngClass]="{
                          'border-[#635BFF] ring-2 ring-[#635BFF]/10 bg-white cursor-pointer': vm.isMatch && selectedVehicleId() === vm.vehicleId,
                          'border-[#E3E8EE] bg-white hover:border-slate-300 cursor-pointer': vm.isMatch && selectedVehicleId() !== vm.vehicleId,
                          'border-[#E3E8EE] bg-[#F8FAFC] opacity-60 cursor-not-allowed': !vm.isMatch
                        }">
                        <div class="flex items-center justify-between">
                          <span class="font-semibold text-[#1A2C44] text-[13px]">{{ vm.vehicleCode }}</span>
                          @if (vm.isMatch) {
                            <span class="text-[10px] bg-emerald-50 text-emerald-700 border border-emerald-200 font-medium px-2 py-0.5 rounded">Compatible</span>
                          } @else {
                            <span class="text-[10px] bg-rose-50 text-rose-700 border border-rose-200 font-medium px-2 py-0.5 rounded">No compatible</span>
                          }
                        </div>

                        <div class="mt-2 grid grid-cols-2 gap-2 text-[11px] text-[#4F5B66]">
                          <div>Capacidad de carga: <strong class="text-[#1A2C44]">{{ vm.maxWeight }} kg</strong></div>
                          <div>Uso estimado: <strong class="text-[#1A2C44]">{{ vm.weightUtilizationPct }}%</strong></div>
                          <div>Distancia inicial: <strong class="text-[#1A2C44]">{{ vm.distanceToFirstPickupKm }} km</strong></div>
                          <div>Costo operativo: <strong class="text-[#1A2C44]">USD {{ vm.estimatedCost }}</strong></div>
                        </div>

                        @if (!vm.isMatch) {
                          <p class="text-[11px] text-rose-700 mt-2 font-medium">
                            Motivo de descarte: {{ vm.discardReason }}
                          </p>
                        }
                      </div>
                    }
                  </div>
                </div>
              }

              <!-- Botones de Navegación del Asistente -->
              <div class="mt-auto pt-3 border-t border-[#E3E8EE] flex items-center justify-between gap-2">
                @if (wizardStep() > 1) {
                  <button
                    type="button"
                    (click)="stepBack()"
                    class="h-9 px-3.5 text-[13px] font-medium rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors">
                    ← Paso Anterior
                  </button>
                } @else {
                  <div></div>
                }

                @if (wizardStep() < 4) {
                  <button
                    type="button"
                    (click)="advanceWizardStep()"
                    [disabled]="selectedOrderIds().size === 0 || actionLoading()"
                    class="h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors disabled:opacity-50">
                    Siguiente Paso →
                  </button>
                } @else {
                  <button
                    type="button"
                    (click)="confirmAndSavePlan()"
                    [disabled]="!selectedVehicleId() || actionLoading()"
                    class="h-9 px-4 text-[13px] font-medium rounded-md bg-emerald-600 text-white hover:bg-emerald-700 shadow-xs transition-colors disabled:opacity-50">
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
                Trazado aproximado (rectas entre paradas): el servicio de cálculo de calles no se encuentra disponible.
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
        // #199: el servidor recalcula la carga y devuelve por línea lo planificado y lo que queda pendiente.
        const pendingText = describePendingLines(res?.lines);
        this.feedbackMessage.set({
          type: 'success',
          text: `¡Ruta guardada y asignada exitosamente con ${stops.length} paradas!`
            + (pendingText ? ` Carga parcial — ${pendingText}. El remanente queda pendiente en el pedido.` : '')
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
