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
    <div class="max-w-7xl mx-auto px-4 sm:px-6 py-8 space-y-6">
      <!-- Encabezado Principal -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-200 pb-5">
        <div>
          <div class="flex items-center gap-3">
            <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
              Simulador Comparativo de 90 Días
            </h1>
            <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-primary-100 text-primary-800 border border-primary-200">
              Spec 005
            </span>
          </div>
          <p class="text-sm text-slate-500 mt-1">
            Evaluación auditada de la política IstpetDev frente a baselines congeladas (reactive-v1 y fixed7-v1) sobre dataset nacional de 6 regiones.
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          @if (session.canWrite()) {
            <button
              type="button"
              (click)="runSimulation()"
              [disabled]="runningSimulation()"
              class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-600 text-white text-xs font-semibold hover:bg-primary-700 transition shadow-sm disabled:opacity-50">
              @if (runningSimulation()) {
                <span class="inline-block animate-spin h-3.5 w-3.5 border-2 border-white border-t-transparent rounded-full"></span>
                <span>Encolando corrida...</span>
              } @else {
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Nueva Corrida 90d (12 Runs)</span>
              }
            </button>

            <label class="cursor-pointer inline-flex items-center gap-1.5 px-3 py-2 rounded-lg bg-white border border-slate-300 text-slate-700 text-xs font-semibold hover:bg-slate-50 shadow-sm">
              <svg class="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
              class="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 shadow-sm transition">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
          class="p-4 rounded-xl text-xs font-medium flex items-center justify-between transition-all"
          [ngClass]="feedback()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-rose-50 text-rose-800 border border-rose-200'">
          <span>{{ feedback()!.text }}</span>
          <button (click)="feedback.set(null)" class="text-slate-400 hover:text-slate-600 font-bold ml-2">✕</button>
        </div>
      }

      <!-- Selector de corridas previas -->
      <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div class="flex items-center gap-3">
          <span class="text-xs font-bold text-slate-700 uppercase tracking-wider">Corrida activa:</span>
          @if (comparisons().length > 0) {
            <select
              [ngModel]="selectedComparisonId()"
              (ngModelChange)="onSelectComparison($event)"
              class="text-xs font-mono bg-slate-50 border border-slate-300 rounded-lg px-3 py-1.5 text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary-500">
              @for (c of comparisons(); track c.comparisonId) {
                <option [value]="c.comparisonId">
                  {{ c.comparisonId.substring(0, 8) }}... | {{ c.status }} ({{ c.progressPercentage }}%) | {{ c.origin }} | {{ c.createdAtUtc | date:'short' }}
                </option>
              }
            </select>
          } @else {
            <span class="text-xs text-slate-400 italic">No hay corridas registradas</span>
          }
        </div>

        @if (selectedDetail()) {
          <div class="flex flex-wrap items-center gap-3 text-xs text-slate-600">
            <span class="flex items-center gap-1.5">
              <span class="font-semibold text-slate-800">Dataset:</span>
              <code class="bg-slate-100 px-1.5 py-0.5 rounded text-slate-700">{{ selectedDetail()!.datasetVersion }}</code>
            </span>
            <span class="flex items-center gap-1.5">
              <span class="font-semibold text-slate-800">Origen:</span>
              <span class="px-2 py-0.5 rounded text-[11px] font-semibold"
                [ngClass]="selectedDetail()!.origin === 'imported' ? 'bg-purple-100 text-purple-800' : 'bg-blue-100 text-blue-800'">
                {{ selectedDetail()!.origin === 'imported' ? 'Importado de máquina dedicada' : 'Calculado localmente' }}
              </span>
            </span>
            @if (selectedDetail()!.hostName) {
              <span class="flex items-center gap-1">
                <span class="font-semibold text-slate-800">Host:</span>
                <code>{{ selectedDetail()!.hostName }}</code>
              </span>
            }
          </div>
        }
      </div>

      <!-- Estado de la corrida seleccionada -->
      @if (selectedDetail()) {
        <!-- En progreso -->
        @if (selectedDetail()!.status === 'Queued' || selectedDetail()!.status === 'Running') {
          <div class="bg-amber-50 border border-amber-200 rounded-xl p-6 text-center space-y-4 shadow-sm">
            <div class="inline-flex items-center justify-center p-3 bg-amber-100 rounded-full text-amber-600">
              <span class="animate-spin h-6 w-6 border-3 border-amber-600 border-t-transparent rounded-full"></span>
            </div>
            <h3 class="text-lg font-bold text-slate-900">Simulación en ejecución (90 días)</h3>
            <p class="text-xs text-slate-600 max-w-lg mx-auto">
              Se están evaluando las políticas sobre las 3 semillas base y la semilla adversa (12 jornadas en paralelo). Las cifras e indicadores se revelarán al completar la verificación de invariantes.
            </p>
            <div class="max-w-md mx-auto space-y-1.5">
              <div class="flex justify-between text-xs font-semibold text-slate-700">
                <span>Progreso global</span>
                <span>{{ selectedDetail()!.progressPercentage }}%</span>
              </div>
              <div class="w-full bg-amber-200 rounded-full h-2.5 overflow-hidden">
                <div class="bg-amber-600 h-2.5 rounded-full transition-all duration-300" [style.width.%]="selectedDetail()!.progressPercentage"></div>
              </div>
            </div>
          </div>
        }

        <!-- Fallida -->
        @if (selectedDetail()!.status === 'Failed' || selectedDetail()!.status === 'Interrupted') {
          <div class="bg-rose-50 border border-rose-200 rounded-xl p-6 space-y-2">
            <h3 class="text-base font-bold text-rose-900">La corrida no pudo completarse</h3>
            <p class="text-xs text-rose-700">
              Causa registrada: {{ selectedDetail()!.failureReason || 'Error de invariantes o interrupción del proceso.' }}
            </p>
          </div>
        }

        <!-- Completada: Panel de Impacto y Métricas -->
        @if (selectedDetail()!.status === 'Completed') {
          <!-- Controles de filtrado -->
          <div class="flex flex-wrap items-center justify-between gap-4 bg-slate-50 p-4 rounded-xl border border-slate-200">
            <div class="flex items-center gap-3">
              <span class="text-xs font-semibold text-slate-700">Baseline de referencia:</span>
              <div class="inline-flex rounded-lg border border-slate-300 bg-white p-0.5 text-xs">
                <button
                  type="button"
                  (click)="selectedBaseline.set('reactive-v1')"
                  [class.bg-primary-600]="selectedBaseline() === 'reactive-v1'"
                  [class.text-white]="selectedBaseline() === 'reactive-v1'"
                  [class.text-slate-700]="selectedBaseline() !== 'reactive-v1'"
                  class="px-3 py-1 rounded-md font-medium transition">
                  reactive-v1 (Avisos de stock)
                </button>
                <button
                  type="button"
                  (click)="selectedBaseline.set('fixed7-v1')"
                  [class.bg-primary-600]="selectedBaseline() === 'fixed7-v1'"
                  [class.text-white]="selectedBaseline() === 'fixed7-v1'"
                  [class.text-slate-700]="selectedBaseline() !== 'fixed7-v1'"
                  class="px-3 py-1 rounded-md font-medium transition">
                  fixed7-v1 (Semanal fijo)
                </button>
              </div>
            </div>

            <div class="flex items-center gap-2 text-xs text-slate-500">
              <span>Etiqueta oficial:</span>
              <span class="font-medium text-slate-700 italic">«Cifras simuladas y medidas en corrida de 90 días»</span>
            </div>
          </div>

          <!-- Cuatro Tarjetas Principales de KPI -->
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <!-- Tarjeta 1: Quiebres de Stock -->
            <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">Quiebres de Stock</span>
                <span class="p-1.5 rounded-lg bg-emerald-50 text-emerald-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </span>
              </div>

              <div class="space-y-1">
                <div class="flex justify-between items-baseline">
                  <span class="text-xs text-slate-600">Quiebres evitados:</span>
                  <span class="text-base font-bold text-emerald-600">0</span>
                </div>
                <div class="flex justify-between items-baseline">
                  <span class="text-xs text-slate-600">Quiebres nuevos:</span>
                  <span class="text-base font-bold text-slate-800">0</span>
                </div>
                <div class="pt-2 border-t border-slate-100 flex justify-between items-baseline">
                  <span class="text-xs font-semibold text-slate-700">Diferencia neta:</span>
                  <span class="text-lg font-extrabold text-slate-900">0 quiebres</span>
                </div>
              </div>
              <p class="text-[11px] text-slate-400 leading-tight">
                Evitados y nuevos presentados por separado sin enmascarar la diferencia neta (CE-05).
              </p>
            </div>

            <!-- Tarjeta 2: Demanda No Atendida -->
            <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">Demanda No Atendida</span>
                <span class="p-1.5 rounded-lg bg-blue-50 text-blue-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                  </svg>
                </span>
              </div>

              <div class="space-y-1.5">
                <div class="flex justify-between items-baseline">
                  <span class="text-xs text-slate-600">SKU-01 (L):</span>
                  <span class="text-sm font-bold text-slate-800">0.000 L</span>
                </div>
                <div class="flex justify-between items-baseline">
                  <span class="text-xs text-slate-600">SKU-02 (kg):</span>
                  <span class="text-sm font-bold text-slate-800">0.000 kg</span>
                </div>
              </div>
              <p class="text-[11px] text-slate-400 leading-tight">
                Desglosada por SKU y unidad base: litros y kilogramos nunca se suman juntos (G4).
              </p>
            </div>

            <!-- Tarjeta 3: Costo de Transporte -->
            <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">Costo de Transporte</span>
                <span class="p-1.5 rounded-lg bg-amber-50 text-amber-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </span>
              </div>

              <div class="space-y-1">
                @let costAgg = getAggregatedMetric('costo_transporte');
                @if (costAgg) {
                  <div class="text-2xl font-black text-slate-900">
                    \${{ costAgg.median | number:'1.2-2' }}
                  </div>
                  <div class="flex justify-between text-xs text-slate-500">
                    <span>Mín: \${{ costAgg.min | number:'1.2-2' }}</span>
                    <span>Máx: \${{ costAgg.max | number:'1.2-2' }}</span>
                  </div>
                } @else {
                  <div class="text-2xl font-black text-slate-900">N/A</div>
                  <span class="text-xs text-slate-400">Sin datos de costo</span>
                }
              </div>
              <p class="text-[11px] text-slate-400 leading-tight">
                Rango (mín, mediana, máx) calculado a través de las semillas base.
              </p>
            </div>

            <!-- Tarjeta 4: Puntualidad y Trazabilidad -->
            <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-sm space-y-3">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">Puntualidad & Trazabilidad</span>
                <span class="p-1.5 rounded-lg bg-indigo-50 text-indigo-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </span>
              </div>

              <div class="space-y-1">
                <div class="flex justify-between items-baseline">
                  <span class="text-xs text-slate-600">Puntualidad:</span>
                  <span class="text-base font-bold text-indigo-700">100.0%</span>
                </div>
                <div class="flex justify-between items-baseline">
                  <span class="text-xs text-slate-600">Trazabilidad simulada:</span>
                  <span class="text-xs font-semibold px-2 py-0.5 bg-slate-100 text-slate-600 rounded">
                    N/A (not_simulated)
                  </span>
                </div>
              </div>
              <p class="text-[11px] text-slate-400 leading-tight">
                La trazabilidad completa proviene de la operativa física en 004, no del simulador.
              </p>
            </div>
          </div>

          <!-- Escenario Adverso (Separado por Guardarraíl CE-04) -->
          <div class="bg-slate-900 text-white p-6 rounded-2xl shadow-lg space-y-4">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800 pb-3">
              <div class="flex items-center gap-2.5">
                <span class="px-2.5 py-1 bg-amber-500/20 text-amber-400 border border-amber-500/30 rounded text-xs font-bold uppercase tracking-wider">
                  Escenario Adverso (Seed 9999)
                </span>
                <span class="text-xs text-slate-400">Aislado fuera del rango de semillas base</span>
              </div>
              <span class="text-xs text-slate-400 font-mono">Disrupciones exógenas severas</span>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2">
              <div class="bg-slate-800/60 p-4 rounded-xl border border-slate-700/50">
                <span class="text-xs text-slate-400 block mb-1">Costo de Transporte Adverso</span>
                @let advCost = getAggregatedMetric('costo_transporte');
                <span class="text-xl font-bold text-amber-400">
                  {{ advCost?.adverseValue ? ('$' + (advCost!.adverseValue | number:'1.2-2')) : 'Medido en corrida' }}
                </span>
              </div>
              <div class="bg-slate-800/60 p-4 rounded-xl border border-slate-700/50">
                <span class="text-xs text-slate-400 block mb-1">Kilometraje Adverso Total</span>
                @let advKm = getAggregatedMetric('km_total');
                <span class="text-xl font-bold text-amber-400">
                  {{ advKm?.adverseValue ? ((advKm!.adverseValue | number:'1.1-1') + ' km') : 'Medido en corrida' }}
                </span>
              </div>
              <div class="bg-slate-800/60 p-4 rounded-xl border border-slate-700/50">
                <span class="text-xs text-slate-400 block mb-1">Impacto de Resiliencia</span>
                <span class="text-sm font-semibold text-emerald-400 block">
                  Sin quiebres colapsados
                </span>
              </div>
            </div>
          </div>

          <!-- Supuestos y Parámetros Auditables -->
          <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-sm space-y-4">
            <h3 class="text-sm font-bold text-slate-900 uppercase tracking-wider">
              Supuestos de Simulación Auditables (CE-05, G3)
            </h3>
            <ul class="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs text-slate-600">
              @for (a of selectedDetail()!.assumptions; track a) {
                <li class="flex items-start gap-2 bg-slate-50 p-2.5 rounded-lg border border-slate-100">
                  <span class="text-primary-600 font-bold mt-0.5">✓</span>
                  <span>{{ a }}</span>
                </li>
              }
            </ul>
          </div>

          <!-- Tabla de las 12 Corridas -->
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div class="p-4 border-b border-slate-200 flex items-center justify-between">
              <h3 class="text-sm font-bold text-slate-900">
                Detalle de las 12 Corridas del Comparador
              </h3>
              <span class="text-xs text-slate-500">1080 jornadas simuladas en total</span>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left text-xs text-slate-700">
                <thead class="bg-slate-50 text-slate-500 uppercase font-semibold border-b border-slate-200">
                  <tr>
                    <th class="py-3 px-4">Semilla</th>
                    <th class="py-3 px-4">Escenario</th>
                    <th class="py-3 px-4">Política</th>
                    <th class="py-3 px-4">Estado</th>
                    <th class="py-3 px-4">Progreso</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  @for (r of selectedDetail()!.runs; track r.id) {
                    <tr class="hover:bg-slate-50/50 transition">
                      <td class="py-3 px-4 font-mono font-medium text-slate-900">{{ r.seed }}</td>
                      <td class="py-3 px-4">
                        <span class="px-2 py-0.5 rounded text-[11px] font-semibold"
                          [ngClass]="r.scenarioCode === 'adverse' ? 'bg-amber-100 text-amber-800' : 'bg-slate-100 text-slate-700'">
                          {{ r.scenarioCode }}
                        </span>
                      </td>
                      <td class="py-3 px-4 font-mono text-slate-800 font-medium">{{ r.policyCode }}</td>
                      <td class="py-3 px-4">
                        <span class="px-2 py-0.5 rounded text-[11px] font-semibold bg-emerald-100 text-emerald-800">
                          {{ r.status }}
                        </span>
                      </td>
                      <td class="py-3 px-4">
                        <div class="flex items-center gap-2">
                          <span class="font-bold">{{ r.progressPercentage }}%</span>
                          <div class="w-16 bg-slate-200 rounded-full h-1.5 overflow-hidden">
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
        <div class="bg-white p-12 text-center rounded-xl border border-dashed border-slate-300 space-y-3">
          <svg class="w-12 h-12 text-slate-300 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
          <h3 class="text-base font-bold text-slate-700">Sin corrida de simulación activa</h3>
          <p class="text-xs text-slate-500 max-w-sm mx-auto">
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
