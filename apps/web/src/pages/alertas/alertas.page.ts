import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import { ApiService, RiskItemDto } from '@shared/api';

@Component({
  selector: 'page-alertas',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="max-w-5xl mx-auto px-4 py-8 space-y-6">
      <!-- Encabezado -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
            Alertas de Cobertura y Demanda
          </h1>
          <p class="text-sm text-slate-500 mt-1">
            Predicción de quiebre de stock calculada con la demanda histórica y el inventario actual.
          </p>
        </div>

        <!-- Filtros rápidos -->
        <div class="flex items-center gap-2 self-start sm:self-auto">
          <button 
            type="button"
            (click)="setFilter('ALL')"
            [class.bg-slate-900]="currentFilter() === 'ALL'"
            [class.text-white]="currentFilter() === 'ALL'"
            [class.bg-white]="currentFilter() !== 'ALL'"
            [class.text-slate-600]="currentFilter() !== 'ALL'"
            class="px-3 py-1.5 rounded-lg border border-slate-200 text-xs font-semibold transition-colors">
            Todos ({{ allItems().length }})
          </button>
          <button 
            type="button"
            (click)="setFilter('CRITICAL')"
            [class.bg-rose-600]="currentFilter() === 'CRITICAL'"
            [class.text-white]="currentFilter() === 'CRITICAL'"
            [class.bg-white]="currentFilter() !== 'CRITICAL'"
            [class.text-rose-600]="currentFilter() !== 'CRITICAL'"
            class="px-3 py-1.5 rounded-lg border border-rose-200 text-xs font-semibold transition-colors">
            Críticos ({{ criticalCount() }})
          </button>
        </div>
      </div>

      <!-- Estado cargando -->
      @if (loading()) {
        <div class="p-8 text-center text-slate-500 text-sm bg-white border border-slate-200 rounded-xl">
          Calculando proyección de inventario...
        </div>
      }

      <!-- Estado vacío -->
      @if (!loading() && allItems().length === 0) {
        <div class="p-8 text-center bg-white border border-slate-200 rounded-xl space-y-3">
          <p class="text-slate-600 text-sm">
            No se han registrado consumos o inventario aún. El sistema no tiene datos para evaluar.
          </p>
          @if (session.canWrite()) {
            <div>
              <a
                routerLink="/datos"
                class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors">
                Ir al Paso 1: Cargar Datos →
              </a>
            </div>
          }
        </div>
      }

      <!-- Tabla de alertas reales -->
      @if (!loading() && filteredItems().length > 0) {
        <div class="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-xs">
          <table class="w-full text-left text-xs text-slate-600">
            <thead class="bg-slate-50 text-[11px] font-semibold text-slate-500 uppercase tracking-wider border-b border-slate-200">
              <tr>
                <th class="py-3 px-4">Hospital / Punto</th>
                <th class="py-3 px-4">Producto (SKU)</th>
                <th class="py-3 px-4 text-right">Disponible</th>
                <th class="py-3 px-4 text-right">Cobertura</th>
                <th class="py-3 px-4 text-center">Nivel de Riesgo</th>
                <th class="py-3 px-4 text-right">Cantidad Sugerida</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 font-sans">
              @for (item of filteredItems(); track item.servicePointCode + item.sku) {
                <tr class="hover:bg-slate-50/70 transition-colors">
                  <td class="py-3.5 px-4">
                    <span class="font-bold text-slate-900 block">{{ item.servicePointCode }}</span>
                  </td>
                  <td class="py-3.5 px-4 font-mono font-medium text-slate-800">
                    {{ item.sku }}
                  </td>
                  <td class="py-3.5 px-4 text-right font-mono">
                    {{ item.availableQuantity }}
                  </td>
                  <td class="py-3.5 px-4 text-right font-mono font-bold" [ngClass]="{
                    'text-rose-600': item.level === 'Critical' || item.level === 'High',
                    'text-amber-600': item.level === 'Medium',
                    'text-emerald-600': item.level === 'Low'
                  }">
                    {{ item.coverageDays.toFixed(1) }} días
                  </td>
                  <td class="py-3.5 px-4 text-center">
                    <span 
                      class="inline-flex px-2 py-0.5 rounded text-[11px] font-medium"
                      [ngClass]="{
                        'bg-rose-50 text-rose-700 border border-rose-200': item.level === 'Critical' || item.level === 'High',
                        'bg-amber-50 text-amber-700 border border-amber-200': item.level === 'Medium',
                        'bg-emerald-50 text-emerald-700 border border-emerald-200': item.level === 'Low'
                      }">
                      {{ item.level }}
                    </span>
                  </td>
                  <td class="py-3.5 px-4 text-right font-mono font-bold text-primary-700">
                    +{{ item.suggestedQuantity }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Acceso directo al paso 3 -->
        <div class="flex justify-end pt-2">
          <a 
            routerLink="/rutas" 
            class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors">
            Pasar al Paso 3: Crear Rutas de Despacho →
          </a>
        </div>
      }
    </div>
  `
})
export class AlertasPage implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly session = inject(AuthSession);

  readonly allItems = signal<RiskItemDto[]>([]);
  readonly loading = signal(true);
  readonly currentFilter = signal<'ALL' | 'CRITICAL'>('ALL');

  readonly criticalCount = computed(() => {
    return this.allItems().filter(i => i.level === 'Critical' || i.level === 'High').length;
  });

  readonly filteredItems = computed(() => {
    if (this.currentFilter() === 'CRITICAL') {
      return this.allItems().filter(i => i.level === 'Critical' || i.level === 'High');
    }
    return this.allItems();
  });

  ngOnInit(): void {
    this.fetchRisks();
  }

  setFilter(filter: 'ALL' | 'CRITICAL'): void {
    this.currentFilter.set(filter);
  }

  private fetchRisks(): void {
    this.loading.set(true);
    this.apiService.getRisks().subscribe(res => {
      this.loading.set(false);
      if (res && res.items) {
        this.allItems.set(res.items);
      } else {
        this.allItems.set([]);
      }
    });
  }
}
