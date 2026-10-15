import { Component, inject, signal, computed, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import {
  ApiService,
  RoutePlanSummaryDto,
  RoutePlanDetailDto,
  DeliveryOrderDto,
  RouteSuggestionOptionDto,
  RouteCandidateStop,
  ServicePoint,
  AgentRunViewDto
} from '@shared/api';
import { MapViewComponent, MapMarker, MapPolyline } from '@shared/ui';

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
                      <button
                        type="button"
                        (click)="publishPlan(selectedDetail()!.id)"
                        [disabled]="actionLoading()"
                        class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-50">
                        {{ actionLoading() ? 'Publicando...' : 'Publicar a Conductor' }}
                      </button>
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

                <!-- Itinerario de Paradas (Pickups & Deliveries) -->
                <div>
                  <div class="flex items-center justify-between mb-2">
                    <h4 class="text-xs font-bold text-slate-700 uppercase">
                      Itinerario Detallado y Tiempos de Paradas
                    </h4>
                    <span class="text-[11px] text-slate-500 font-mono">
                      {{ selectedDetail()!.stops.length }} paradas programadas
                    </span>
                  </div>
                  <div class="border border-slate-200 rounded-lg overflow-x-auto">
                    <table class="w-full text-left text-xs min-w-[550px]">
                      <thead class="bg-slate-50 text-[11px] text-slate-500 uppercase border-b border-slate-200">
                        <tr>
                          <th class="p-2.5">#</th>
                          <th class="p-2.5">Tipo</th>
                          <th class="p-2.5">Punto / Hospital</th>
                          <th class="p-2.5">Ventana Atención</th>
                          <th class="p-2.5">Llegada ETA</th>
                          <th class="p-2.5">Tiempo Atención</th>
                          <th class="p-2.5 text-right">Estado</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-slate-100">
                        @for (stop of selectedDetail()!.stops; track stop.sequence) {
                          <tr>
                            <td class="p-2.5 font-bold font-mono">{{ stop.sequence }}</td>
                            <td class="p-2.5 font-bold">
                              <span class="px-2 py-0.5 rounded text-[10px]"
                                [ngClass]="stop.sequence === 1 ? 'bg-amber-100 text-amber-800' : 'bg-blue-100 text-blue-800'">
                                {{ stop.sequence === 1 ? 'RECOGIDA' : 'ENTREGA' }}
                              </span>
                            </td>
                            <td class="p-2.5 font-semibold text-slate-800">
                              {{ stop.servicePointName || stop.servicePointCode }}
                            </td>
                            <td class="p-2.5 font-mono text-slate-500 text-[11px]">
                              {{ stop.windowStart || '08:00' }} - {{ stop.windowEnd || '17:00' }}
                            </td>
                            <td class="p-2.5 font-mono font-bold text-primary-700">
                              {{ stop.etaUtc ? (stop.etaUtc | date:'shortTime') : '08:30' }}
                            </td>
                            <td class="p-2.5 font-mono text-slate-600">
                              {{ stop.serviceMinutes || 15 }} min
                            </td>
                            <td class="p-2.5 text-right font-mono text-[11px]">
                              <span class="px-2 py-0.5 rounded text-[10px] font-semibold"
                                [ngClass]="stop.status === 'Completed' ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'">
                                {{ stop.status }}
                              </span>
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
          </div>
        </div>
      </div>
    }
  `
})
export class RutasPage implements OnInit {
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

  ngOnInit(): void {
    this.loadPlans();
    this.loadPendingOrders();
    this.loadCatalogData();
  }

  loadPlans(): void {
    this.loading.set(true);
    this.apiService.getRoutePlans().subscribe({
      next: (plans) => {
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
      },
      error: () => this.loading.set(false)
    });
  }

  selectPlan(id: string): void {
    this.selectedPlanId.set(id);
    this.apiService.getRoutePlanById(id).subscribe({
      next: (detail) => {
        this.selectedDetail.set(detail);
        this.buildMap(detail);
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

  publishPlan(id: string): void {
    if (!this.session.canWrite()) return;
    this.actionLoading.set(true);
    this.apiService.publishRoutePlan(id).subscribe({
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
          text: err?.error?.message || 'Error al publicar la ruta.'
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

    this.updateWizardMap(opt.stops, opt.evaluation.coordinates);
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
      this.updateWizardMap(stops, evalRes.coordinates);
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

  private updateWizardMap(stops: RouteCandidateStop[], coordinates?: [number, number][]): void {
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
      this.wizardPolylines.set([{
        id: 'wizard-road-route',
        coordinates,
        color: '#00A7C4',
        weight: 5
      }]);
    } else {
      this.wizardPolylines.set([]);
    }
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
