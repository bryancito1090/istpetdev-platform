import { Component, computed, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthSession } from '@entities/session';
import {
  SimulationsApiService,
  SimulationComparisonSummary,
  SimulationComparisonDetail,
  SimulationRunSummaryDto,
  AggregatedMetricSummary
} from '@shared/api';

type PolicyCode = 'istpetdev-v1' | 'reactive-v1' | 'fixed7-v1';
type BaselineCode = 'reactive-v1' | 'fixed7-v1';
type MetricDirection = 'lower' | 'higher';

interface PolicyDefinition {
  code: PolicyCode;
  label: string;
  description: string;
  chipClass: string;
  barClass: string;
  dotClass: string;
}

interface ComparisonRow {
  code: string;
  label: string;
  unit: string;
  direction: MetricDirection;
  hint: string;
}

interface MetricComparison {
  platform: number;
  baseline: number;
  absolute: number;
  percent: number | null;
  favorable: boolean;
}

interface RunGroup {
  key: string;
  seed: number;
  scenario: string;
  runs: SimulationRunSummaryDto[];
}

@Component({
  selector: 'page-simulation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="max-w-7xl mx-auto px-4 py-8 space-y-5 font-sans text-[#30313D]">
      <!-- Encabezado -->
      <div class="flex flex-col sm:flex-row sm:items-end justify-between gap-4 border-b border-[#E3E8EE] pb-5">
        <div>
          <p class="text-[11px] font-semibold uppercase tracking-[0.16em] text-[#00A7C4]">
            Evaluación comparativa · 90 días
          </p>
          <h1 class="text-[26px] font-semibold text-[#1A2C44] tracking-tight mt-1">
            Simulador de 90 días
          </h1>
          <p class="text-[13px] text-[#3C4F69] mt-1 max-w-2xl">
            Compara IstpetDev con dos prácticas de reposición sobre el mismo dataset, calendario y recursos.
            Las cifras son simuladas y corresponden a la corrida seleccionada.
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            (click)="loadComparisons()"
            class="inline-flex items-center gap-1.5 h-9 px-3 text-[12px] font-medium rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs cursor-pointer">
            <svg class="w-3.5 h-3.5 text-[#697386]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            Actualizar
          </button>

          @if (session.canWrite()) {
            <button
              type="button"
              (click)="runSimulation()"
              [disabled]="runningSimulation()"
              class="inline-flex items-center gap-1.5 h-9 px-4 text-[13px] font-medium rounded-md bg-[#635BFF] hover:bg-[#5248E0] text-white transition-colors shadow-2xs disabled:opacity-50 cursor-pointer">
              @if (runningSimulation()) {
                <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-white border-t-transparent rounded-full"></span>
                <span>Encolando corrida...</span>
              } @else {
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Nueva corrida</span>
              }
            </button>

            <label class="cursor-pointer inline-flex items-center gap-1.5 h-9 px-3.5 text-[13px] font-medium rounded-md bg-white border border-[#E3E8EE] text-[#30313D] hover:bg-[#F8FAFC] transition-colors shadow-2xs">
              <svg class="w-3.5 h-3.5 text-[#697386]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
              </svg>
              <span>Importar ZIP</span>
              <input type="file" accept=".zip" (change)="onFileSelected($event)" class="hidden" />
            </label>
          }

          @if (selectedDetail()?.status === 'Completed') {
            <button
              type="button"
              (click)="exportZip()"
              class="inline-flex items-center gap-1.5 h-9 px-3.5 text-[13px] font-medium rounded-md bg-white border border-emerald-300 text-emerald-700 hover:bg-emerald-50 transition-colors shadow-2xs cursor-pointer">
              <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
              <span>Exportar ZIP</span>
            </button>
          }
        </div>
      </div>

      <!-- Feedback -->
      @if (feedback()) {
        <div
          class="p-3.5 rounded-lg text-[13px] flex items-center justify-between border shadow-2xs"
          [ngClass]="feedback()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border-emerald-200' : 'bg-rose-50 text-rose-800 border-rose-200'">
          <span>{{ feedback()!.text }}</span>
          <button type="button" (click)="feedback.set(null)" class="text-[#8792A2] hover:text-[#30313D] font-bold ml-2" aria-label="Cerrar aviso">✕</button>
        </div>
      }

      <!-- Corrida activa -->
      <div class="bg-white p-3.5 rounded-lg border border-[#E3E8EE] shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div class="flex items-center gap-3 flex-wrap">
          <span class="text-[11px] font-semibold text-[#8792A2] uppercase tracking-wider">Corrida activa</span>
          @if (comparisons().length > 0) {
            <select
              [ngModel]="selectedComparisonId()"
              (ngModelChange)="onSelectComparison($event)"
              class="h-8 text-[12px] bg-[#F8FAFC] border border-[#E3E8EE] rounded-md px-2.5 text-[#1A2C44] focus:outline-none focus:border-[#635BFF] transition-all max-w-full">
              @for (c of comparisons(); track c.comparisonId) {
                <option [value]="c.comparisonId">
                  {{ c.comparisonId.substring(0, 8) }}... · {{ statusLabel(c.status) }} · {{ c.datasetVersion }} · {{ c.createdAtUtc | date:'short' }}
                </option>
              }
            </select>
          } @else {
            <span class="text-[12px] text-[#8792A2] italic">No hay corridas registradas</span>
          }
        </div>

        @if (selectedDetail()) {
          <div class="flex flex-wrap items-center gap-2 text-[11px] text-[#4F5B66]">
            <span class="px-2 py-0.5 rounded-md border font-medium" [ngClass]="statusChipClass(selectedDetail()!.status)">
              {{ statusLabel(selectedDetail()!.status) }}
            </span>
            <span class="text-[#8792A2]">Dataset:</span>
            <code class="bg-[#F8FAFC] border border-[#E3E8EE] px-1.5 py-0.5 rounded text-[11px] text-[#1A2C44]">{{ selectedDetail()!.datasetVersion }}</code>
            @if (selectedDetail()!.datasetHash) {
              <code class="bg-[#F8FAFC] border border-[#E3E8EE] px-1.5 py-0.5 rounded text-[10px] text-[#697386]" [title]="selectedDetail()!.datasetHash!">
                #{{ selectedDetail()!.datasetHash!.substring(0, 10) }}
              </code>
            }
            <span class="text-[#8792A2]">Origen:</span>
            <span class="font-medium">{{ selectedDetail()!.origin === 'imported' ? 'Importado' : 'Calculado localmente' }}</span>
          </div>
        }
      </div>

      <!-- Estados -->
      @if (selectedDetail()) {
        @if (selectedDetail()!.status === 'Queued' || selectedDetail()!.status === 'Running') {
          <div class="bg-white border border-[#E3E8EE] rounded-xl p-6 text-center space-y-4 shadow-xs">
            <div class="inline-flex items-center justify-center p-3 bg-amber-50 rounded-full text-amber-600 border border-amber-200">
              <span class="animate-spin h-5 w-5 border-2 border-amber-600 border-t-transparent rounded-full"></span>
            </div>
            <div>
              <h3 class="text-[16px] font-semibold text-[#1A2C44]">Simulación en ejecución</h3>
              <p class="text-[13px] text-[#4F5B66] max-w-lg mx-auto mt-1">
                Se están evaluando las políticas sobre las semillas base y el escenario adverso.
                Las cifras se revelan al completar la verificación de invariantes.
              </p>
            </div>
            <div class="max-w-md mx-auto space-y-1.5">
              <div class="flex justify-between text-[12px] font-medium text-[#4F5B66]">
                <span>Progreso global</span>
                <span class="font-semibold text-[#1A2C44]">{{ selectedDetail()!.progressPercentage }}%</span>
              </div>
              <div class="w-full bg-[#E3E8EE] rounded-full h-2 overflow-hidden" role="progressbar" [attr.aria-valuenow]="selectedDetail()!.progressPercentage" aria-valuemin="0" aria-valuemax="100">
                <div class="bg-[#635BFF] h-2 rounded-full transition-all duration-300" [style.width.%]="selectedDetail()!.progressPercentage"></div>
              </div>
            </div>
          </div>
        }

        @if (selectedDetail()!.status === 'Failed' || selectedDetail()!.status === 'Interrupted') {
          <div class="bg-rose-50 border border-rose-200 rounded-xl p-5 space-y-1.5">
            <h3 class="text-[14px] font-semibold text-rose-900">La corrida no pudo completarse</h3>
            <p class="text-[12px] text-rose-700">
              Causa registrada: {{ selectedDetail()!.failureReason || 'Error de invariantes o interrupción del proceso.' }}
            </p>
          </div>
        }

        @if (selectedDetail()!.status === 'Completed') {
          <!-- Selector de baseline y etiqueta oficial -->
          <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-3 bg-white p-3.5 rounded-lg border border-[#E3E8EE] shadow-xs">
            <div class="flex flex-wrap items-center gap-2.5">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">Comparar contra</span>
              <div class="inline-flex rounded-md border border-[#E3E8EE] bg-[#F8FAFC] p-0.5 text-[12px]" role="group" aria-label="Política de referencia">
                @for (baseline of baselineOptions; track baseline.code) {
                  <button
                    type="button"
                    (click)="selectedBaseline.set(baseline.code)"
                    [attr.aria-pressed]="selectedBaseline() === baseline.code"
                    class="px-3 h-8 rounded-sm font-medium transition-colors cursor-pointer"
                    [ngClass]="selectedBaseline() === baseline.code ? 'bg-[#1A2C44] text-white shadow-2xs' : 'text-[#4F5B66] hover:text-[#1A2C44]'">
                    {{ baseline.label }}
                  </button>
                }
              </div>
            </div>

            <div class="flex flex-wrap items-center gap-2 text-[11px] text-[#697386]">
              <span class="inline-flex items-center gap-1.5 px-2 py-1 rounded-md bg-[#F8FAFC] border border-[#E3E8EE]">
                <span class="w-1.5 h-1.5 rounded-full bg-[#00A7C4]"></span>
                Cifras simuladas y medidas en esta corrida
              </span>
              <span>{{ completedRuns() }}/{{ totalRuns() }} corridas completadas</span>
            </div>
          </div>

          <!-- Comparación principal -->
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-3.5">
            @let costComparison = comparison('costo_transporte', 'lower');
            <div class="lg:col-span-2 rounded-xl border border-[#1A2C44] bg-gradient-to-br from-[#0A2540] to-[#1A2C44] text-white p-5 shadow-sm">
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-300">IstpetDev frente a {{ baselineLabel() }}</p>
                  @if (costComparison) {
                    <div class="mt-2 flex items-baseline gap-2">
                      <span class="text-[32px] font-semibold tracking-tight" [ngClass]="costComparison.favorable ? 'text-emerald-300' : 'text-amber-300'">
                        {{ formatSigned(costComparison.absolute, 'USD') }}
                      </span>
                      <span class="text-[12px] text-slate-300">en costo de transporte</span>
                    </div>
                    <p class="text-[12px] text-slate-300 mt-1">
                      {{ formatDelta(costComparison) }}
                    </p>
                  } @else {
                    <p class="text-[22px] font-semibold text-slate-300 mt-2">Sin dato</p>
                    <p class="text-[12px] text-slate-400 mt-1">La corrida no publicó el costo para ambas políticas.</p>
                  }
                </div>
                <span class="hidden sm:inline-flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-cyan-300 border border-cyan-400/30 bg-cyan-400/10 rounded-full px-2.5 py-1">
                  <span class="w-1.5 h-1.5 rounded-full bg-cyan-300"></span>
                  Simulación
                </span>
              </div>

              <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 mt-5 pt-4 border-t border-white/10">
                @for (row of comparisonRows; track row.code) {
                  @let policyMetric = metric('istpetdev-v1', row.code);
                  <div>
                    <p class="text-[10px] uppercase tracking-wider text-slate-400">{{ row.label }}</p>
                    <p class="text-[17px] font-semibold text-white mt-0.5">
                      {{ policyMetric?.median != null ? formatMetric(policyMetric!.median!, row.unit) : 'Sin dato' }}
                    </p>
                    @if (policyMetric?.min != null && policyMetric?.max != null) {
                      <p class="text-[10px] text-slate-400">Rango {{ formatMetric(policyMetric!.min!, row.unit) }} – {{ formatMetric(policyMetric!.max!, row.unit) }}</p>
                    }
                  </div>
                }
              </div>
            </div>

            <div class="rounded-xl border border-[#E3E8EE] bg-white p-4 shadow-xs space-y-3" data-testid="service-card">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">Calidad de servicio</span>
                <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-[#F8FAFC] text-[#697386] border border-[#E3E8EE]">Demanda</span>
              </div>

              @let punctuality = metric('istpetdev-v1', 'puntualidad');
              <div class="flex items-end justify-between gap-3">
                <div>
                  <p class="text-[11px] text-[#697386]">Puntualidad de demanda</p>
                  <p class="text-[28px] font-semibold text-[#1A2C44] tracking-tight">
                    {{ punctuality?.median != null ? formatNumber(punctuality!.median!, 1) + ' %' : 'Sin dato' }}
                  </p>
                </div>
                @if (punctuality?.adverseValue != null) {
                  <span class="text-[10px] font-medium text-amber-700 bg-amber-50 border border-amber-200 rounded px-2 py-1">
                    Adverso {{ formatNumber(punctuality!.adverseValue!, 1) }} %
                  </span>
                }
              </div>
              @if (punctualityDetail()) {
                <p class="text-[11px] text-[#8792A2]">
                  {{ punctualityDetail()!.numerator | number:'1.0-0' }} líneas atendidas de
                  {{ punctualityDetail()!.denominator | number:'1.0-0' }} solicitadas.
                </p>
              } @else {
                <p class="text-[11px] text-[#8792A2]">La corrida no publicó líneas de demanda.</p>
              }

              <div class="pt-2 border-t border-[#F1F3F5] text-[11px] text-[#697386] space-y-1">
                <p class="flex items-center justify-between gap-2">
                  <span>Trazabilidad</span>
                  <span class="font-medium text-[#4F5B66]">No simulada</span>
                </p>
                <p>La trazabilidad operativa proviene de la operación física, no del simulador.</p>
              </div>
            </div>
          </div>

          <!-- Tarjetas KPI -->
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-3.5">
            <div class="bg-white p-4 rounded-xl border border-[#E3E8EE] shadow-xs space-y-3" data-testid="stockout-card">
              <div class="flex items-center justify-between gap-2">
                <span class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">Quiebres de stock</span>
                <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">Comparado</span>
              </div>

              @let avoided = metric('istpetdev-v1', 'quiebres_evitados_' + selectedBaseline());
              @let added = metric('istpetdev-v1', 'quiebres_nuevos_' + selectedBaseline());
              @let net = metric('istpetdev-v1', 'quiebres_diferencia_neta_' + selectedBaseline());

              @if (avoided?.median != null || added?.median != null || net?.median != null) {
                <div class="space-y-1.5">
                  <div class="flex items-baseline justify-between gap-2">
                    <span class="text-[12px] text-[#4F5B66]">Evitados</span>
                    <span class="text-[16px] font-semibold text-emerald-600">{{ avoided?.median != null ? formatMetric(avoided!.median!, 'quiebres') : 'Sin dato' }}</span>
                  </div>
                  <div class="flex items-baseline justify-between gap-2">
                    <span class="text-[12px] text-[#4F5B66]">Nuevos</span>
                    <span class="text-[16px] font-semibold text-rose-600">{{ added?.median != null ? formatMetric(added!.median!, 'quiebres') : 'Sin dato' }}</span>
                  </div>
                  <div class="pt-2 border-t border-[#F1F3F5] flex items-baseline justify-between gap-2">
                    <span class="text-[12px] font-semibold text-[#30313D]">Diferencia neta</span>
                    <span class="text-[19px] font-bold text-[#1A2C44]">{{ net?.median != null ? formatSigned(net!.median!, 'quiebres') : 'Sin dato' }}</span>
                  </div>
                </div>
                <p class="text-[10px] text-[#8792A2]">
                  Evitados y nuevos se muestran por separado. Adverso:
                  {{ avoided?.adverseValue != null ? formatMetric(avoided!.adverseValue!, 'quiebres') : 'sin dato' }} evitados,
                  {{ added?.adverseValue != null ? formatMetric(added!.adverseValue!, 'quiebres') : 'sin dato' }} nuevos.
                </p>
              } @else {
                <div class="py-4">
                  <p class="text-[18px] font-semibold text-[#1A2C44]">Sin dato</p>
                  <p class="text-[11px] text-[#8792A2] mt-1">La corrida no publicó quiebres comparados para esta política.</p>
                </div>
              }
            </div>

            <div class="bg-white p-4 rounded-xl border border-[#E3E8EE] shadow-xs space-y-3" data-testid="unattended-card">
              <div class="flex items-center justify-between gap-2">
                <span class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">Demanda no atendida</span>
                <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-200">Por SKU</span>
              </div>

              @if (unattendedRows().length > 0) {
                <div class="space-y-2">
                  @for (row of unattendedRows(); track row.sku) {
                    <div class="flex items-center justify-between gap-2 text-[12px]">
                      <span class="text-[#4F5B66]">{{ row.sku }} <span class="text-[10px] text-[#8792A2]">({{ row.unit }})</span></span>
                      <span class="font-semibold text-[#1A2C44] text-right">
                        {{ row.median != null ? formatMetric(row.median!, row.unit) : 'Sin dato' }}
                        @if (row.adverse != null) {
                          <span class="block text-[10px] font-normal text-amber-700">Adverso {{ formatMetric(row.adverse!, row.unit) }}</span>
                        }
                      </span>
                    </div>
                  }
                </div>
                <p class="text-[10px] text-[#8792A2]">Litros y kilogramos se presentan por separado; nunca se suman.</p>
              } @else {
                <div class="py-4">
                  <p class="text-[18px] font-semibold text-[#1A2C44]">Sin dato</p>
                  <p class="text-[11px] text-[#8792A2] mt-1">La corrida no publicó demanda no atendida por SKU.</p>
                </div>
              }
            </div>

            <div class="bg-white p-4 rounded-xl border border-[#E3E8EE] shadow-xs space-y-3" data-testid="cost-card">
              <div class="flex items-center justify-between gap-2">
                <span class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">Costo de transporte</span>
                <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-amber-50 text-amber-700 border border-amber-200">Flete y combustible</span>
              </div>

              @let costMetric = metric('istpetdev-v1', 'costo_transporte');
              @let costDelta = comparison('costo_transporte', 'lower');
              @if (costMetric?.median != null) {
                <p class="text-[26px] font-semibold text-[#1A2C44] tracking-tight">{{ formatMetric(costMetric!.median!, 'USD') }}</p>
                <p class="text-[11px] text-[#8792A2]">
                  Rango {{ formatMetric(costMetric!.min!, 'USD') }} – {{ formatMetric(costMetric!.max!, 'USD') }} en semillas base.
                </p>
                @if (costDelta) {
                  <p class="text-[11px] font-medium" [ngClass]="costDelta.favorable ? 'text-emerald-700' : 'text-amber-700'">
                    {{ formatDelta(costDelta) }}.
                  </p>
                }
              } @else {
                <p class="text-[18px] font-semibold text-[#1A2C44]">Sin dato</p>
                <p class="text-[11px] text-[#8792A2] mt-1">La corrida no publicó costo de transporte.</p>
              }
            </div>

            <div class="bg-white p-4 rounded-xl border border-[#E3E8EE] shadow-xs space-y-3">
              <div class="flex items-center justify-between gap-2">
                <span class="text-[11px] font-semibold uppercase tracking-wider text-[#8792A2]">Entregas y kilometraje</span>
                <span class="text-[10px] font-medium px-2 py-0.5 rounded bg-[#F8FAFC] text-[#4F5B66] border border-[#E3E8EE]">Operación</span>
              </div>
              @let deliveries = metric('istpetdev-v1', 'entregas_completadas');
              @let km = metric('istpetdev-v1', 'km_total');
              <div class="space-y-2">
                <div class="flex items-baseline justify-between gap-2">
                  <span class="text-[12px] text-[#4F5B66]">Entregas completadas</span>
                  <span class="text-[15px] font-semibold text-[#1A2C44]">{{ deliveries?.median != null ? formatMetric(deliveries!.median!, 'entregas') : 'Sin dato' }}</span>
                </div>
                <div class="flex items-baseline justify-between gap-2">
                  <span class="text-[12px] text-[#4F5B66]">Kilometraje total</span>
                  <span class="text-[15px] font-semibold text-[#1A2C44]">{{ km?.median != null ? formatMetric(km!.median!, 'km') : 'Sin dato' }}</span>
                </div>
                @if (kmDelta()) {
                  <p class="text-[11px] text-[#8792A2]">{{ formatDelta(kmDelta()!, 'km') }} en recorrido.</p>
                }
              </div>
            </div>
          </div>

          <!-- Comparación visual -->
          <div class="bg-white rounded-xl border border-[#E3E8EE] shadow-xs p-5 space-y-4">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
              <div>
                <h2 class="text-[14px] font-semibold text-[#1A2C44]">Comparación por política</h2>
                <p class="text-[11px] text-[#697386] mt-0.5">Cada política mueve un volumen distinto; compara costo y servicio en conjunto.</p>
              </div>
              <span class="text-[10px] font-medium text-[#8792A2]">Mediana de semillas base · rango mínimo–máximo</span>
            </div>

            @for (row of comparisonRows; track row.code) {
              <div class="rounded-lg border border-[#E3E8EE] p-3.5 space-y-2.5">
                <div class="flex items-baseline justify-between gap-2">
                  <div>
                    <p class="text-[12px] font-semibold text-[#1A2C44]">{{ row.label }}</p>
                    <p class="text-[10px] text-[#8792A2]">{{ row.hint }}</p>
                  </div>
                  <span class="text-[10px] font-medium text-[#697386] uppercase tracking-wider">{{ row.unit }}</span>
                </div>

                @for (policy of policyDefinitions; track policy.code) {
                  @let value = metric(policy.code, row.code);
                  <div class="grid grid-cols-[92px_minmax(0,1fr)_88px] items-center gap-2.5">
                    <span class="flex items-center gap-1.5 text-[11px] font-medium text-[#4F5B66] min-w-0">
                      <span class="w-1.5 h-1.5 rounded-full shrink-0" [ngClass]="policy.dotClass"></span>
                      <span class="truncate">{{ policy.label }}</span>
                    </span>
                    <div class="h-2.5 rounded-full bg-[#EDF1F5] overflow-hidden" [attr.aria-label]="policy.label + ': ' + (value?.median != null ? formatMetric(value!.median!, row.unit) : 'sin dato')">
                      <div class="h-2.5 rounded-full transition-all duration-500 motion-reduce:transition-none" [ngClass]="policy.barClass" [style.width.%]="barWidth(policy.code, row.code)"></div>
                    </div>
                    <span class="text-right text-[12px] font-semibold text-[#1A2C44] tabular-nums">
                      {{ value?.median != null ? formatMetric(value!.median!, row.unit) : 'Sin dato' }}
                    </span>
                  </div>
                  @if (value?.min != null && value?.max != null) {
                    <p class="text-[10px] text-[#8792A2] pl-[102px] -mt-1">
                      {{ policy.description }} · rango {{ formatMetric(value!.min!, row.unit) }} – {{ formatMetric(value!.max!, row.unit) }}
                    </p>
                  }
                }
              </div>
            }
          </div>

          <!-- Escenario adverso -->
          <div class="bg-[#1A2C44] text-white p-5 rounded-xl border border-slate-700 shadow-sm space-y-4" data-testid="adverse-card">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-700 pb-3">
              <div class="flex items-center gap-2.5 flex-wrap">
                <span class="px-2 py-0.5 bg-amber-400/20 text-amber-300 border border-amber-400/30 rounded text-[11px] font-semibold uppercase tracking-wider">
                  Escenario adverso · semilla {{ adverseSeed() }}
                </span>
                <span class="text-[12px] text-slate-300">Aislado del rango de semillas base</span>
              </div>
              <span class="text-[11px] text-slate-400">Demanda +20 % · un vehículo menos · lead time +1 día</span>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
              <div class="bg-[#24354D] p-3.5 rounded-lg border border-slate-600/50">
                <span class="text-[11px] text-slate-400 block mb-1">Costo de transporte</span>
                <span class="text-[20px] font-semibold text-amber-300">{{ adverseMetric('costo_transporte', 'USD') }}</span>
              </div>
              <div class="bg-[#24354D] p-3.5 rounded-lg border border-slate-600/50">
                <span class="text-[11px] text-slate-400 block mb-1">Kilometraje total</span>
                <span class="text-[20px] font-semibold text-amber-300">{{ adverseMetric('km_total', 'km') }}</span>
              </div>
              <div class="bg-[#24354D] p-3.5 rounded-lg border border-slate-600/50">
                <span class="text-[11px] text-slate-400 block mb-1">Entregas completadas</span>
                <span class="text-[20px] font-semibold text-amber-300">{{ adverseMetric('entregas_completadas', 'entregas') }}</span>
              </div>
              <div class="bg-[#24354D] p-3.5 rounded-lg border border-slate-600/50">
                <span class="text-[11px] text-slate-400 block mb-1">Quiebres evitados / nuevos</span>
                <span class="text-[15px] font-semibold text-amber-300 block">
                  {{ adverseStockout('quiebres_evitados_' + selectedBaseline()) }} /
                  {{ adverseStockout('quiebres_nuevos_' + selectedBaseline()) }}
                </span>
                <span class="text-[10px] text-slate-400">frente a {{ baselineLabel() }}</span>
              </div>
            </div>
          </div>

          <!-- Semillas y políticas -->
          <div class="bg-white rounded-xl border border-[#E3E8EE] shadow-xs overflow-hidden">
            <div class="p-4 border-b border-[#E3E8EE] flex flex-col sm:flex-row sm:items-center justify-between gap-2 bg-[#F8FAFC]">
              <div>
                <h2 class="text-[13px] font-semibold text-[#1A2C44]">Corridas del comparador</h2>
                <p class="text-[11px] text-[#8792A2]">Tres semillas base y una adversa por política. El progreso se actualiza al consultar la API.</p>
              </div>
              <span class="text-[11px] font-medium text-[#697386]">{{ completedRuns() }}/{{ totalRuns() }} completadas</span>
            </div>

            <div class="p-4 grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-3">
              @for (group of runGroups(); track group.key) {
                <div class="rounded-lg border border-[#E3E8EE] bg-[#FBFCFE] p-3 space-y-2.5">
                  <div class="flex items-center justify-between gap-2">
                    <div>
                      <p class="text-[12px] font-semibold text-[#1A2C44]">Semilla {{ group.seed }}</p>
                      <p class="text-[10px] uppercase tracking-wider text-[#8792A2]">{{ group.scenario === 'adverse' ? 'Adversa' : 'Base' }}</p>
                    </div>
                    <span class="text-[10px] font-medium px-2 py-0.5 rounded border"
                      [ngClass]="group.scenario === 'adverse' ? 'bg-amber-50 text-amber-700 border-amber-200' : 'bg-white text-[#4F5B66] border-[#E3E8EE]'">
                      {{ group.scenario === 'adverse' ? 'Adversa' : 'Base' }}
                    </span>
                  </div>

                  @for (run of group.runs; track run.id) {
                    <div class="space-y-1">
                      <div class="flex items-center justify-between gap-2 text-[11px]">
                        <span class="flex items-center gap-1.5 min-w-0">
                          <span class="w-1.5 h-1.5 rounded-full shrink-0" [ngClass]="policyDefinition(run.policyCode).dotClass"></span>
                          <span class="truncate text-[#4F5B66]">{{ policyDefinition(run.policyCode).label }}</span>
                        </span>
                        <span class="px-1.5 py-0.5 rounded border text-[10px] font-medium shrink-0" [ngClass]="statusChipClass(run.status)">
                          {{ statusLabel(run.status) }}
                        </span>
                      </div>
                      <div class="flex items-center gap-2">
                        <div class="flex-1 h-1.5 rounded-full bg-[#E3E8EE] overflow-hidden" role="progressbar" [attr.aria-valuenow]="run.progressPercentage" aria-valuemin="0" aria-valuemax="100">
                          <div class="h-1.5 rounded-full bg-emerald-500 transition-all duration-300" [style.width.%]="run.progressPercentage"></div>
                        </div>
                        <span class="text-[10px] font-semibold text-[#1A2C44] tabular-nums">{{ run.progressPercentage }}%</span>
                      </div>
                      @if (run.failureReason) {
                        <p class="text-[10px] text-rose-700">{{ run.failureReason }}</p>
                      }
                    </div>
                  }
                </div>
              }
            </div>
          </div>

          <!-- Supuestos y trazabilidad -->
          <details class="bg-white rounded-xl border border-[#E3E8EE] shadow-xs p-4 group">
            <summary class="cursor-pointer list-none flex items-center justify-between gap-3 text-[12px] font-semibold text-[#1A2C44]">
              <span>Supuestos y trazabilidad de la corrida</span>
              <span class="text-[11px] font-normal text-[#8792A2] group-open:hidden">Abrir detalle</span>
              <span class="text-[11px] font-normal text-[#8792A2] hidden group-open:inline">Cerrar detalle</span>
            </summary>

            <div class="mt-4 grid grid-cols-1 lg:grid-cols-2 gap-4">
              <ul class="space-y-2">
                @for (assumption of selectedDetail()!.assumptions; track assumption) {
                  <li class="flex items-start gap-2 bg-[#F8FAFC] p-2.5 rounded-md border border-[#E3E8EE] text-[12px] text-[#4F5B66]">
                    <span class="text-[#635BFF] font-bold mt-0.5">✓</span>
                    <span>{{ assumption }}</span>
                  </li>
                }
              </ul>

              <div class="space-y-2 text-[11px] text-[#4F5B66]">
                <div class="flex justify-between gap-3 border-b border-[#F1F3F5] pb-2">
                  <span class="text-[#8792A2]">Dataset</span>
                  <span class="font-medium text-right">{{ selectedDetail()!.datasetVersion }}</span>
                </div>
                @if (selectedDetail()!.datasetHash) {
                  <div class="flex justify-between gap-3 border-b border-[#F1F3F5] pb-2">
                    <span class="text-[#8792A2]">Hash</span>
                    <code class="text-[10px] text-right break-all">{{ selectedDetail()!.datasetHash }}</code>
                  </div>
                }
                <div class="flex justify-between gap-3 border-b border-[#F1F3F5] pb-2">
                  <span class="text-[#8792A2]">Origen</span>
                  <span class="font-medium">{{ selectedDetail()!.origin === 'imported' ? 'Importado de otra máquina' : 'Calculado localmente' }}</span>
                </div>
                @if (selectedDetail()!.hostName) {
                  <div class="flex justify-between gap-3 border-b border-[#F1F3F5] pb-2">
                    <span class="text-[#8792A2]">Host</span>
                    <span class="font-medium">{{ selectedDetail()!.hostName }}</span>
                  </div>
                }
                @if (selectedDetail()!.commitHash) {
                  <div class="flex justify-between gap-3">
                    <span class="text-[#8792A2]">Commit</span>
                    <code class="text-[10px]">{{ selectedDetail()!.commitHash }}</code>
                  </div>
                }
                <p class="pt-2 text-[10px] text-[#8792A2]">
                  Estas cifras no son ahorro observado ni una medición empresarial. Provienen de una corrida sintética reproducible.
                </p>
              </div>
            </div>
          </details>
        }
      } @else {
        <div class="bg-white p-12 text-center rounded-xl border border-dashed border-[#E3E8EE] space-y-3">
          <svg class="w-10 h-10 text-[#8792A2] mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
          <h3 class="text-[15px] font-semibold text-[#1A2C44]">Sin corrida de simulación activa</h3>
          <p class="text-[13px] text-[#697386] max-w-sm mx-auto">
            Lanza una nueva corrida de 90 días o importa un ZIP reproducible para visualizar los resultados comparativos.
          </p>
        </div>
      }
    </div>
  `
})
export class SimulationPage implements OnInit, OnDestroy {
  readonly session = inject(AuthSession);
  private api = inject(SimulationsApiService);

  readonly policyDefinitions: PolicyDefinition[] = [
    {
      code: 'istpetdev-v1',
      label: 'IstpetDev',
      description: 'Reposición preventiva',
      chipClass: 'bg-indigo-50 text-indigo-700 border-indigo-200',
      barClass: 'bg-[#635BFF]',
      dotClass: 'bg-[#635BFF]'
    },
    {
      code: 'reactive-v1',
      label: 'Reactiva',
      description: 'Avisos de stock',
      chipClass: 'bg-amber-50 text-amber-700 border-amber-200',
      barClass: 'bg-amber-400',
      dotClass: 'bg-amber-400'
    },
    {
      code: 'fixed7-v1',
      label: 'Semanal fija',
      description: 'Visita cada 7 días',
      chipClass: 'bg-slate-100 text-slate-700 border-slate-200',
      barClass: 'bg-slate-400',
      dotClass: 'bg-slate-400'
    }
  ];

  readonly baselineOptions: Array<{ code: BaselineCode; label: string }> = [
    { code: 'reactive-v1', label: 'Reactiva (avisos)' },
    { code: 'fixed7-v1', label: 'Semanal fija (7 días)' }
  ];

  readonly comparisonRows: ComparisonRow[] = [
    { code: 'costo_transporte', label: 'Costo de transporte', unit: 'USD', direction: 'lower', hint: 'Flete, combustible, conductor y ayudantes.' },
    { code: 'km_total', label: 'Kilometraje total', unit: 'km', direction: 'lower', hint: 'Incluye el regreso a bodega.' },
    { code: 'entregas_completadas', label: 'Entregas completadas', unit: 'entregas', direction: 'higher', hint: 'Líneas de reposición entregadas.' }
  ];

  comparisons = signal<SimulationComparisonSummary[]>([]);
  selectedComparisonId = signal<string | null>(null);
  selectedDetail = signal<SimulationComparisonDetail | null>(null);
  runningSimulation = signal<boolean>(false);
  feedback = signal<{ type: 'success' | 'error'; text: string } | null>(null);
  selectedBaseline = signal<BaselineCode>('reactive-v1');

  readonly completedRuns = computed(() => this.selectedDetail()?.runs.filter(r => r.status === 'Completed').length ?? 0);
  readonly totalRuns = computed(() => this.selectedDetail()?.runs.length ?? 0);
  readonly baselineLabel = computed(() => this.policyDefinition(this.selectedBaseline()).label);
  readonly adverseSeed = computed(() => this.selectedDetail()?.runs.find(r => r.scenarioCode === 'adverse')?.seed ?? 9999);
  readonly punctualityDetail = computed(() =>
    this.selectedDetail()?.metrics.find(m => m.code === 'puntualidad' && m.policyCode === 'istpetdev-v1' && m.scenarioCode === 'base'));
  readonly runGroups = computed<RunGroup[]>(() => {
    const runs = this.selectedDetail()?.runs ?? [];
    const groups = new Map<string, RunGroup>();
    for (const run of runs) {
      const key = `${run.seed}-${run.scenarioCode}`;
      if (!groups.has(key)) groups.set(key, { key, seed: run.seed, scenario: run.scenarioCode, runs: [] });
      groups.get(key)!.runs.push(run);
    }
    return [...groups.values()].sort((a, b) => a.seed - b.seed || a.scenario.localeCompare(b.scenario));
  });

  private pollTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.loadComparisons();
  }

  ngOnDestroy(): void {
    if (this.pollTimer) clearInterval(this.pollTimer);
  }

  loadComparisons(): void {
    this.api.listComparisons().subscribe({
      next: (list) => {
        this.comparisons.set(list);
        if (list.length > 0 && !this.selectedComparisonId()) {
          this.onSelectComparison(list[0].comparisonId);
        }
      },
      error: () => this.feedback.set({ type: 'error', text: 'Error al consultar corridas de simulación.' })
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
      error: () => this.feedback.set({ type: 'error', text: 'No se pudo cargar el detalle de la corrida.' })
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
        this.feedback.set({ type: 'error', text: err?.error?.message || 'Error al lanzar la simulación.' });
      }
    });
  }

  exportZip(): void {
    const id = this.selectedComparisonId();
    if (!id) return;
    this.api.exportComparison(id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `simulation-${id}.zip`;
        anchor.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => this.feedback.set({ type: 'error', text: 'Error al exportar el paquete ZIP.' })
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
      error: (err) => this.feedback.set({ type: 'error', text: err?.error?.message || 'Error al importar archivo ZIP. Verifique los hashes.' })
    });
  }

  metric(policy: string, code: string, sku?: string | null): AggregatedMetricSummary | undefined {
    return this.selectedDetail()?.aggregated.find(m =>
      m.policy === policy &&
      m.metricCode === code &&
      (m.sku ?? null) === (sku ?? null));
  }

  comparison(code: string, direction: MetricDirection): MetricComparison | null {
    const platform = this.metric('istpetdev-v1', code)?.median;
    const baseline = this.metric(this.selectedBaseline(), code)?.median;
    if (platform == null || baseline == null) return null;

    const absolute = platform - baseline;
    const percent = baseline === 0 ? null : Math.abs(absolute / baseline) * 100;
    const favorable = direction === 'lower' ? absolute <= 0 : absolute >= 0;
    return { platform, baseline, absolute, percent, favorable };
  }

  kmDelta(): MetricComparison | null {
    return this.comparison('km_total', 'lower');
  }

  unattendedRows(): Array<{ sku: string; unit: string; median: number | null | undefined; adverse: number | null | undefined; min: number | null | undefined; max: number | null | undefined }> {
    return (this.selectedDetail()?.aggregated ?? [])
      .filter(m => m.policy === 'istpetdev-v1' && m.metricCode === 'demanda_no_atendida' && m.sku)
      .map(m => ({
        sku: m.sku!,
        unit: this.unitFor('demanda_no_atendida', m.sku),
        median: m.median,
        adverse: m.adverseValue,
        min: m.min,
        max: m.max
      }))
      .sort((a, b) => a.sku.localeCompare(b.sku));
  }

  adverseMetric(code: string, unit: string): string {
    const value = this.metric('istpetdev-v1', code)?.adverseValue;
    return value != null ? this.formatMetric(value, unit) : 'Sin dato';
  }

  adverseStockout(code: string): string {
    const value = this.metric('istpetdev-v1', code)?.adverseValue;
    return value != null ? this.formatMetric(value, 'quiebres') : 'Sin dato';
  }

  barWidth(policy: string, code: string): number {
    const values = this.policyDefinitions
      .map(p => this.metric(p.code, code)?.median)
      .filter((value): value is number => value != null && Number.isFinite(value));
    const max = Math.max(0, ...values);
    const value = this.metric(policy, code)?.median;
    if (max <= 0 || value == null) return 0;
    return Math.max(4, Math.min(100, (value / max) * 100));
  }

  unitFor(code: string, sku?: string | null): string {
    const metric = this.selectedDetail()?.metrics.find(m =>
      m.code === code && ((m.sku ?? null) === (sku ?? null)));
    return metric?.unit ?? this.comparisonRows.find(row => row.code === code)?.unit ?? '';
  }

  policyDefinition(code: string): PolicyDefinition {
    return this.policyDefinitions.find(p => p.code === code) ?? this.policyDefinitions[2];
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'Completed': return 'Completada';
      case 'Running': return 'En ejecución';
      case 'Queued': return 'En cola';
      case 'Failed': return 'Fallida';
      case 'Interrupted': return 'Interrumpida';
      default: return status;
    }
  }

  statusChipClass(status: string): string {
    switch (status) {
      case 'Completed': return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'Failed':
      case 'Interrupted': return 'bg-rose-50 text-rose-700 border-rose-200';
      case 'Running': return 'bg-amber-50 text-amber-700 border-amber-200';
      default: return 'bg-[#F8FAFC] text-[#4F5B66] border-[#E3E8EE]';
    }
  }

  formatMetric(value: number, unit: string): string {
    const normalized = (unit || '').toUpperCase();
    if (normalized === 'USD') return this.formatMoney(value);
    if (normalized === 'KM') return `${this.formatNumber(value, 1)} km`;
    if (normalized === '%') return `${this.formatNumber(value, 1)} %`;
    if (normalized === 'DÍAS' || normalized === 'DIAS') return `${this.formatNumber(value, 1)} días`;
    if (normalized === 'QUIEBRES') return `${this.formatNumber(value, 0)} ${Math.abs(value) === 1 ? 'quiebre' : 'quiebres'}`;
    if (normalized === 'LÍNEAS' || normalized === 'LINEAS') return `${this.formatNumber(value, 0)} ${Math.abs(value) === 1 ? 'línea' : 'líneas'}`;
    if (normalized === 'ENTREGAS') return `${this.formatNumber(value, 0)} ${Math.abs(value) === 1 ? 'entrega' : 'entregas'}`;
    return `${this.formatNumber(value, 2)} ${unit}`.trim();
  }

  formatSigned(value: number, unit: string): string {
    if (value === 0) return this.formatMetric(0, unit);
    const sign = value > 0 ? '+' : '−';
    return `${sign}${this.formatMetric(Math.abs(value), unit)}`;
  }

  formatDelta(comparison: MetricComparison, unit = 'USD'): string {
    const percent = comparison.percent == null ? null : `${this.formatNumber(comparison.percent, 1)} %`;
    if (comparison.absolute === 0) return `Igual que ${this.baselineLabel()}`;
    const direction = comparison.absolute > 0 ? 'más' : 'menos';
    return percent
      ? `${percent} ${direction} que ${this.baselineLabel()}`
      : `${this.formatSigned(comparison.absolute, unit)} ${direction} que ${this.baselineLabel()}`;
  }

  formatMoney(value: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
  }

  formatNumber(value: number, digits: number): string {
    return new Intl.NumberFormat('en-US', { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(value);
  }
}
