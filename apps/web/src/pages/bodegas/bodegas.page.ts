import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, WarehouseAnalysisDto, TransferSuggestionDto } from '@shared/api';

@Component({
  selector: 'page-bodegas',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8 space-y-6">
      <!-- Encabezado -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
            Gestión de Bodegas & Rebalanceo de Stock
          </h1>
          <p class="text-sm text-slate-500 mt-1">
            Supervisa el consumo mensual, autonomía de cobertura y traslados inteligentes entre bodegas para evitar desabastecimiento.
          </p>
        </div>

        <div class="flex items-center gap-2">
          <button
            type="button"
            (click)="generateSuggestions()"
            [disabled]="loadingAction()"
            class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors disabled:opacity-50 shadow-sm">
            {{ loadingAction() ? 'Analizando...' : 'Analizar y Sugerir Traslados' }}
          </button>
        </div>
      </div>

      <!-- Banner Feedback -->
      @if (feedback()) {
        <div 
          class="p-4 rounded-xl text-xs font-medium flex items-center justify-between transition-all"
          [ngClass]="feedback()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-rose-50 text-rose-800 border border-rose-200'">
          <span>{{ feedback()!.text }}</span>
          <button (click)="feedback.set(null)" class="text-slate-400 hover:text-slate-600 font-bold ml-2">✕</button>
        </div>
      }

      <!-- Navegación de pestañas -->
      <div class="border-b border-slate-200 flex gap-6 text-xs font-bold uppercase tracking-wider">
        <button
          type="button"
          (click)="activeTab.set('analysis')"
          class="pb-3 border-b-2 transition-colors"
          [ngClass]="activeTab() === 'analysis' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-800'">
          Análisis de Consumo & Cobertura
        </button>
        <button
          type="button"
          (click)="activeTab.set('rebalancing')"
          class="pb-3 border-b-2 transition-colors relative"
          [ngClass]="activeTab() === 'rebalancing' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-800'">
          <span>Traslados Sugeridos</span>
          @if (pendingSuggestionsCount() > 0) {
            <span class="ml-1 px-1.5 py-0.5 rounded-full text-[10px] bg-amber-100 text-amber-800 font-bold font-mono">
              {{ pendingSuggestionsCount() }}
            </span>
          }
        </button>
        <button
          type="button"
          (click)="activeTab.set('policies')"
          class="pb-3 border-b-2 transition-colors"
          [ngClass]="activeTab() === 'policies' ? 'border-primary-600 text-primary-600' : 'border-transparent text-slate-500 hover:text-slate-800'">
          Políticas de Stock Mínimo/Máximo
        </button>
      </div>

      <!-- PESTAÑA 1: ANÁLISIS DE CONSUMO -->
      @if (activeTab() === 'analysis') {
        <div class="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-sm">
          <div class="p-4 border-b border-slate-100 flex items-center justify-between">
            <h2 class="text-sm font-bold text-slate-800">
              Inventario de Bodegas y Autonomía de Consumo
            </h2>
            <button (click)="loadAnalysis()" class="text-xs text-primary-600 hover:text-primary-800 font-semibold">
              Actualizar datos
            </button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 text-[11px] text-slate-500 uppercase border-b border-slate-200">
                <tr>
                  <th class="p-3">Bodega</th>
                  <th class="p-3">Producto / SKU</th>
                  <th class="p-3 text-right">Stock Disp.</th>
                  <th class="p-3 text-right">Consumo Mensual</th>
                  <th class="p-3 text-right">Días Cobertura</th>
                  <th class="p-3 text-center">Estado</th>
                  <th class="p-3 text-right">Mín / Máx</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 font-mono">
                @for (item of analysisList(); track item.locationId + item.productId) {
                  <tr class="hover:bg-slate-50/80 transition-colors">
                    <td class="p-3 font-bold text-slate-900 font-sans">{{ item.locationCode }}</td>
                    <td class="p-3 font-sans">
                      <span class="font-bold text-slate-800">{{ item.productName }}</span>
                      <span class="text-slate-400 block text-[10px] font-mono">{{ item.sku }}</span>
                    </td>
                    <td class="p-3 text-right font-bold text-slate-800">{{ item.availableQuantity }}</td>
                    <td class="p-3 text-right text-slate-600">{{ item.monthlyConsumptionRate }} u/mes</td>
                    <td class="p-3 text-right">
                      <span class="font-bold px-2 py-0.5 rounded text-[11px]"
                        [ngClass]="{
                          'bg-rose-50 text-rose-700 border border-rose-200': item.coverageDays < 15,
                          'bg-amber-50 text-amber-700 border border-amber-200': item.coverageDays >= 15 && item.coverageDays <= 30,
                          'bg-emerald-50 text-emerald-700 border border-emerald-200': item.coverageDays > 30 && item.coverageDays <= 90,
                          'bg-purple-50 text-purple-700 border border-purple-200': item.coverageDays > 90
                        }">
                        {{ item.coverageDays > 900 ? '∞ Sin Salida' : item.coverageDays + ' días' }}
                      </span>
                    </td>
                    <td class="p-3 text-center">
                      <span class="text-[10px] px-2 py-0.5 rounded font-sans uppercase font-bold"
                        [ngClass]="{
                          'bg-rose-100 text-rose-800': item.status === 'Critico' || item.status === 'SinStock',
                          'bg-emerald-100 text-emerald-800': item.status === 'Normal',
                          'bg-purple-100 text-purple-800': item.status === 'Exceso'
                        }">
                        {{ item.status }}
                      </span>
                    </td>
                    <td class="p-3 text-right text-slate-500 text-[11px]">
                      {{ item.minStock }} / {{ item.maxStock }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- PESTAÑA 2: REBALANCEO Y TRASLADOS SUGERIDOS -->
      @if (activeTab() === 'rebalancing') {
        <div class="space-y-4">
          @if (suggestionsList().length === 0) {
            <div class="p-8 text-center bg-white border border-slate-200 rounded-xl space-y-3">
              <p class="text-sm font-semibold text-slate-700">No hay traslados pendientes de revisión</p>
              <p class="text-xs text-slate-500 max-w-md mx-auto">
                La red de bodegas se encuentra equilibrada. Puedes pulsar en "Analizar y Sugerir Traslados" para reevaluar con el histórico de consumos.
              </p>
              <button
                type="button"
                (click)="generateSuggestions()"
                class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors">
                Ejecutar Análisis Ahora
              </button>
            </div>
          } @else {
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              @for (sug of suggestionsList(); track sug.id) {
                <div class="bg-white border border-slate-200 rounded-xl p-4 shadow-sm flex flex-col justify-between space-y-3">
                  <div>
                    <div class="flex items-center justify-between border-b border-slate-100 pb-2">
                      <span class="font-bold text-sm text-slate-900">{{ sug.productName }}</span>
                      <span class="text-[10px] font-mono px-2 py-0.5 rounded uppercase font-semibold"
                        [ngClass]="sug.status === 'Suggested' ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'">
                        {{ sug.status === 'Suggested' ? 'Sugerencia Pendiente' : 'Aceptada' }}
                      </span>
                    </div>

                    <div class="mt-2 text-xs text-slate-600 space-y-1">
                      <div class="flex items-center justify-between font-mono bg-slate-50 p-2 rounded-lg">
                        <span>Origen: <strong>{{ sug.sourceLocationCode }}</strong></span>
                        <span>➔</span>
                        <span>Destino: <strong>{{ sug.targetLocationCode }}</strong></span>
                      </div>
                      <p class="text-[11px] text-slate-500 mt-2 leading-relaxed">
                        {{ sug.rationale }}
                      </p>
                    </div>

                    <div class="mt-3 grid grid-cols-2 gap-2 text-center text-xs font-mono">
                      <div class="bg-purple-50 p-2 rounded border border-purple-100">
                        <span class="text-[10px] text-purple-600 block">ORIGEN (ANTES / DESPUÉS)</span>
                        <span class="font-bold text-purple-900">{{ sug.sourceCoverageDaysBefore }}d ➔ {{ sug.sourceCoverageDaysAfter }}d</span>
                      </div>
                      <div class="bg-emerald-50 p-2 rounded border border-emerald-100">
                        <span class="text-[10px] text-emerald-600 block">DESTINO (ANTES / DESPUÉS)</span>
                        <span class="font-bold text-emerald-900">{{ sug.targetCoverageDaysBefore }}d ➔ {{ sug.targetCoverageDaysAfter }}d</span>
                      </div>
                    </div>
                  </div>

                  @if (sug.status === 'Suggested') {
                    <div class="pt-2 border-t border-slate-100 flex items-center justify-between gap-2">
                      <div class="flex items-center gap-1">
                        <label class="text-[10px] text-slate-400 font-bold uppercase">Mover:</label>
                        <input
                          type="number"
                          [(ngModel)]="sug.suggestedQuantity"
                          class="w-16 px-2 py-1 text-xs border border-slate-300 rounded font-mono font-bold" />
                        <span class="text-[10px] text-slate-500">u</span>
                      </div>

                      <div class="flex items-center gap-2">
                        <button
                          type="button"
                          (click)="dismissSuggestion(sug.id)"
                          class="px-3 py-1.5 text-xs font-semibold text-rose-600 hover:bg-rose-50 rounded-lg transition-colors">
                          Descartar
                        </button>
                        <button
                          type="button"
                          (click)="acceptSuggestion(sug.id, sug.suggestedQuantity)"
                          class="px-3 py-1.5 text-xs font-semibold bg-emerald-600 text-white hover:bg-emerald-700 rounded-lg transition-colors shadow-xs">
                          Aceptar y Crear Pedido
                        </button>
                      </div>
                    </div>
                  } @else {
                    <div class="pt-2 border-t border-slate-100 flex items-center justify-between text-xs">
                      <span class="text-emerald-700 font-semibold">✓ Pedido generado en cola de rutas</span>
                      <a routerLink="/rutas" class="text-primary-600 hover:underline font-semibold">
                        Ir a Planificar Ruta ➔
                      </a>
                    </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }

      <!-- PESTAÑA 3: POLÍTICAS DE STOCK -->
      @if (activeTab() === 'policies') {
        <div class="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-sm">
          <div class="p-4 border-b border-slate-100 flex items-center justify-between">
            <h2 class="text-sm font-bold text-slate-800">
              Configuración de Mínimos y Máximos de Inventario
            </h2>
            <span class="text-xs text-slate-400">Valores sugeridos calculados automáticamente según consumo</span>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 text-[11px] text-slate-500 uppercase border-b border-slate-200">
                <tr>
                  <th class="p-3">Bodega</th>
                  <th class="p-3">Producto</th>
                  <th class="p-3 text-right">Consumo Diario</th>
                  <th class="p-3 text-right">Mínimo Actual</th>
                  <th class="p-3 text-right">Sugerido Mín.</th>
                  <th class="p-3 text-right">Máximo Actual</th>
                  <th class="p-3 text-right">Sugerido Máx.</th>
                  <th class="p-3 text-center">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 font-mono">
                @for (pol of policiesList(); track pol.locationId + pol.productId) {
                  <tr class="hover:bg-slate-50/80 transition-colors">
                    <td class="p-3 font-bold text-slate-900 font-sans">{{ pol.locationCode }}</td>
                    <td class="p-3 font-sans">
                      <span class="font-bold text-slate-800">{{ pol.productName }}</span>
                      <span class="text-slate-400 block text-[10px] font-mono">{{ pol.sku }}</span>
                    </td>
                    <td class="p-3 text-right">{{ pol.dailyConsumptionRate }} u/d</td>
                    <td class="p-3 text-right">
                      <input
                        type="number"
                        [(ngModel)]="pol.currentMinStock"
                        class="w-16 px-1.5 py-0.5 border border-slate-300 rounded text-right font-mono" />
                    </td>
                    <td class="p-3 text-right text-emerald-700 font-bold bg-emerald-50/40">
                      {{ pol.suggestedMinStock }}
                    </td>
                    <td class="p-3 text-right">
                      <input
                        type="number"
                        [(ngModel)]="pol.currentMaxStock"
                        class="w-16 px-1.5 py-0.5 border border-slate-300 rounded text-right font-mono" />
                    </td>
                    <td class="p-3 text-right text-purple-700 font-bold bg-purple-50/40">
                      {{ pol.suggestedMaxStock }}
                    </td>
                    <td class="p-3 text-center space-x-1 font-sans">
                      <button
                        type="button"
                        (click)="applySuggestionToRow(pol)"
                        class="px-2 py-1 text-[11px] bg-slate-100 hover:bg-slate-200 text-slate-700 font-semibold rounded">
                        Usar Sugerido
                      </button>
                      <button
                        type="button"
                        (click)="savePolicy(pol)"
                        class="px-2 py-1 text-[11px] bg-primary-600 hover:bg-primary-700 text-white font-semibold rounded">
                        Guardar
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }
    </div>
  `
})
export class BodegasPage implements OnInit {
  private readonly apiService = inject(ApiService);

  readonly activeTab = signal<'analysis' | 'rebalancing' | 'policies'>('analysis');
  readonly analysisList = signal<WarehouseAnalysisDto[]>([]);
  readonly suggestionsList = signal<TransferSuggestionDto[]>([]);
  readonly policiesList = signal<any[]>([]);
  readonly loadingAction = signal(false);
  readonly feedback = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  readonly pendingSuggestionsCount = signal(0);

  ngOnInit(): void {
    this.loadAnalysis();
    this.loadSuggestions();
    this.loadPolicies();
  }

  loadAnalysis(): void {
    this.apiService.getWarehouseAnalysis().subscribe(res => {
      this.analysisList.set(res || []);
    });
  }

  loadSuggestions(): void {
    this.apiService.getTransferSuggestions().subscribe(res => {
      const list = res || [];
      this.suggestionsList.set(list);
      this.pendingSuggestionsCount.set(list.filter(s => s.status === 'Suggested').length);
    });
  }

  loadPolicies(): void {
    this.apiService.getWarehousePolicies().subscribe(res => {
      this.policiesList.set(res || []);
    });
  }

  generateSuggestions(): void {
    this.loadingAction.set(true);
    this.feedback.set(null);
    this.apiService.generateRebalancingSuggestions().subscribe({
      next: (res) => {
        this.loadingAction.set(false);
        this.feedback.set({
          type: 'success',
          text: `Análisis de consumo finalizado. Se actualizaron las sugerencias de traslado entre bodegas.`
        });
        this.loadSuggestions();
        this.loadAnalysis();
        this.activeTab.set('rebalancing');
      },
      error: (err) => {
        this.loadingAction.set(false);
        this.feedback.set({
          type: 'error',
          text: err?.error?.message || 'Error al ejecutar el análisis de rebalanceo.'
        });
      }
    });
  }

  acceptSuggestion(id: string, qty: number): void {
    this.apiService.acceptTransferSuggestion(id, qty).subscribe({
      next: () => {
        this.feedback.set({
          type: 'success',
          text: 'Traslado aceptado exitosamente. Se ha generado un pedido en la cola de planificación de rutas.'
        });
        this.loadSuggestions();
      },
      error: (err) => {
        this.feedback.set({
          type: 'error',
          text: err?.error?.message || 'Error al aceptar sugerencia.'
        });
      }
    });
  }

  dismissSuggestion(id: string): void {
    this.apiService.dismissTransferSuggestion(id, 'Descartado manualmente').subscribe({
      next: () => {
        this.feedback.set({
          type: 'success',
          text: 'Sugerencia descartada.'
        });
        this.loadSuggestions();
      }
    });
  }

  applySuggestionToRow(pol: any): void {
    pol.currentMinStock = pol.suggestedMinStock;
    pol.currentMaxStock = pol.suggestedMaxStock;
  }

  savePolicy(pol: any): void {
    this.apiService.updateWarehousePolicy(pol.locationId, pol.productId, {
      minStock: pol.currentMinStock,
      maxStock: pol.currentMaxStock,
      leadTimeDays: pol.leadTimeDays,
      targetCoverageDays: pol.targetCoverageDays,
      isManual: true
    }).subscribe({
      next: () => {
        this.feedback.set({
          type: 'success',
          text: `Política guardada para ${pol.productName} en ${pol.locationCode}.`
        });
      },
      error: (err) => {
        this.feedback.set({
          type: 'error',
          text: err?.error?.message || 'Error al guardar política.'
        });
      }
    });
  }
}
