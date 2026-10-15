import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSession } from '@entities/session';
import { ApiService, RiskItemDto } from '@shared/api';

type FilterType = 'ALL' | 'CRITICAL' | 'WARNING' | 'NORMAL';

@Component({
  selector: 'page-alertas',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8 space-y-6">
      <!-- Encabezado Principal de Auditoría -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 pb-5">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
              Auditoría de Cobertura y Demanda
            </h1>
            <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200">
              Panel de Fiscalización (Solo Lectura)
            </span>
          </div>
          <p class="text-sm text-slate-500 mt-1">
            Fiscalización de stock crítico, validación de umbrales normativos (Lead Time + Margen) y trazabilidad de cálculo de reabastecimiento.
          </p>
        </div>

        <!-- Acciones de Cabecera -->
        <div class="flex items-center gap-3">
          <button
            type="button"
            (click)="refreshData()"
            [disabled]="loading()"
            class="inline-flex items-center text-xs font-semibold px-3 py-2 rounded-lg border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 transition-colors shadow-xs disabled:opacity-50">
            <span>{{ loading() ? 'Recalculando...' : 'Actualizar Análisis' }}</span>
          </button>

          <a
            routerLink="/rutas"
            class="inline-flex items-center text-xs font-semibold px-3 py-2 rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors shadow-xs">
            <span>Auditar Rutas de Distribución</span>
          </a>
        </div>
      </div>

      <!-- Tarjetas de Métricas de Auditoría (KPIs) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Puntos Críticos -->
        <div class="p-4 rounded-xl bg-white border border-rose-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-rose-700 uppercase tracking-wide">Puntos Críticos</span>
            <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-200">
              Quiebre Inminente
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-2xl font-bold font-mono text-rose-700">{{ criticalCount() }}</span>
            <span class="text-xs text-slate-500">ítems con stock &le; lead time</span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            Atención prioritaria obligatoria para evitar desabastecimiento hospitalario.
          </p>
        </div>

        <!-- Puntos en Observación -->
        <div class="p-4 rounded-xl bg-white border border-amber-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-amber-700 uppercase tracking-wide">En Observación</span>
            <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-amber-50 text-amber-700 border border-amber-200">
              Vigilar / Alto
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-2xl font-bold font-mono text-amber-700">{{ warningCount() }}</span>
            <span class="text-xs text-slate-500">ítems en margen preventivo</span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            Cobertura dentro de ventana de seguridad y consolidación de despachos.
          </p>
        </div>

        <!-- Cobertura Promedio -->
        <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-600 uppercase tracking-wide">Cobertura Media</span>
            <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-slate-100 text-slate-600">
              Red General
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-2xl font-bold font-mono text-slate-900">{{ averageCoverage().toFixed(1) }}</span>
            <span class="text-xs text-slate-500">días promedio</span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            Calculado sobre los puntos con consumo activo en el periodo de demanda.
          </p>
        </div>

        <!-- Volumen Sugerido -->
        <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-xs">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-primary-700 uppercase tracking-wide">Déficit Requerido</span>
            <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-primary-50 text-primary-700 border border-primary-200">
              Reabastecimiento
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="text-2xl font-bold font-mono text-primary-700">{{ totalSuggested() | number }}</span>
            <span class="text-xs text-slate-500">unidades calculadas</span>
          </div>
          <p class="text-[11px] text-slate-500 mt-2">
            Requerimiento total para alcanzar cobertura objetivo neta de la red.
          </p>
        </div>
      </div>

      <!-- Barra de Filtros y Búsqueda -->
      <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-white p-3 rounded-xl border border-slate-200 shadow-xs">
        <div class="flex flex-wrap items-center gap-1.5">
          <button 
            type="button"
            (click)="setFilter('ALL')"
            [class.bg-slate-900]="currentFilter() === 'ALL'"
            [class.text-white]="currentFilter() === 'ALL'"
            [class.bg-slate-100]="currentFilter() !== 'ALL'"
            [class.text-slate-700]="currentFilter() !== 'ALL'"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors">
            Todos ({{ allItems().length }})
          </button>

          <button 
            type="button"
            (click)="setFilter('CRITICAL')"
            [class.bg-rose-600]="currentFilter() === 'CRITICAL'"
            [class.text-white]="currentFilter() === 'CRITICAL'"
            [class.bg-rose-50]="currentFilter() !== 'CRITICAL'"
            [class.text-rose-700]="currentFilter() !== 'CRITICAL'"
            class="px-3 py-1.5 rounded-lg border border-rose-200 text-xs font-semibold transition-colors">
            Críticos ({{ criticalCount() }})
          </button>

          <button 
            type="button"
            (click)="setFilter('WARNING')"
            [class.bg-amber-600]="currentFilter() === 'WARNING'"
            [class.text-white]="currentFilter() === 'WARNING'"
            [class.bg-amber-50]="currentFilter() !== 'WARNING'"
            [class.text-amber-700]="currentFilter() !== 'WARNING'"
            class="px-3 py-1.5 rounded-lg border border-amber-200 text-xs font-semibold transition-colors">
            En Observación ({{ warningCount() }})
          </button>

          <button 
            type="button"
            (click)="setFilter('NORMAL')"
            [class.bg-emerald-600]="currentFilter() === 'NORMAL'"
            [class.text-white]="currentFilter() === 'NORMAL'"
            [class.bg-emerald-50]="currentFilter() !== 'NORMAL'"
            [class.text-emerald-700]="currentFilter() !== 'NORMAL'"
            class="px-3 py-1.5 rounded-lg border border-emerald-200 text-xs font-semibold transition-colors">
            Conformes ({{ normalCount() }})
          </button>
        </div>

        <div class="w-full sm:w-64">
          <input
            type="text"
            [(ngModel)]="searchQuery"
            placeholder="Buscar por hospital o SKU..."
            class="w-full text-xs px-3 py-1.5 rounded-lg border border-slate-300 focus:outline-none focus:ring-1 focus:ring-primary-500 text-slate-800" />
        </div>
      </div>

      <!-- Estado cargando -->
      @if (loading()) {
        <div class="p-12 text-center text-slate-500 text-sm bg-white border border-slate-200 rounded-xl space-y-2">
          <div class="font-semibold text-slate-700">Calculando proyección determinística de inventario...</div>
          <div class="text-xs text-slate-400">Analizando balances de stock, consumos históricos prorrateados y tránsitos activos.</div>
        </div>
      }

      <!-- Estado vacío -->
      @if (!loading() && allItems().length === 0) {
        <div class="p-8 text-center bg-white border border-slate-200 rounded-xl space-y-3">
          <p class="text-slate-600 text-sm font-medium">
            No se han registrado consumos o inventario aún. El sistema no tiene datos para evaluar.
          </p>
          <p class="text-xs text-slate-400">
            Para auditar riesgos, asegúrate de que existan puntos de servicio con stock y consumos cargados.
          </p>
          @if (session.canWrite()) {
            <div>
              <a
                routerLink="/datos"
                class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors">
                Cargar Datos Maestros
              </a>
            </div>
          }
        </div>
      }

      <!-- Tabla Principal de Auditoría -->
      @if (!loading() && filteredItems().length > 0) {
        <div class="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-xs">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs text-slate-600 min-w-[700px]">
              <thead class="bg-slate-50 text-[11px] font-bold text-slate-500 uppercase tracking-wider border-b border-slate-200">
                <tr>
                  <th class="py-3 px-4">Punto / Hospital</th>
                  <th class="py-3 px-4">Insumo / SKU</th>
                  <th class="py-3 px-4 text-right">Disponible</th>
                  <th class="py-3 px-4 text-right">En Tránsito</th>
                  <th class="py-3 px-4 text-right">Demanda Diaria</th>
                  <th class="py-3 px-4 text-right">Cobertura</th>
                  <th class="py-3 px-4 text-center">Nivel de Riesgo</th>
                  <th class="py-3 px-4 text-right">Reabastecimiento</th>
                  <th class="py-3 px-4 text-center">Trazabilidad</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 font-sans">
                @for (item of filteredItems(); track item.servicePointCode + item.sku) {
                  <tr class="hover:bg-slate-50/80 transition-colors" [class.bg-rose-50/30]="item.level === 'Critico' || item.level === 'Critical'">
                    <!-- Punto / Hospital -->
                    <td class="py-3.5 px-4">
                      <span class="font-bold text-slate-900 block">{{ item.servicePointCode }}</span>
                      @if (item.criticality) {
                        <span class="text-[10px] text-slate-500 uppercase">Criticidad: {{ item.criticality }}</span>
                      }
                    </td>

                    <!-- SKU -->
                    <td class="py-3.5 px-4 font-mono font-medium text-slate-800">
                      <div>{{ item.sku }}</div>
                      @if (item.baseUnit) {
                        <span class="text-[10px] text-slate-400 font-sans">{{ item.baseUnit }}</span>
                      }
                    </td>

                    <!-- Disponible -->
                    <td class="py-3.5 px-4 text-right font-mono font-medium text-slate-800">
                      {{ item.availableQuantity | number }}
                    </td>

                    <!-- En Tránsito -->
                    <td class="py-3.5 px-4 text-right font-mono text-slate-500">
                      {{ (item.inTransitQuantity || 0) | number }}
                    </td>

                    <!-- Demanda Diaria -->
                    <td class="py-3.5 px-4 text-right font-mono text-slate-600">
                      {{ item.dailyDemand ? (item.dailyDemand.toFixed(2) + ' /día') : 'Sin consumo' }}
                    </td>

                    <!-- Cobertura -->
                    <td class="py-3.5 px-4 text-right font-mono font-bold" [ngClass]="{
                      'text-rose-700': item.level === 'Critico' || item.level === 'Critical' || item.level === 'Alto' || item.level === 'High',
                      'text-amber-700': item.level === 'Vigilar' || item.level === 'Medium',
                      'text-emerald-700': item.level === 'Normal' || item.level === 'Low',
                      'text-slate-400': !item.coverageDays
                    }">
                      {{ item.coverageDays != null ? (item.coverageDays.toFixed(1) + ' días') : 'N/D' }}
                    </td>

                    <!-- Nivel de Riesgo (Etiqueta de Texto Limpia) -->
                    <td class="py-3.5 px-4 text-center">
                      <span 
                        class="inline-flex px-2 py-0.5 rounded text-[10px] font-bold tracking-wide uppercase font-mono"
                        [ngClass]="getRiskBadgeClass(item.level)">
                        [{{ item.level }}]
                      </span>
                    </td>

                    <!-- Reabastecimiento Sugerido -->
                    <td class="py-3.5 px-4 text-right font-mono font-bold" [ngClass]="item.suggestedQuantity > 0 ? 'text-primary-700' : 'text-slate-400'">
                      {{ item.suggestedQuantity > 0 ? ('+' + (item.suggestedQuantity | number)) : '0' }}
                    </td>

                    <!-- Botón de Inspección / Trazabilidad -->
                    <td class="py-3.5 px-4 text-center">
                      <button
                        type="button"
                        (click)="inspectItem(item)"
                        class="text-[11px] font-semibold text-indigo-600 hover:text-indigo-800 underline">
                        Auditar
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Modal de Detalle / Trazabilidad Matemática de Auditoría -->
      @if (selectedAuditItem()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
          <div class="bg-white rounded-2xl shadow-xl border border-slate-200 max-w-lg w-full overflow-hidden animate-in fade-in zoom-in duration-150">
            <div class="bg-slate-900 text-white p-4 flex items-center justify-between">
              <div>
                <span class="text-xs font-mono uppercase text-indigo-300">Expediente de Auditoría</span>
                <h3 class="text-base font-bold text-white">
                  {{ selectedAuditItem()!.servicePointCode }} &mdash; {{ selectedAuditItem()!.sku }}
                </h3>
              </div>
              <button
                type="button"
                (click)="selectedAuditItem.set(null)"
                class="text-xs font-semibold px-2.5 py-1 rounded bg-slate-800 text-slate-300 hover:text-white transition-colors">
                Cerrar
              </button>
            </div>

            <div class="p-5 space-y-4 text-xs">
              <!-- Desglose de Parámetros -->
              <div class="grid grid-cols-2 gap-3 bg-slate-50 p-3 rounded-xl border border-slate-200 font-mono">
                <div>
                  <span class="text-[10px] text-slate-400 block font-sans">Stock en Sitio</span>
                  <span class="font-bold text-slate-800 text-sm">{{ selectedAuditItem()!.availableQuantity }}</span>
                </div>
                <div>
                  <span class="text-[10px] text-slate-400 block font-sans">En Tránsito (Despachos)</span>
                  <span class="font-bold text-slate-800 text-sm">{{ selectedAuditItem()!.inTransitQuantity || 0 }}</span>
                </div>
                <div>
                  <span class="text-[10px] text-slate-400 block font-sans">Demanda Diaria Estimada</span>
                  <span class="font-bold text-slate-800 text-sm">{{ selectedAuditItem()!.dailyDemand != null ? (selectedAuditItem()!.dailyDemand.toFixed(2) + ' /día') : '0.00 /día' }}</span>
                </div>
                <div>
                  <span class="text-[10px] text-slate-400 block font-sans">Días de Cobertura Reales</span>
                  <span class="font-bold text-sm" [ngClass]="selectedAuditItem()!.coverageDays <= selectedAuditItem()!.leadTimeDays ? 'text-rose-600' : 'text-slate-800'">
                    {{ selectedAuditItem()!.coverageDays != null ? (selectedAuditItem()!.coverageDays.toFixed(1) + ' días') : '0.0 días' }}
                  </span>
                </div>
              </div>

              <!-- Umbrales Normativos del Centro de Salud -->
              <div class="border border-slate-200 rounded-xl p-3 space-y-2">
                <span class="font-bold text-slate-800 block text-[11px] uppercase tracking-wide">
                  Umbrales Normativos de Suministro
                </span>
                <div class="space-y-1 text-slate-600">
                  <div class="flex justify-between">
                    <span>Lead Time (Tiempo de Despacho):</span>
                    <span class="font-mono font-bold">{{ selectedAuditItem()!.leadTimeDays }} días</span>
                  </div>
                  <div class="flex justify-between">
                    <span>Margen de Seguridad (Buffer):</span>
                    <span class="font-mono font-bold">{{ selectedAuditItem()!.safetyMarginDays }} días</span>
                  </div>
                  <div class="flex justify-between border-t border-slate-100 pt-1 text-slate-800 font-semibold">
                    <span>Umbral Crítico de Ruptura:</span>
                    <span class="font-mono text-rose-600">&le; {{ selectedAuditItem()!.leadTimeDays }} días</span>
                  </div>
                  <div class="flex justify-between text-slate-800 font-semibold">
                    <span>Umbral Preventivo (Vigilancia):</span>
                    <span class="font-mono text-amber-600">&le; {{ selectedAuditItem()!.leadTimeDays + selectedAuditItem()!.safetyMarginDays }} días</span>
                  </div>
                </div>
              </div>

              <!-- Justificación Técnica / Conclusión -->
              <div class="bg-indigo-50 border border-indigo-200 rounded-xl p-3 text-indigo-900 space-y-1">
                <span class="font-bold block text-[11px] uppercase tracking-wide">
                  Dictamen de Auditoría
                </span>
                <p class="text-[11px] leading-relaxed text-indigo-800">
                  @if (selectedAuditItem()!.level === 'Critico' || selectedAuditItem()!.level === 'Critical') {
                    El stock actual no cubre el tiempo de reposición del proveedor (Lead Time). Se requiere despacho prioritario de {{ selectedAuditItem()!.suggestedQuantity }} unidades.
                  } @else if (selectedAuditItem()!.level === 'Alto' || selectedAuditItem()!.level === 'High' || selectedAuditItem()!.level === 'Vigilar') {
                    El stock cubre el tiempo mínimo de reposición pero se encuentra dentro del margen de seguridad preventivo. Se sugiere programar consolidación en el próximo ciclo de ruta.
                  } @else {
                    El nivel de inventario cumple con las normas de cobertura establecida. No requiere reposición emergente.
                  }
                </p>
              </div>
            </div>

            <div class="bg-slate-50 px-5 py-3 border-t border-slate-200 flex justify-end">
              <button
                type="button"
                (click)="selectedAuditItem.set(null)"
                class="px-4 py-1.5 text-xs font-semibold rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors">
                Entendido
              </button>
            </div>
          </div>
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
  readonly currentFilter = signal<FilterType>('ALL');
  searchQuery = '';
  readonly selectedAuditItem = signal<RiskItemDto | null>(null);

  readonly criticalCount = computed(() => {
    return this.allItems().filter(i => i.level === 'Critico' || i.level === 'Critical').length;
  });

  readonly warningCount = computed(() => {
    return this.allItems().filter(i => i.level === 'Alto' || i.level === 'High' || i.level === 'Vigilar' || i.level === 'Medium').length;
  });

  readonly normalCount = computed(() => {
    return this.allItems().filter(i => i.level === 'Normal' || i.level === 'Low').length;
  });

  readonly averageCoverage = computed(() => {
    const valid = this.allItems().filter(i => i.coverageDays != null && i.coverageDays > 0);
    if (valid.length === 0) return 0;
    const sum = valid.reduce((acc, cur) => acc + (cur.coverageDays || 0), 0);
    return sum / valid.length;
  });

  readonly totalSuggested = computed(() => {
    return this.allItems().reduce((acc, cur) => acc + (cur.suggestedQuantity || 0), 0);
  });

  readonly filteredItems = computed(() => {
    let items = this.allItems();

    // Filtro por estado
    if (this.currentFilter() === 'CRITICAL') {
      items = items.filter(i => i.level === 'Critico' || i.level === 'Critical');
    } else if (this.currentFilter() === 'WARNING') {
      items = items.filter(i => i.level === 'Alto' || i.level === 'High' || i.level === 'Vigilar' || i.level === 'Medium');
    } else if (this.currentFilter() === 'NORMAL') {
      items = items.filter(i => i.level === 'Normal' || i.level === 'Low');
    }

    // Filtro por texto de búsqueda
    const q = this.searchQuery.trim().toLowerCase();
    if (q) {
      items = items.filter(i =>
        i.servicePointCode.toLowerCase().includes(q) ||
        i.sku.toLowerCase().includes(q)
      );
    }

    return items;
  });

  ngOnInit(): void {
    this.fetchRisks(false);
  }

  setFilter(filter: FilterType): void {
    this.currentFilter.set(filter);
  }

  refreshData(): void {
    this.fetchRisks(true);
  }

  inspectItem(item: RiskItemDto): void {
    this.selectedAuditItem.set(item);
  }

  getRiskBadgeClass(level: string): string {
    switch (level) {
      case 'Critico':
      case 'Critical':
        return 'bg-rose-50 text-rose-700 border border-rose-200';
      case 'Alto':
      case 'High':
        return 'bg-orange-50 text-orange-700 border border-orange-200';
      case 'Vigilar':
      case 'Medium':
        return 'bg-amber-50 text-amber-700 border border-amber-200';
      case 'Normal':
      case 'Low':
        return 'bg-emerald-50 text-emerald-700 border border-emerald-200';
      default:
        return 'bg-slate-50 text-slate-600 border border-slate-200';
    }
  }

  private fetchRisks(refresh: boolean): void {
    this.loading.set(true);
    this.apiService.getRisks(undefined, refresh).subscribe(res => {
      this.loading.set(false);
      if (res && res.items) {
        this.allItems.set(res.items);
      } else {
        this.allItems.set([]);
      }
    });
  }
}
