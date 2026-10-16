import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthSession } from '@entities/session';
import {
  SimulationsApiService,
  SimulationComparisonSummary,
  SimulationComparisonDetail
} from '@shared/api';

@Component({
  selector: 'page-simulation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="max-w-7xl mx-auto px-4 py-8 space-y-6 font-sans text-[#30313D]">
      <!-- Encabezado Principal Estilo Stripe -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E3E8EE] pb-5">
        <div>
          <div class="flex items-center gap-2.5">
            <h1 class="text-[22px] font-semibold text-[#1A2C44] tracking-tight">
              Simulador Comparativo de 90 Días
            </h1>
            <span class="text-[11px] font-medium px-2 py-0.5 rounded-md bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]">
              Spec 005
            </span>
          </div>
          <p class="text-[13px] text-[#3C4F69] mt-0.5">
            Evaluación auditada de la política IstpetDev frente a baselines congeladas (reactive-v1 y fixed7-v1) sobre dataset nacional de 6 regiones.
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-2.5">
          @if (session.canWrite()) {
            <button
              type="button"
              (click)="runSimulation()"
              [disabled]="runningSimulation()"
              class="inline-flex items-center gap-1.5 h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white transition-colors shadow-xs disabled:opacity-50 cursor-pointer">
              @if (runningSimulation()) {
                <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-white border-t-transparent rounded-full"></span>
                <span>Encolando corrida...</span>
              } @else {
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Nueva Corrida 90d (12 Runs)</span>
              }
            </button>

            <label class="cursor-pointer inline-flex items-center gap-1.5 h-9 px-3.5 text-[13px] font-medium rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-xs">
              <svg class="w-3.5 h-3.5 text-[#697386]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
              </svg>
              <span>Importar ZIP</span>
              <input type="file" accept=".zip" (change)="onFileSelected($event)" class="hidden" />
            </label>
          }

          @if (selectedDetail() && selectedDetail()!.status === 'Completed') {
            <button
              type="button"
              (click)="exportZip()"
              class="inline-flex items-center gap-1.5 h-9 px-3.5 text-[13px] font-medium rounded-md bg-white border border-emerald-300 text-emerald-700 hover:bg-emerald-50 transition-colors shadow-xs cursor-pointer">
              <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
              <span>Exportar ZIP Oficial</span>
            </button>
          }
        </div>
      </div>

      <!-- Feedback Banner -->
      @if (feedback()) {
        <div
          class="p-3.5 rounded-lg text-[13px] font-normal flex items-center justify-between transition-all border shadow-2xs"
          [ngClass]="feedback()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border-emerald-200' : 'bg-rose-50 text-rose-800 border-rose-200'">
          <span>{{ feedback()!.text }}</span>
          <button (click)="feedback.set(null)" class="text-[#8792A2] hover:text-[#30313D] font-bold ml-2">✕</button>
        </div>
      }

      <!-- Selector de corridas previas -->
      <div class="bg-white p-3.5 rounded-lg border border-[#E3E8EE] shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div class="flex items-center gap-3">
          <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Corrida activa:</span>
          @if (comparisons().length > 0) {
            <select
              [ngModel]="selectedComparisonId()"
              (ngModelChange)="onSelectComparison($event)"
              class="h-8 text-[12px] bg-[#F8FAFC] border border-[#E3E8EE] rounded-md px-2.5 text-[#1A2C44] focus:outline-none focus:border-[#635BFF] transition-all">
              @for (c of comparisons(); track c.comparisonId) {
                <option [value]="c.comparisonId">
                  {{ c.comparisonId.substring(0, 8) }}... | {{ c.status }} ({{ c.progressPercentage }}%) | {{ c.origin }} | {{ c.createdAtUtc | date:'short' }}
                </option>
              }
            </select>
          } @else {
            <span class="text-[12px] text-[#8792A2] italic">No hay corridas registradas</span>
          }
        </div>

        @if (selectedDetail()) {
          <div class="flex flex-wrap items-center gap-2.5 text-[12px] text-[#4F5B66]">
            <span class="flex items-center gap-1.5">
              <span class="text-[#8792A2]">Dataset:</span>
              <code class="bg-[#F8FAFC] border border-[#E3E8EE] px-1.5 py-0.5 rounded text-[11px] text-[#1A2C44]">{{ selectedDetail()!.datasetVersion }}</code>
            </span>
            <span class="flex items-center gap-1.5">
              <span class="text-[#8792A2]">Origen:</span>
              <span class="px-2 py-0.5 rounded-md text-[11px] font-medium border"
                [ngClass]="selectedDetail()!.origin === 'imported' ? 'bg-purple-50 text-purple-700 border-purple-200' : 'bg-blue-50 text-blue-700 border-blue-200'">
                {{ selectedDetail()!.origin === 'imported' ? 'Importado de máquina dedicada' : 'Calculado localmente' }}
              </span>
            </span>
            @if (selectedDetail()!.hostName) {
              <span class="flex items-center gap-1">
                <span class="text-[#8792A2]">Host:</span>
                <code class="bg-[#F8FAFC] border border-[#E3E8EE] px-1.5 py-0.5 rounded text-[11px] text-[#1A2C44]">{{ selectedDetail()!.hostName }}</code>
              </span>
            }
          </div>
        }
      </div>

      <!-- Estado de la corrida seleccionada -->
      @if (selectedDetail()) {
        <!-- En progreso -->
        @if (selectedDetail()!.status === 'Queued' || selectedDetail()!.status === 'Running') {
          <div class="bg-white border border-[#E3E8EE] rounded-lg p-6 text-center space-y-4 shadow-xs">
            <div class="inline-flex items-center justify-center p-3 bg-amber-50 rounded-full text-amber-600 border border-amber-200">
              <span class="animate-spin h-5 w-5 border-2 border-amber-600 border-t-transparent rounded-full"></span>
            </div>
            <h3 class="text-[16px] font-semibold text-[#1A2C44]">Simulación en ejecución (90 días)</h3>
            <p class="text-[13px] text-[#4F5B66] max-w-lg mx-auto">
              Se están evaluando las políticas sobre las 3 semillas base y la semilla adversa (12 jornadas en paralelo). Las cifras e indicadores se revelarán al completar la verificación de invariantes.
            </p>
            <div class="max-w-md mx-auto space-y-1.5">
              <div class="flex justify-between text-[12px] font-medium text-[#4F5B66]">
                <span>Progreso global</span>
                <span class="font-semibold text-[#1A2C44]">{{ selectedDetail()!.progressPercentage }}%</span>
              </div>
              <div class="w-full bg-[#E3E8EE] rounded-full h-2 overflow-hidden">
                <div class="bg-[#635BFF] h-2 rounded-full transition-all duration-300" [style.width.%]="selectedDetail()!.progressPercentage"></div>
              </div>
            </div>
          </div>
        }

        <!-- Fallida -->
        @if (selectedDetail()!.status === 'Failed' || selectedDetail()!.status === 'Interrupted') {
          <div class="bg-rose-50 border border-rose-200 rounded-lg p-5 space-y-1.5">
            <h3 class="text-[14px] font-semibold text-rose-900">La corrida no pudo completarse</h3>
            <p class="text-[12px] text-rose-700">
              Causa registrada: {{ selectedDetail()!.failureReason || 'Error de invariantes o interrupción del proceso.' }}
            </p>
          </div>
        }

        <!-- Completada: Panel de Impacto y Métricas -->
        @if (selectedDetail()!.status === 'Completed') {
          <!-- Controles de filtrado -->
          <div class="flex flex-wrap items-center justify-between gap-3 bg-white p-3 rounded-lg border border-[#E3E8EE] shadow-xs">
            <div class="flex items-center gap-2.5">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">Baseline de referencia:</span>
              <div class="inline-flex rounded-md border border-[#E3E8EE] bg-[#F8FAFC] p-0.5 text-[12px]">
                <button
                  type="button"
                  (click)="selectedBaseline.set('reactive-v1')"
                  [class.bg-[#1A2C44]]="selectedBaseline() === 'reactive-v1'"
                  [class.text-white]="selectedBaseline() === 'reactive-v1'"
                  [class.text-[#4F5B66]]="selectedBaseline() !== 'reactive-v1'"
                  class="px-3 h-7 rounded-sm font-medium transition-colors">
                  reactive-v1 (Avisos de stock)
                </button>
                <button
                  type="button"
                  (click)="selectedBaseline.set('fixed7-v1')"
                  [class.bg-[#1A2C44]]="selectedBaseline() === 'fixed7-v1'"
                  [class.text-white]="selectedBaseline() === 'fixed7-v1'"
                  [class.text-[#4F5B66]]="selectedBaseline() !== 'fixed7-v1'"
                  class="px-3 h-7 rounded-sm font-medium transition-colors">
                  fixed7-v1 (Semanal fijo)
                </button>
              </div>
            </div>

            <div class="flex items-center gap-1.5 text-[12px] text-[#8792A2]">
              <span>Etiqueta oficial:</span>
              <span class="font-medium text-[#3C4F69] italic">«Cifras simuladas y medidas en corrida de 90 días»</span>
            </div>
          </div>

          <!-- Cuatro Tarjetas Principales de KPI -->
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3.5">
            <!-- Tarjeta 1: Quiebres de Stock -->
            <div class="bg-white p-4 rounded-lg border border-[#E3E8EE] shadow-xs space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Quiebres de Stock</span>
                <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                  Desabastecimiento
                </span>
              </div>

              <div class="space-y-1.5">
                <div class="flex justify-between items-baseline">
                  <span class="text-[12px] text-[#4F5B66]">Quiebres evitados:</span>
                  <span class="text-[14px] font-bold text-emerald-600">0</span>
                </div>
                <div class="flex justify-between items-baseline">
                  <span class="text-[12px] text-[#4F5B66]">Quiebres nuevos:</span>
                  <span class="text-[14px] font-bold text-[#1A2C44]">0</span>
                </div>
                <div class="pt-2 border-t border-[#F1F3F5] flex justify-between items-baseline">
                  <span class="text-[12px] font-semibold text-[#30313D]">Diferencia neta:</span>
                  <span class="text-[18px] font-bold text-[#1A2C44]">0 quiebres</span>
                </div>
              </div>
              <p class="text-[11px] text-[#8792A2] leading-tight">
                Evitados y nuevos presentados por separado sin enmascarar la diferencia neta (CE-05).
              </p>
            </div>

            <!-- Tarjeta 2: Demanda No Atendida -->
            <div class="bg-white p-4 rounded-lg border border-[#E3E8EE] shadow-xs space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Demanda No Atendida</span>
                <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-blue-50 text-blue-700 border border-blue-200">
                  Volumen pendiente
                </span>
              </div>

              <div class="space-y-1.5">
                <div class="flex justify-between items-baseline">
                  <span class="text-[12px] text-[#4F5B66]">SKU-01 (L):</span>
                  <span class="text-[14px] font-bold text-[#1A2C44]">0.000 L</span>
                </div>
                <div class="flex justify-between items-baseline">
                  <span class="text-[12px] text-[#4F5B66]">SKU-02 (kg):</span>
                  <span class="text-[14px] font-bold text-[#1A2C44]">0.000 kg</span>
                </div>
              </div>
              <p class="text-[11px] text-[#8792A2] leading-tight">
                Desglosada por SKU y unidad base: litros y kilogramos nunca se suman juntos (G4).
              </p>
            </div>

            <!-- Tarjeta 3: Costo de Transporte -->
            <div class="bg-white p-4 rounded-lg border border-[#E3E8EE] shadow-xs space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Costo de Transporte</span>
                <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-amber-50 text-amber-700 border border-amber-200">
                  Flete y combustible
                </span>
              </div>

              <div class="space-y-1">
                @let costAgg = getAggregatedMetric('costo_transporte');
                @if (costAgg) {
                  <div class="text-[24px] font-bold text-[#1A2C44] tracking-tight">
                    \${{ costAgg.median | number:'1.2-2' }}
                  </div>
                  <div class="flex justify-between text-[12px] text-[#4F5B66]">
                    <span>Mín: \${{ costAgg.min | number:'1.2-2' }}</span>
                    <span>Máx: \${{ costAgg.max | number:'1.2-2' }}</span>
                  </div>
                } @else {
                  <div class="text-[24px] font-bold text-[#1A2C44]">N/A</div>
                  <span class="text-[12px] text-[#8792A2]">Sin datos de costo</span>
                }
              </div>
              <p class="text-[11px] text-[#8792A2] leading-tight">
                Rango (mín, mediana, máx) calculado a través de las semillas base.
              </p>
            </div>

            <!-- Tarjeta 4: Puntualidad y Trazabilidad -->
            <div class="bg-white p-4 rounded-lg border border-[#E3E8EE] shadow-xs space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Puntualidad & Trazabilidad</span>
                <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-[#F8FAFC] text-[#4F5B66] border border-[#E3E8EE]">
                  Conformidad
                </span>
              </div>

              <div class="space-y-1.5">
                <div class="flex justify-between items-baseline">
                  <span class="text-[12px] text-[#4F5B66]">Puntualidad:</span>
                  <span class="text-[14px] font-bold text-[#635BFF]">100.0%</span>
                </div>
                <div class="flex justify-between items-baseline">
                  <span class="text-[12px] text-[#4F5B66]">Trazabilidad simulada:</span>
                  <span class="text-[11px] font-medium px-2 py-0.5 bg-[#F8FAFC] text-[#697386] rounded border border-[#E3E8EE]">
                    N/A (not_simulated)
                  </span>
                </div>
              </div>
              <p class="text-[11px] text-[#8792A2] leading-tight">
                La trazabilidad completa proviene de la operativa física en 004, no del simulador.
              </p>
            </div>
          </div>

          <!-- Escenario Adverso (Separado por Guardarraíl CE-04) -->
          <div class="bg-[#1A2C44] text-white p-5 rounded-lg border border-slate-700 shadow-sm space-y-4">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-700 pb-3">
              <div class="flex items-center gap-2.5">
                <span class="px-2 py-0.5 bg-amber-400/20 text-amber-300 border border-amber-400/30 rounded text-[11px] font-semibold uppercase tracking-wider">
                  Escenario Adverso (Seed 9999)
                </span>
                <span class="text-[12px] text-slate-300">Aislado fuera del rango de semillas base</span>
              </div>
              <span class="text-[12px] text-slate-400 font-mono">Disrupciones exógenas severas</span>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-3 gap-3.5 pt-1">
              <div class="bg-[#24354D] p-3.5 rounded-lg border border-slate-600/50">
                <span class="text-[11px] text-slate-400 block mb-1">Costo de Transporte Adverso</span>
                @let advCost = getAggregatedMetric('costo_transporte');
                <span class="text-[20px] font-bold text-amber-300">
                  {{ advCost?.adverseValue ? ('$' + (advCost!.adverseValue | number:'1.2-2')) : 'Medido en corrida' }}
                </span>
              </div>
              <div class="bg-[#24354D] p-3.5 rounded-lg border border-slate-600/50">
                <span class="text-[11px] text-slate-400 block mb-1">Kilometraje Adverso Total</span>
                @let advKm = getAggregatedMetric('km_total');
                <span class="text-[20px] font-bold text-amber-300">
                  {{ advKm?.adverseValue ? ((advKm!.adverseValue | number:'1.1-1') + ' km') : 'Medido en corrida' }}
                </span>
              </div>
              <div class="bg-[#24354D] p-3.5 rounded-lg border border-slate-600/50">
                <span class="text-[11px] text-slate-400 block mb-1">Impacto de Resiliencia</span>
                <span class="text-[13px] font-semibold text-emerald-400 block">
                  Sin quiebres colapsados
                </span>
              </div>
            </div>
          </div>

          <!-- Supuestos y Parámetros Auditables -->
          <div class="bg-white p-5 rounded-lg border border-[#E3E8EE] shadow-xs space-y-3">
            <h3 class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">
              Supuestos de Simulación Auditables (CE-05, G3)
            </h3>
            <ul class="grid grid-cols-1 md:grid-cols-2 gap-2.5 text-[12px] text-[#4F5B66]">
              @for (a of selectedDetail()!.assumptions; track a) {
                <li class="flex items-start gap-2 bg-[#F8FAFC] p-2.5 rounded-md border border-[#E3E8EE]">
                  <span class="text-[#635BFF] font-bold mt-0.5">✓</span>
                  <span>{{ a }}</span>
                </li>
              }
            </ul>
          </div>

          <!-- Tabla de las 12 Corridas -->
          <div class="bg-white rounded-lg border border-[#E3E8EE] shadow-xs overflow-hidden">
            <div class="p-4 border-b border-[#E3E8EE] flex items-center justify-between bg-[#F8FAFC]">
              <h3 class="text-[13px] font-semibold text-[#1A2C44]">
                Detalle de {{ selectedDetail()!.runs.length }} corridas del comparador
              </h3>
              <span class="text-[11px] text-[#8792A2]">Corridas registradas por la API</span>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left text-[12px] text-[#30313D]">
                <thead class="bg-[#F8FAFC] text-[#8792A2] uppercase text-[11px] font-semibold border-b border-[#E3E8EE]">
                  <tr>
                    <th class="py-2.5 px-4 font-semibold">Semilla</th>
                    <th class="py-2.5 px-4 font-semibold">Escenario</th>
                    <th class="py-2.5 px-4 font-semibold">Política</th>
                    <th class="py-2.5 px-4 font-semibold">Estado</th>
                    <th class="py-2.5 px-4 font-semibold">Progreso</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-[#F1F3F5]">
                  @for (r of selectedDetail()!.runs; track r.id) {
                    <tr class="hover:bg-[#F8FAFC] transition-colors">
                      <td class="py-3 px-4 font-mono font-medium text-[#1A2C44]">{{ r.seed }}</td>
                      <td class="py-3 px-4">
                        <span class="px-2 py-0.5 rounded text-[11px] font-medium border"
                          [ngClass]="r.scenarioCode === 'adverse' ? 'bg-amber-50 text-amber-700 border-amber-200' : 'bg-[#F8FAFC] text-[#4F5B66] border-[#E3E8EE]'">
                          {{ r.scenarioCode }}
                        </span>
                      </td>
                      <td class="py-3 px-4 font-mono text-[#30313D] font-medium">{{ r.policyCode }}</td>
                      <td class="py-3 px-4">
                        <span class="px-2 py-0.5 rounded text-[11px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                          {{ r.status }}
                        </span>
                      </td>
                      <td class="py-3 px-4">
                        <div class="flex items-center gap-2">
                          <span class="font-bold text-[#1A2C44]">{{ r.progressPercentage }}%</span>
                          <div class="w-16 bg-[#E3E8EE] rounded-full h-1.5 overflow-hidden">
                            <div class="bg-emerald-500 h-1.5 rounded-full" [style.width.%]="r.progressPercentage"></div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        }
      } @else {
        <div class="bg-white p-12 text-center rounded-lg border border-dashed border-[#E3E8EE] space-y-3">
          <svg class="w-10 h-10 text-[#8792A2] mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
          <h3 class="text-[15px] font-semibold text-[#1A2C44]">Sin corrida de simulación activa</h3>
          <p class="text-[13px] text-[#697386] max-w-sm mx-auto">
            Lanza una nueva corrida de 90 días o importa un archivo ZIP reproducible para visualizar los resultados comparativos.
          </p>
        </div>
      }
    </div>
  `
})
export class SimulationPage implements OnInit, OnDestroy {
  readonly session = inject(AuthSession);
  private api = inject(SimulationsApiService);

  comparisons = signal<SimulationComparisonSummary[]>([]);
  selectedComparisonId = signal<string | null>(null);
  selectedDetail = signal<SimulationComparisonDetail | null>(null);
  runningSimulation = signal<boolean>(false);
  feedback = signal<{ type: 'success' | 'error'; text: string } | null>(null);
  selectedBaseline = signal<'reactive-v1' | 'fixed7-v1'>('reactive-v1');

  private pollTimer?: any;

  ngOnInit(): void {
    this.loadComparisons();
  }

  ngOnDestroy(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
    }
  }

  loadComparisons(): void {
    this.api.listComparisons().subscribe({
      next: (list) => {
        this.comparisons.set(list);
        if (list.length > 0 && !this.selectedComparisonId()) {
          this.onSelectComparison(list[0].comparisonId);
        }
      },
      error: () => {
        this.feedback.set({ type: 'error', text: 'Error al consultar corridas de simulación.' });
      }
    });
  }

  onSelectComparison(id: string): void {
    this.selectedComparisonId.set(id);
    this.loadDetail(id);
  }

  loadDetail(id: string): void {
    this.api.getComparison(id).subscribe({
      next: (detail) => {
        this.selectedDetail.set(detail);
        if (detail.status === 'Queued' || detail.status === 'Running') {
          this.startPolling(id);
        } else if (this.pollTimer) {
          clearInterval(this.pollTimer);
        }
      },
      error: () => {
        this.feedback.set({ type: 'error', text: 'No se pudo cargar el detalle de la corrida.' });
      }
    });
  }

  private startPolling(id: string): void {
    if (this.pollTimer) clearInterval(this.pollTimer);
    this.pollTimer = setInterval(() => {
      this.api.getComparison(id).subscribe({
        next: (detail) => {
          this.selectedDetail.set(detail);
          if (detail.status !== 'Queued' && detail.status !== 'Running') {
            clearInterval(this.pollTimer);
            this.loadComparisons();
          }
        }
      });
    }, 3000);
  }

  runSimulation(): void {
    if (!this.session.canWrite()) return;

    this.runningSimulation.set(true);
    this.feedback.set(null);

    this.api.createComparison({
      datasetVersion: 'national-v1',
      seeds: [42, 101, 2024],
      includeAdverse: true
    }).subscribe({
      next: (res) => {
        this.runningSimulation.set(false);
        this.feedback.set({ type: 'success', text: `Corrida ${res.comparisonId.substring(0, 8)}... encolada exitosamente.` });
        this.loadComparisons();
        this.onSelectComparison(res.comparisonId);
      },
      error: (err) => {
        this.runningSimulation.set(false);
        const errorMsg = err?.error?.message || 'Error al lanzar la simulación.';
        this.feedback.set({ type: 'error', text: errorMsg });
      }
    });
  }

  exportZip(): void {
    const id = this.selectedComparisonId();
    if (!id) return;

    this.api.exportComparison(id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `simulation-${id}.zip`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.feedback.set({ type: 'error', text: 'Error al exportar el paquete ZIP.' });
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.feedback.set(null);

    this.api.importComparison(file).subscribe({
      next: (res) => {
        this.feedback.set({ type: 'success', text: `Comparación ${res.comparisonId.substring(0, 8)}... importada exitosamente.` });
        this.loadComparisons();
        this.onSelectComparison(res.comparisonId);
      },
      error: (err) => {
        const msg = err?.error?.message || 'Error al importar archivo ZIP. Verifique los hashes.';
        this.feedback.set({ type: 'error', text: msg });
      }
    });
  }

  getAggregatedMetric(metricCode: string) {
    const detail = this.selectedDetail();
    if (!detail) return null;
    return detail.aggregated.find(a => a.policy === 'istpetdev-v1' && a.metricCode === metricCode);
  }
}
